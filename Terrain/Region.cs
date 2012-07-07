/*-----------------------------------------------------------------------------
  Region.cs
-------------------------------------------------------------------------------
  This holds the region grid, which is the main table of information from 
  which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
  the resulting data is not STORED here. Regions are sets of rules and 
  properties. You crank numbers through them, and it creates the world. 

  This output data is stored and managed elsewhere. (See CPage.cpp)
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	enum Climate {
		Invalid, Ocean, Coast, Mountain, River, RiverBank, Swamp,
		Rocky, Lake, Desert, Field, Plains, Canyon, Forest
	}

	struct Region {
		#region Member variables
		public int
			MountainHeight,
			RiverId,
			RiverSegment,
			FlagsShape;

		public float
			RiverWidth,
			// Geo scale is a number from -1 to 1. -1 is lowest ocean. 0 is sea level. 
			// +1 is highest elevation on the island. This is used to guide other derived numbers.
			GeoScale,
			GeoWater,
			GeoDetail,
			GeoLarge,
			Temperature,
			Moisture,
			Threshold,
			BeachThreshold;

		public Color4
			ColorMap,
			ColorRock,
			ColorDirt,
			ColorGrass,
			ColorAtmosphere;
		public Color4[] ColorFlowers;
		public int[] FlowerShape;

		public string Title;
		public Coord GridPos;
		public Climate Climate;
		public bool HasFlowers;
		#endregion

		#region Methods
		public Region(string title, float geoLarge, float geoDetail, float geoScale, float geoWater,
				int mountainHeight, int x, int y, Color4 colorMap, Climate climate) {
			Title = title;
			GeoLarge = geoLarge;
			GeoDetail = geoDetail;
			GeoScale = geoDetail;
			GeoWater = geoWater;
			MountainHeight = mountainHeight;
			GridPos = new Coord(x, y);
			ColorMap = colorMap;
			Climate = climate;
			
			ColorFlowers = new Color4[FLOWERS];
			FlowerShape = new int[FLOWERS];
		}

		#region Module functions
		private Region RegionGet(float x, float y) {
			x /= REGION_SIZE;
			y /= REGION_SIZE;
			if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
				return Continent[0, 0];
			return Continent[(int) x, (int) y];
		}

		private Region RegionGet(int x, int y) {
			x = Math.Max(x, 0);
			y = Math.Max(y, 0);
			x += Dithermap[x % DITHER_SIZE, y % DITHER_SIZE].X;
			y += Dithermap[x % DITHER_SIZE, y % DITHER_SIZE].Y;
			x /= REGION_SIZE;
			y /= REGION_SIZE;
			if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
				return Continent[0, 0];
			return Continent[x, y];
		}

		private void RegionInit() {
			// Fill in the dither table - a table of random offsets
			for (int x = 0; x < DITHER_SIZE; x++) {
				for (int y = 0; y < DITHER_SIZE; y++) {
					Dithermap[x, y].X = Random.Value() % DITHER_SIZE + Random.Value() % DITHER_SIZE;
					Dithermap[x, y].Y = Random.Value() % DITHER_SIZE + Random.Value() % DITHER_SIZE;
				}
			}
			// Set some defaults
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					Region r = new Region();
					Continent[x, y] = r;
				}
			}
		}

		private Cell WorldCell(int world_x, int world_y) {
			float esmall = Entropy(world_x, world_y);
			float elarge = Entropy((float) world_x / LARGE_SCALE, (float) world_y / LARGE_SCALE);
			float bias = RegionWaterLevel(world_x, world_y);

			Coord origin = new Coord(
				FMath.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				FMath.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));

			// Get our offset from the region origin as a pair of scalars.
			Vector2 blend = new Vector2(
				(world_x % BLEND_DISTANCE) / BLEND_DISTANCE,
				(world_y % BLEND_DISTANCE) / BLEND_DISTANCE);

			Vector2 offset = new Vector2(
				((world_x) % REGION_SIZE) / REGION_SIZE,
				((world_y) % REGION_SIZE) / REGION_SIZE);

			bool left = ((origin.X + origin.Y) % 2) == 0;

			Cell result;
			result.detail = esmall;
			result.water_level = bias;

			//Upper left and bottom-right corners
			Coord ul = new Coord(origin.X, origin.Y);
			Coord br = new Coord(
				(world_x + BLEND_DISTANCE) / REGION_SIZE,
				(world_y + BLEND_DISTANCE) / REGION_SIZE);

			if (ul == br) {
				Region rul = GetRegion(ul.X, ul.Y);
				result.elevation = DoHeight(rul, offset, bias, esmall, elarge);
				result.elevation = DoHeightNoBlend(result.elevation, rul, offset, bias);
				return result;
			} else {
			  // Four corners: upper left, upper right, etc.
				Region rul = GetRegion(ul.X, ul.Y);
				Region rur = GetRegion(br.X, ul.Y);
				Region rbl = GetRegion(ul.X, br.Y);
				Region rbr = GetRegion(br.X, br.Y);

				float eul = DoHeight(rul, offset, bias, esmall, elarge);
				float eur = DoHeight(rur, offset, bias, esmall, elarge);
				float ebl = DoHeight(rbl, offset, bias, esmall, elarge);
				float ebr = DoHeight(rbr, offset, bias, esmall, elarge);

				result.elevation = FMath.InterpolateQuad(eul, eur, ebl, ebr, blend, left);
				result.elevation = DoHeightNoBlend(result.elevation, rul, offset, bias);
				return result;
			}
		}
		#endregion
		#endregion
	}

	class RegionManager {
		#region Constants
		public const int
			REGION_SIZE             = 64,
			REGION_HALF             = (REGION_SIZE / 2),
			WORLD_GRID              = 128,
			WORLD_GRID_EDGE         = (WORLD_GRID + 1),
			WORLD_GRID_CENTER       = (WORLD_GRID / 2),

			REGION_FLAG_TEST        = 0x0001,
			REGION_FLAG_MESAS       = 0x0002,
			REGION_FLAG_CRATER      = 0x0004,
			REGION_FLAG_BEACH       = 0x0008,
			REGION_FLAG_BEACH_CLIFF = 0x0010,
			REGION_FLAG_SINKHOLE    = 0x0020,
			REGION_FLAG_CRACK       = 0x0040,
			REGION_FLAG_TIERED      = 0x0080,
			REGION_FLAG_CANYON_NS   = 0x0100,
			REGION_FLAG_NOBLEND     = 0x0200,
			REGION_FLAG_RIVERN      = 0x1000,
			REGION_FLAG_RIVERE      = 0x2000,
			REGION_FLAG_RIVERS      = 0x4000,
			REGION_FLAG_RIVERW      = 0x8000,
			REGION_FLAG_RIVER_ANY   = 0xf000,
			//REGION_FLAG_DESERT      = 0x4000

			REGION_FLAG_RIVERNS     = (REGION_FLAG_RIVERN | REGION_FLAG_RIVERS),
			REGION_FLAG_RIVEREW     = (REGION_FLAG_RIVERE | REGION_FLAG_RIVERW),
			
			FLOWERS                 = 3;

		private const int
			LARGE_SCALE       = 9,
			SMALL_STRENGTH    = 1,
			LARGE_STRENGTH    = (REGION_SIZE * 1),
			BLEND_DISTANCE    = (REGION_SIZE / 4),
			DITHER_SIZE       = (REGION_SIZE / 2),
			OCEAN_BUFFER      = 20, // The number of regions around the edge which must be ocean
			FREQUENCY         = 3; // Higher numbers make the overall map repeat more often

		private const string
			NNORTH            = "Northern",
			NSOUTH            = "Southern",
			NEAST             = "Eastern",
			NWEST             = "Western";
		#endregion

		#region Member variables
		private static Region[,] Continent = new Region[WORLD_GRID, WORLD_GRID];
		private static Coord[,] Dithermap = new Coord[DITHER_SIZE, DITHER_SIZE];

		private static int mapId;
		public static int RegionMap { get { return mapId; } private set { mapId = value; } }

		private static Color4[] FlowerPalette = {
			Color4.White, Color4.White, Color4.White,
			Color4.Red, Color4.Red,
			Color4.Yellow, Color4.Yellow,
			Color4.Violet,
			Color4.Pink,
			Color4.LightPink,
			Color4.Maroon
		};
		#endregion

		#region Methods
		public static Region WorldRegionGet(int x, int y) { return Continent[x, y]; }
		public static void WorldRegionSet(int x, int y, Region region) { Continent[x, y] = region; }

		private static void DoMap() {
			if (mapId == 0)
				GL.GenTextures(1, out mapId);
			GL.BindTexture(TextureTarget.Texture2D, mapId);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);	
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);	

			int[] buffer = new int[WORLD_GRID * WORLD_GRID * 3];
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					//Flip it vertically, because the OpenGL texture coord system is retarded.
					int yy = (WORLD_GRID - 1) - y;
					Region r = Continent[x, yy];
					int i = (x + y * WORLD_GRID) * 3;
					buffer[i]			= (int) (r.ColorMap.R * 255.0f);
					buffer[i + 1]	= (int) (r.ColorMap.G * 255.0f);
					buffer[i + 2]	= (int) (r.ColorMap.B * 255.0f);
				}
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, WORLD_GRID, WORLD_GRID,
				0, PixelFormat.Rgb, PixelType.Int, buffer);
		}

		private static void GenerateRegions() {
			//Set some defaults
			Coord offset = new Coord(
				Random.Value() % 1024,
				Random.Value() % 1024);
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					Coord from_center = new Coord(
						Math.Abs(x - WORLD_GRID_CENTER),
						Math.Abs(y - WORLD_GRID_CENTER));

					Vector2 v = new Vector2(from_center.X, from_center.Y);

					Region r = new Region(
						"NOTHING",				// Title
						0,								// GeoLarge
						0,								// GeoDetail
						// Geo scale is a number from -1 to 1. -1 is lowest ocean. 0 is sea level. 
						// +1 is highest elevation on the island. This is used to guide other derived numbers.
						v.Length / (WORLD_GRID_CENTER - OCEAN_BUFFER),
						0,								// GeoWater
						0,								// MountainHeight
						x, y,							// GridPos
						Color4.Black,			// ColorMap
						Climate.Invalid		// Climate
					);

					// Create a steep drop around the edge of the world
					if (r.GeoScale > 1.25f)
						r.GeoScale = 1.25f + (r.GeoScale - 1.25f) * 2.0f;
					r.GeoScale = 1.0f - r.GeoScale;
					r.GeoScale += (Entropy((x + offset.X) * FREQUENCY, (y + offset.Y) * FREQUENCY) - 0.2f) / 1;
					r.GeoScale = FMath.Clamp(r.GeoScale, -1.0f, 1.0f);
					if (r.GeoScale > 0.0f)
						r.GeoWater = 1.0f + r.GeoScale;
					r.GeoLarge = 0.3f;
					r.GeoLarge = 0.0f;
					r.GeoDetail = 0.0f;

					Continent[x, y] = r;
				}
			}
			TerraformOceans();
			TerraformCoast();
			TerraformClimate();
			TerraformRivers(4);
			TerraformClimate(); // Do climate a second time now that rivers are in
			TerraformZones();
			TerraformFill();
			TerraformAverage();
			TerraformColors();
			DoMap();
		}

		private static Color4 WorldColorGet(int world_x, int world_y, SurfaceColor c) {
			int x = Math.Max(world_x % DITHER_SIZE, 0);
			int y = Math.Max(world_y % DITHER_SIZE, 0);

			world_x += Dithermap[x, y].X;
			world_y += Dithermap[x, y].Y;

			Vector2 offset = new Vector2(
				(world_x % REGION_SIZE) / REGION_SIZE,
				(world_y % REGION_SIZE) / REGION_SIZE);

			Coord origin = new Coord(
				world_x / REGION_SIZE,
				world_y / REGION_SIZE);

			Region r0 = GetRegion(origin.X, origin.Y);
			Region r1 = GetRegion(origin.X + 1, origin.Y);
			Region r2 = GetRegion(origin.X, origin.Y + 1);
			Region r3 = GetRegion(origin.X + 1, origin.Y + 1);
			//return r0.color_grass;

			Color4    c0, c1, c2, c3;
			switch (c) {
				case SurfaceColor.Grass:
					c0 = r0.ColorGrass;
					c1 = r1.ColorGrass;
					c2 = r2.ColorGrass;
					c3 = r3.ColorGrass;
					break;
				case SurfaceColor.Dirt:
					c0 = r0.ColorDirt;
					c1 = r1.ColorDirt;
					c2 = r2.ColorDirt;
					c3 = r3.ColorDirt;
					break;
				case SurfaceColor.Rock:
				default:
					c0 = r0.ColorRock;
					c1 = r1.ColorRock;
					c2 = r2.ColorRock;
					c3 = r3.ColorRock;
					break;
			}

			return new Color4(
				FMath.InterpolateQuad(c0.R, c1.R, c2.R, c3.R, offset),
				FMath.InterpolateQuad(c0.G, c1.G, c2.G, c3.G, offset),
				FMath.InterpolateQuad(c0.B, c1.B, c2.B, c3.B, offset),
				1);
		}

		private static Color4 RegionAtmosphere(int world_x, int world_y) {
			Vector2 offset = new Vector2(
				(world_x % REGION_SIZE) / REGION_SIZE,
				(world_y % REGION_SIZE) / REGION_SIZE);

			Coord origin = new Coord(
				world_x / REGION_SIZE,
				world_y / REGION_SIZE);

			return GetRegion(origin.X, origin.Y).ColorAtmosphere;
		}

		private static float RegionWaterLevel(int world_x, int world_y) {
			world_x += REGION_HALF;
			world_y += REGION_HALF;

			Coord origin = new Coord(
				FMath.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				FMath.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));

			Vector2 offset = new Vector2(
				((world_x) % REGION_SIZE) / REGION_SIZE,
				((world_y) % REGION_SIZE) / REGION_SIZE);

			// Four corners: upper left, upper right, etc.
			Region rul = GetRegion(origin.X, origin.Y);
			Region rur = GetRegion(origin.X + 1, origin.Y);
			Region rbl = GetRegion(origin.X, origin.Y + 1);
			Region rbr = GetRegion(origin.X + 1, origin.Y + 1);
			return FMath.InterpolateQuad(rul.GeoWater, rur.GeoWater, rbl.GeoWater, rbr.GeoWater, offset, ((origin.X + origin.Y) % 2) == 0);
		}

		#region The following functions are used when generating elevation data
		// This modifies the passed elevation value AFTER region cross-fading is complete,
		// For things that should not be mimicked by neighbors. (Like rivers.)
		private static float DoHeightNoBlend(float val, Region r, Vector2 offset, float bias) {
			if ((r.FlagsShape & REGION_FLAG_RIVER_ANY) != 0) {
				Vector2 cen;
				float strength, delta;

				// If this river is strictly north / south
				if (((r.FlagsShape & REGION_FLAG_RIVERNS) != 0) && ((r.FlagsShape & REGION_FLAG_RIVEREW) == 0)) {
					// This makes the river bend side-to-side
					switch ((r.GridPos.X + r.GridPos.Y) % 4) {
						case 0:
							offset.X += Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 1:
							offset.X -= Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 2:
							offset.X += Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f; break;
						case 3:
							offset.X -= Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f; break;
					}
				}

				// If this river is strictly east / west
				if (((r.FlagsShape & REGION_FLAG_RIVEREW) != 0) && ((r.FlagsShape & REGION_FLAG_RIVERNS) == 0)) {
					// This makes the river bend side-to-side
					switch ((r.GridPos.X + r.GridPos.Y) % 4) {
						case 0:
							offset.Y -= Math.Abs(Math.Sin(offset.X * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 1:
							offset.Y += Math.Abs(Math.Sin(offset.X * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 2:
							offset.Y -= Math.Abs(Math.Sin(offset.X * 180.0f * DEGREES_TO_RADIANS)) * 0.10f; break;
						case 3:
							offset.Y += Math.Abs(Math.Sin(offset.X * 180.0f * DEGREES_TO_RADIANS)) * 0.10f; break;
					}
				}

				cen.X = Math.Abs((offset.X - 0.5f) * 2.0f);
				cen.Y = Math.Abs((offset.Y - 0.5f) * 2.0f);
				strength = cen.Length;
				if (((r.FlagsShape & REGION_FLAG_RIVERN) != 0) && offset.Y < 0.5f)
					strength = Math.Min(strength, cen.X);
				if (((r.FlagsShape & REGION_FLAG_RIVERS) != 0) && offset.Y >= 0.5f)
					strength = Math.Min(strength, cen.X);
				if (((r.FlagsShape & REGION_FLAG_RIVERW) != 0) && offset.X < 0.5f)
					strength = Math.Min(strength, cen.Y);
				if (((r.FlagsShape & REGION_FLAG_RIVERE) != 0) && offset.X >= 0.5f)
					strength = Math.Min(strength, cen.Y);
				if (strength < (r.RiverWidth / 2)) {
					strength *= 1.0f / (r.RiverWidth / 2);
					delta = (val - bias) + 4.0f * r.RiverWidth;
					val -= (delta) * (1.0f - strength);
				}
			}
			return val;
		}

		// This takes the given properties and generates a single unit of elevation data, according to the local region rules.
		private static float DoHeight(Region r, Vector2 offset, float bias, float esmall, float elarge) {
			float val;

			// Modify the detail values before they are applied
			if ((r.FlagsShape & REGION_FLAG_CRATER) != 0) {
				if (esmall > 0.5f)
					esmall = 0.5f;
			}
			if ((r.FlagsShape & REGION_FLAG_TIERED) != 0) {
				if (esmall < 0.2f)
					esmall += 0.2f;
				else
					if (esmall < 0.5f)
						esmall -= 0.2f;
			}
			if ((r.FlagsShape & REGION_FLAG_CRACK) != 0) {
				if (esmall > 0.2f && esmall < 0.3f)
					esmall = 0.0f;
			}
			if ((r.FlagsShape & REGION_FLAG_SINKHOLE) != 0) {
				float    x = Math.Abs(offset.X - 0.5f);
				float    y = Math.Abs(offset.Y - 0.5f);
				if (esmall > Math.Max(x, y))
					esmall /= 4.0f;
			}
			// Soften up the banks of a river 
			if ((r.FlagsShape & REGION_FLAG_RIVER_ANY) != 0) {
				Vector2   cen;
				float       strength;

				cen.X = Math.Abs((offset.X - 0.5f) * 2.0f);
				cen.Y = Math.Abs((offset.Y - 0.5f) * 2.0f);
				strength = Math.Min(cen.X, cen.Y);
				strength = Math.Max(strength, 0.2f);
				esmall *= strength;
			}

			elarge *= r.GeoLarge;
			// Apply the values!
			val = esmall * r.GeoDetail + elarge * LARGE_STRENGTH;
			val += bias;
			if (r.Climate == Climate.Swamp) {
				val -= r.GeoDetail / 2.0f;
				val = Math.Max(val, r.GeoWater - 0.5f);
			}
			// Modify the final value.
			if ((r.FlagsShape & REGION_FLAG_MESAS) != 0) {
				float    x = Math.Abs(offset.X - 0.5f) / 5;
				float    y = Math.Abs(offset.Y - 0.5f) / 5;
				if ((esmall + 0.01f) < (x + y)) {
					val += 5;
				}
			}
			if ((r.FlagsShape & REGION_FLAG_CANYON_NS) != 0) {
				float    x = Math.Abs(offset.X - 0.5f) * 2.0f; ;

				if (x + esmall < 0.5f)
					val = bias + (val - bias) / 2.0f;
				else
					val += r.GeoWater;
			}
			if (((r.FlagsShape & REGION_FLAG_BEACH) != 0) && val < r.BeachThreshold && val > 0.0f) {
				val /= r.BeachThreshold;
				val = 1 - val;
				val *= val * val;
				val = 1 - val;
				val *= r.BeachThreshold;
				val -= 0.2f;
			}
			if (((r.FlagsShape & REGION_FLAG_BEACH_CLIFF) != 0) && val < r.BeachThreshold && val > -0.1f) {
				val -= r.BeachThreshold;
			}
			return val;
		}

		private static Region GetRegion(int x, int y) {
			if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
				return Continent[0, 0];
			return Continent[x, y];
		}
		#endregion
		#endregion
	}
}
