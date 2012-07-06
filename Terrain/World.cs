/*-----------------------------------------------------------------------------
  World.cpp
-------------------------------------------------------------------------------
  This holds the region grid, which is the main table of information from 
  which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
  the resulting data is not STORED here. Regions are sets of rules and 
  properties. You crank numbers through them, and it creates the world. 

  This output data is stored and managed elsewhere. (See Page.cs)

  This also holds tables of random numbers.  Basically, everything needed to
  re-create the world should be stored here.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class FWorld {
		#region Enums and structs
		enum Climate {
		  Invalid,		Ocean,		Coast,		Mountain,		River,		RiverBank,		Swamp,		Rocky,		Lake,
			Desert,			Field,		Plains,		Canyon,			Forest,		Types
		}

		//Only one of these is ever instanced.  This is everything that goes into a "save file".
		//UMath.Sing only this, the entire world can be re-created.
		struct World {
			public Region[,] Map { get; private set; }

			public int Seed { get; private set; }
			public int RiverCount { get; private set; }
			public int LakeCount { get; private set; }

			public bool WindFromWest { get; private set; }
			public bool NorthernHemisphere { get; private set; }

			public int[] NoiseInt { get; private set; }
			public float[] NoiseFloat { get; private set; }

			public World(int seed, int riverCount, int lakeCount, bool windFromWest, bool northernHemisphere) {
				Map = new Region[WORLD_GRID, WORLD_GRID];
				NoiseInt = new int[NOISE_BUFFER];
				NoiseFloat = new float[NOISE_BUFFER];

				Seed = seed;
				RiverCount = riverCount;
				LakeCount = lakeCount;
				WindFromWest = windFromWest;
				NorthernHemisphere = northernHemisphere;
			}
		}

		//struct WorldHeader { int version, seed, world_grid, noise_buffer, tree_types, map_bytes; }
		#endregion

		#region Constants, member variables and properties
		private const int
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

			REGION_FLAG_RIVERNS     = (REGION_FLAG_RIVERN | REGION_FLAG_RIVERS),
			REGION_FLAG_RIVEREW     = (REGION_FLAG_RIVERE | REGION_FLAG_RIVERW),
			REGION_FLAG_RIVERNW     = (REGION_FLAG_RIVERN | REGION_FLAG_RIVERW),
			REGION_FLAG_RIVERSE     = (REGION_FLAG_RIVERS | REGION_FLAG_RIVERE),
			REGION_FLAG_RIVERNE     = (REGION_FLAG_RIVERN | REGION_FLAG_RIVERE),
			REGION_FLAG_RIVERSW     = (REGION_FLAG_RIVERS | REGION_FLAG_RIVERW),
			REGION_FLAG_RIVER_ANY   = (REGION_FLAG_RIVERNS | REGION_FLAG_RIVEREW),

			REGION_SIZE             = 128,
			REGION_HALF             = (REGION_SIZE / 2),
			WORLD_GRID              = 256,
			WORLD_GRID_EDGE         = (WORLD_GRID + 1),
			WORLD_GRID_CENTER       = (WORLD_GRID / 2),
			WORLD_SIZE_METERS       = (REGION_SIZE * WORLD_GRID),

			FLOWERS                 = 3,

			// We keep a list of random numbers so we can have deterministic "randomness". This is the size of that list.
			NOISE_BUFFER            = 1024,

			// This is the size of the grid of trees. The total number of tree species in the world
			// is the square of this value, minus one. ("tree zero" is actually "no trees at all".)
			TREE_TYPES              = 6,

			//The dither Map scatters surface data so that grass colorings end up in adjacent regions.
			DITHER_SIZE             = (REGION_SIZE / 2),

			//How much space in a region is spent interpolating between itself and its neighbors.
			BLEND_DISTANCE          = (REGION_SIZE / 4),
			
			FILE_VERSION            = 1;

		private static Coord[,]   dithermap = new Coord[DITHER_SIZE, DITHER_SIZE];
		private static int        map_id;
		private static World      planet;		// THE WHOLE THING!
		private static Tree[,]    tree = new Tree[TREE_TYPES, TREE_TYPES];

		public static int WorldCanopyTree { get; private set; }

		private int WorldMap() {
			return map_id;
		}
		#endregion

		#region Methods
		#region The following functions are used when generating elevation data
		// This modifies the passed elevation value AFTER region cross-fading is complete,
		// For things that should not be mimicked by neighbors. (Like rivers.)
		public static float DoHeightNoBlend(float val, Region r, Vector2 offset, float water) {
			if ((r.FlagsShape & REGION_FLAG_RIVER_ANY) != 0) {
				// If this river is strictly north / south
				if (((r.FlagsShape & REGION_FLAG_RIVERNS) != 0) && ((r.FlagsShape & REGION_FLAG_RIVEREW) == 0)) {
					// This makes the river bend side-to-side
					switch ((r.GridPos.X + r.GridPos.Y) % 6) {
						case 0:
							offset.X += Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 1:
							offset.X -= Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f; break;
						case 2:
							offset.X += Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f; break;
						case 3:
							offset.X -= Math.Abs(Math.Sin(offset.Y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f; break;
						case 4:
							offset.X += Math.Sin(offset.Y * 360.0f * DEGREES_TO_RADIANS) * 0.1f; break;
						case 5:
							offset.X += Math.Sin(offset.Y * 360.0f * DEGREES_TO_RADIANS) * 0.1f; break;
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

				// If this river curves around a bend
				if (((r.FlagsShape & REGION_FLAG_RIVERNW) != 0) && ((r.FlagsShape & REGION_FLAG_RIVERSE) == 0))
					offset.X = offset.Y = offset.Length;
				if (((r.FlagsShape & REGION_FLAG_RIVERSE) != 0) && ((r.FlagsShape & REGION_FLAG_RIVERNW) == 0)) {
					Vector2 new_off = new Vector2(
						1.0f - offset.X,
						1.0f - offset.Y);
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}

				if (((r.FlagsShape & REGION_FLAG_RIVERNE) != 0) && ((r.FlagsShape & REGION_FLAG_RIVERSW) == 0)) {
					Vector2 new_off = new Vector2(
						1.0f - offset.X,
						offset.Y);
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}

				if (((r.FlagsShape & REGION_FLAG_RIVERSW) != 0) && ((r.FlagsShape & REGION_FLAG_RIVERNE) == 0)) {
					Vector2 new_off = new Vector2(
						offset.X,
						1.0f - offset.Y);
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}

				Vector2 cen = new Vector2(
					Math.Abs((offset.X - 0.5f) * 2.0f),
					Math.Abs((offset.Y - 0.5f) * 2.0f));
				float strength = cen.Length;

				if (((r.FlagsShape & REGION_FLAG_RIVERN) != 0) && (offset.Y < 0.5f))
					strength = Math.Min(strength, cen.X);
				if (((r.FlagsShape & REGION_FLAG_RIVERS) != 0) && (offset.Y >= 0.5f))
					strength = Math.Min(strength, cen.X);
				if (((r.FlagsShape & REGION_FLAG_RIVERW) != 0) && (offset.X < 0.5f))
					strength = Math.Min(strength, cen.Y);
				if (((r.FlagsShape & REGION_FLAG_RIVERE) != 0) && (offset.X >= 0.5f))
					strength = Math.Min(strength, cen.Y);

				if (strength < (r.RiverWidth / 2)) {
					strength *= 1.0f / (r.RiverWidth / 2);
					float delta = (val - water) + 4.0f * r.RiverWidth;
					val -= (delta) * (1.0f - strength);
				}
			}
			return val;
		}

		// This takes the given properties and generates a Math.Single unit of elevation data, according to the local region rules.
		// Water is the water level.  Detail is the height of the rolling hills. Bias is a direct height added on to these.
		public static float DoHeight(Region r, Vector2 offset, float water, float detail, float bias) {
			// Modify the detail values before they are applied
			if ((r.FlagsShape & REGION_FLAG_CRATER) != 0) {
				if (detail > 0.5f)
					detail = 0.5f;
			}
			if ((r.FlagsShape & REGION_FLAG_TIERED) != 0) {
				if (detail < 0.2f)
					detail += 0.2f;
				else
					if (detail < 0.5f)
						detail -= 0.2f;
			}
			if ((r.FlagsShape & REGION_FLAG_CRACK) != 0) {
				if (detail > 0.2f && detail < 0.3f)
					detail = 0.0f;
			}
			if ((r.FlagsShape & REGION_FLAG_SINKHOLE) != 0) {
				float    x = Math.Abs(offset.X - 0.5f);
				float    y = Math.Abs(offset.Y - 0.5f);
				if (detail > Math.Max(x, y))
					detail /= 4.0f;
			}

			// Soften up the banks of a river 
			if ((r.FlagsShape & REGION_FLAG_RIVER_ANY) != 0) {
				Vector2 cen = new Vector2(
					Math.Abs((offset.X - 0.5f) * 2.0f),
					Math.Abs((offset.Y - 0.5f) * 2.0f));
				float strength = Math.Min(cen.X, cen.Y);
				strength = Math.Max(strength, 0.1f);
				detail *= strength;
			}

			// Apply the values!
			float val = water + detail * r.GeoDetail + bias;
			if (r.Climate == Climate.Swamp) {
				val -= r.GeoDetail / 2.0f;
				val = Math.Max(val, r.GeoWater - 0.5f);
			}

			// Modify the final value.
			if ((r.FlagsShape & REGION_FLAG_MESAS) != 0) {
				float x = Math.Abs(offset.X - 0.5f) / 5;
				float y = Math.Abs(offset.Y - 0.5f) / 5;
				if ((detail + 0.01f) < (x + y)) {
					val += 5;
				}
			}
			if ((r.FlagsShape & REGION_FLAG_CANYON_NS) != 0) {
				float    x = Math.Abs(offset.X - 0.5f) * 2.0f; ;
				if (x + detail < 0.5f)
					val -= Math.Min(r.GeoDetail, 10.0f);
			}
			if (((r.FlagsShape & REGION_FLAG_BEACH) != 0) && (val < r.cliff_threshold) && (val > 0)) {
				val /= r.cliff_threshold;
				val *= val;
				val *= r.cliff_threshold;
				val += 0.2f;
			}

			if (((r.FlagsShape & REGION_FLAG_BEACH_CLIFF) != 0) && (val < r.cliff_threshold) && (val > -0.1f)) {
				val -= Math.Min(r.cliff_threshold, 10.0f);
			}
			// If a point dips below the water table, make sure it's not too close to the water, to avoid ugly z-fighting
			//if (val < bias)
			//val = Math.Min (val, bias - 2.5f);
			return val;
		}

		public static void BuildTrees() {
			int rotator = 0;
			for (int m = 0; m < TREE_TYPES; m++) {
				for (int t = 0; t < TREE_TYPES; t++) {
					bool is_canopy;
					if ((m == TREE_TYPES / 2) && (t == TREE_TYPES / 2)) {
						is_canopy = true;
						WorldCanopyTree = m + t * TREE_TYPES;
					} else
						is_canopy = false;
					tree[m, t].Create(is_canopy, (float) m / TREE_TYPES, (float) t / TREE_TYPES, rotator++);
				}
			}
		}

		public static void BuildMapTexture() {
			if (map_id == 0)
				GL.GenTextures (1, out map_id);
			GL.BindTexture(TextureTarget.Texture2D, map_id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			
			int[] pixels = new int[WORLD_GRID * WORLD_GRID * 3];

			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					//Flip it vertically, because the OpenGL texture coord system is retarded.
					int yy = (WORLD_GRID - 1) - y;
					Region r = planet.Map[x, yy];
					int i = (x + y * WORLD_GRID) * 3;
					pixels[i]			= (int) (r.ColorMap.R * 255.0f);
					pixels[i + 1] = (int) (r.ColorMap.G * 255.0f);
					pixels[i + 2] = (int) (r.ColorMap.B * 255.0f);
				}
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, WORLD_GRID, WORLD_GRID, 0, PixelFormat.Rgb, PixelType.Byte, pixels);
		}
		#endregion

		public static float NoiseFloat(int index) { return planet.NoiseFloat[Math.Abs(index % NOISE_BUFFER)]; }
		public static int NoiseInt(int index) { return planet.NoiseInt[Math.Abs(index % NOISE_BUFFER)]; }
		
		private float WorldWaterLevel(int world_x, int world_y) {
			world_x += REGION_HALF;
			world_y += REGION_HALF;
			
			Coord origin = new Coord(
				FMath.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				FMath.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));

			Vector2 offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);

			// Four corners: upper left, upper right, etc.
			Region rul = RegionGet(origin.X, origin.Y);
			Region rur = RegionGet(origin.X + 1, origin.Y);
			Region rbl = RegionGet(origin.X, origin.Y + 1);
			Region rbr = RegionGet(origin.X + 1, origin.Y + 1);

			return FMath.InterpolateQuad(rul.GeoWater, rur.GeoWater, rbl.GeoWater, rbr.GeoWater, offset, ((origin.X + origin.Y) % 2) == 0);
		}

		private float WorldBiasLevel(int world_x, int world_y) {
			world_x += REGION_HALF;
			world_y += REGION_HALF;

			Coord origin = new Coord(
				FMath.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				FMath.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));

			Vector2 offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);

			// Four corners: upper left, upper right, etc.
			Region rul = RegionGet(origin.X, origin.Y);
			Region rur = RegionGet(origin.X + 1, origin.Y);
			Region rbl = RegionGet(origin.X, origin.Y + 1);
			Region rbr = RegionGet(origin.X + 1, origin.Y + 1);

			return FMath.InterpolateQuad(rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset, ((origin.X + origin.Y) % 2) == 0);
		}

		private Cell WorldCell(int world_x, int world_y) {
			float detail = Entropy(world_x, world_y);
			float bias = WorldBiasLevel(world_x, world_y);
			float water = WorldWaterLevel(world_x, world_y);

			Coord origin = new Coord(
				FMath.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				FMath.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));

			// Get our offset from the region origin as a pair of scalars.
			Vector2 blend = new Vector2(
				(float) (world_x % BLEND_DISTANCE) / BLEND_DISTANCE,
				(float) (world_y % BLEND_DISTANCE) / BLEND_DISTANCE);

			bool left = ((origin.X + origin.Y) % 2) == 0;

			Vector2 offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);

			Cell result;
			result.detail = detail;
			result.water_level = water;

			//Upper left and bottom-right corners
			Coord ul = new Coord(origin);

			Coord br = new Coord(
				(world_x + BLEND_DISTANCE) / REGION_SIZE,
				(world_y + BLEND_DISTANCE) / REGION_SIZE);

			if (ul == br) {
				rul = RegionGet(ul.X, ul.Y);
				result.elevation = DoHeight(rul, offset, water, detail, bias);
				result.elevation = DoHeightNoBlend(result.elevation, rul, offset, water);
				return result;
			}

			// Four corners: upper left, upper right, etc.
			Region rul = RegionGet(ul.X, ul.Y);
			Region rur = RegionGet(br.X, ul.Y);
			Region rbl = RegionGet(ul.X, br.Y);
			Region rbr = RegionGet(br.X, br.Y);

			float eul = DoHeight(rul, offset, water, detail, bias);
			float eur = DoHeight(rur, offset, water, detail, bias);
			float ebl = DoHeight(rbl, offset, water, detail, bias);
			float ebr = DoHeight(rbr, offset, water, detail, bias);

			result.elevation = FMath.InterpolateQuad(eul, eur, ebl, ebr, blend, left);
			result.elevation = DoHeightNoBlend(result.elevation, rul, offset, water);
			return result;
		}

		private int WorldTreeType(float moisture, float temperature) {
			int m = FMath.Clamp((int) (moisture * TREE_TYPES), 0, TREE_TYPES - 1);
			int t = FMath.Clamp((int) (temperature * TREE_TYPES), 0, TREE_TYPES - 1);

			return m + t * TREE_TYPES;
		}

		private Tree WorldTree(int id) {
			int m = id % TREE_TYPES;
			int t = (id - m) / TREE_TYPES;

			return tree[m, t];
		}

		private string WorldLocationName(int world_x, int world_y) {
			world_x /= REGION_SIZE;
			world_y /= REGION_SIZE;
			world_x -= WORLD_GRID_CENTER;
			world_y -= WORLD_GRID_CENTER;

			if ((world_x == 0) && (world_y == 0))
				return "Equatorial meridian";

			string lng;
			if (world_x == 0)				lng = "meridian";
			else if (world_x < 0)		lng = Math.Abs(world_x).ToString() + " west";
			else										lng = world_x.ToString() + " east";

			string lat;
			if (world_y == 0)				lat = "Equator";
			else if (world_y < 0)		lat = Math.Abs(world_y).ToString() + " north";
			else										lat = world_y.ToString() + " south";

			return lat + ' ' + lng;
		}

		private void WorldInit() {
			// Fill in the dither table - a table of random offsets
			for (int y = 0; y < DITHER_SIZE; y++) {
				for (int x = 0; x < DITHER_SIZE; x++) {
					dithermap[x, y].X = Random.Value() % DITHER_SIZE + Random.Value() % DITHER_SIZE;
					dithermap[x, y].Y = Random.Value() % DITHER_SIZE + Random.Value() % DITHER_SIZE;
				}
			}
		}

		private void WorldSave() {
			//FILE*     f;
			//char      filename[256];
			//WorldHeader   header;

			//return;
			//sprintf (filename, "%sworld.sav", GameDirectory ());
			//if (!(f = fopen (filename, "wb"))) {
			//  ConsoleLog ("WorldSave: Could not open file %s", filename);
			//  return;
			//}
			//header.version = FILE_VERSION;
			//header.Seed = planet.Seed;
			//header.world_grid = WORLD_GRID;
			//header.noise_buffer = NOISE_BUFFER;
			//header.map_bytes = sizeof (planet);
			//header.tree_types = TREE_TYPES;
			//fwrite (&header, sizeof (header), 1, f);
			//fwrite (&planet, sizeof (planet), 1, f);
			//fclose (f);
			//ConsoleLog ("WorldSave: '%s' saved.", filename);
		}

		private void WorldLoad(int seed_in) {
			//FILE     f;
			//char      filename[256];
			//WorldHeader   header;

			//sprintf (filename, "%sworld.sav", GameDirectory ());
			//if (!(f = fopen (filename, "rb"))) {
			//  ConsoleLog ("WorldLoad: Could not open file %s", filename);
			//  Generate (seedIn);
			//  return;
			//}
			//fread (&header, sizeof (header), 1, f);
			//fread (&planet, sizeof (planet), 1, f);
			//fclose (f);
			////ConsoleLog ("WorldLoad: '%s' loaded.", filename);
			//BuildTrees ();
			//BuildMapTexture ();
		}

		private void Generate(int seedIn) {
			Random.Init(seedIn);
			BuildTrees();

			planet = new World(
				seedIn,
				4 + Random.Value() % 4,			// RiverCount
				1 + Random.Value() % 4,			// LakeCount
				(Random.Value() % 2) != 0,	// WindFromWest
				(Random.Value() % 2) != 0);	// NorthernHemisphere

			for (int x = 0; x < NOISE_BUFFER; x++) {
				planet.NoiseInt[x] = Random.Value();
				planet.NoiseFloat[x] = Random.Float();
			}
		
			TerraformPrepare();
			TerraformOceans();
			TerraformCoast();
			TerraformClimate();
			TerraformRivers(planet.RiverCount);
			TerraformLakes(planet.LakeCount);
			TerraformClimate(); // Do climate a second time now that rivers are in
			TerraformZones();
			TerraformClimate(); // Now again, since we have added climate-modifying features (Mountains, etc.)
			TerraformFill();
			TerraformAverage();
			TerraformFlora();
			TerraformColors();
			BuildMapTexture();
		}

		private Region RegionGet(int index_x, int index_y) {
			return planet.Map[index_x][index_y];
		}

		private void RegionSet(int index_x, int index_y, Region val) {
			planet.Map[index_x][index_y] = val;
		}

		public static Region RegionFromPosition(int world_x, int world_y) {
			world_x = Math.Max(world_x, 0);
			world_y = Math.Max(world_y, 0);
			world_x += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].X;
			world_y += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].Y;
			world_x /= REGION_SIZE;
			world_y /= REGION_SIZE;
			if (world_x >= WORLD_GRID || world_y >= WORLD_GRID)
				return planet.Map[0, 0];
			return planet.Map[world_x, world_y];
		}

		public static Region RegionFromPosition(float world_x, float world_y) {
			return RegionFromPosition((int) world_x, (int) world_y);
		}

		private Color4 ColorGet(int world_x, int world_y, SurfaceColor c) {
			int x = Math.Max(world_x % DITHER_SIZE, 0);
			int y = Math.Max(world_y % DITHER_SIZE, 0);

			world_x += dithermap[x, y].X;
			world_y += dithermap[x, y].Y;

			Vector2 offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);

			Coord origin = new Coord(
				world_x / REGION_SIZE,
				world_y / REGION_SIZE);

			Region r0 = RegionGet(origin.X, origin.Y);
			Region r1 = RegionGet(origin.X + 1, origin.Y);
			Region r2 = RegionGet(origin.X, origin.Y + 1);
			Region r3 = RegionGet(origin.X + 1, origin.Y + 1);

			Color4 c0, c1, c2, c3;
			switch (c) {
				case SurfaceColor.Dirt:
					c0 = r0.ColorDirt;
					c1 = r1.ColorDirt;
					c2 = r2.ColorDirt;
					c3 = r3.ColorDirt;
					break;
				case SurfaceColor.Rock:
					c0 = r0.ColorRock;
					c1 = r1.ColorRock;
					c2 = r2.ColorRock;
					c3 = r3.ColorRock;
					break;
				case SurfaceColor.Sand:
					return new Color4(0.98f, 0.82f, 0.42f, 1);
				case SurfaceColor.Grass:
				default:
					c0 = r0.ColorGrass;
					c1 = r1.ColorGrass;
					c2 = r2.ColorGrass;
					c3 = r3.ColorGrass;
					break;
			}
			return new Color4(
				FMath.InterpolateQuad(c0.R, c1.R, c2.R, c3.R, offset),
				FMath.InterpolateQuad(c0.G, c1.G, c2.G, c3.G, offset),
				FMath.InterpolateQuad(c0.B, c1.B, c2.B, c3.B, offset),
				1);
		}

		private void TexturePurge() {
			for (int m = 0; m < TREE_TYPES; m++)
				for (int t = 0; t < TREE_TYPES; t++)
					tree[m, t].TexturePurge();

			BuildMapTexture();
		}

		public string DirectionFromAngle(float angle) {
			string direction = "North";
			if (angle < 22.5f)				direction = "North";
			else if (angle < 67.5f)		direction = "Northwest";
			else if (angle < 112.5f)	direction = "West";
			else if (angle < 157.5f)	direction = "Southwest";
			else if (angle < 202.5f)	direction = "South";
			else if (angle < 247.5f)	direction = "Southeast";
			else if (angle < 292.5f)	direction = "East";
			else if (angle < 337.5f)	direction = "Northeast";
			return direction;
		}

		/* char* WorldDirectory () {
			static char     dir[32];

			sprintf (dir, "saves//Seed%d//", planet.Seed);
			return dir;
		}
		*/
		#endregion
	}
}
