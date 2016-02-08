namespace terrain_sharp.Source.World {
	using System;

	using MathNet.Numerics.Random;

	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.OpenGL;

	using CTree;
	using GLTypes;
	using StdAfx;
	using Utils;

	///<summary>
	///	This holds the region grid, which is the main table of information from
	///	which ALL OTHER GEOGRAPHICAL DATA is generated or derived.Note that
	///	the resulting data is not STORED here.Regions are sets of rules and
	///	properties.You crank numbers through them, and it creates the world.
	///
	///	This output data is stored and managed elsewhere. (See CPage.cs)
	///
	///	This also holds tables of random numbers.Basically, everything needed to
	///	re-create the world should be stored here.
	///</summary>
	///<remarks>
	///	Only one of these is ever instanced.  This is everything that goes into a "save file".
	///	Using only this, the entire world can be re-created.
	///</remarks>
	partial class World {
		#region Constants
		public const int FLOWERS = 3;
		///<summary>
		///	We keep a list of random numbers so we can have deterministic "randomness". 
		///	This is the size of that list.
		///</summary>
		public const int NOISE_BUFFER = 1024;
		///<summary>
		///	This is the size of the grid of trees. The total number of tree species 
		///	in the world is the square of this value, minus one. ("tree zero" is actually
		///	"no trees at all".)
		///</summary>
		public const int TREE_TYPES = 6;
		public const int REGION_SIZE = 128;
		public const int REGION_HALF = (REGION_SIZE / 2);
		public const int WORLD_GRID = 256;
		public const int WORLD_GRID_EDGE = (WORLD_GRID + 1);
		public const int WORLD_GRID_CENTER = (WORLD_GRID / 2);
		public const int WORLD_SIZE_METERS = (REGION_SIZE * WORLD_GRID);
		///<summary>The dither map scatters surface data so that grass colorings end up in adjacent regions.</summary>
		public const int DITHER_SIZE = (REGION_SIZE / 2);
		///<summary>How much space in a region is spent interpolating between itself and its neighbors.</summary>
		public const int BLEND_DISTANCE = (REGION_SIZE / 4);
		public const int FILE_VERSION = 1;
		#endregion

		private static readonly World instance = new World();
		private World() { }
		public static World Instance { get { return instance; } }

		public int CanopyTree { get; private set; }
		public int Map { get; private set; }

		private CTree[,] tree = new CTree[TREE_TYPES, TREE_TYPES];
		private Coord[,] dithermap = new Coord[DITHER_SIZE, DITHER_SIZE];

		private int seed;
		private bool windFromWest;
		private bool northernHemisphere;
		private int riverCount;
		private int lakeCount;
		private float[] noiseFloat = new float[NOISE_BUFFER];
		private int[] noiseInt = new int[NOISE_BUFFER];

		private Region[,] map = new Region[WORLD_GRID, WORLD_GRID];
		public Region GetRegion(int index_x, int index_y) { return map[index_x, index_y]; }
		public void SetRegion(int index_x, int index_y, Region val) { map[index_x, index_y] = val; }

		#region The following functions are used when generating elevation data
		///<summary>
		///	This modifies the passed elevation value AFTER region cross-fading is complete,
		///	For things that should not be mimicked by neighbors. (Like rivers.)
		///</summary>
		private float DoHeightNoBlend(float val, Region r, Vector2 offset, float water) {
			//return val;
			if (r.FlagsShape.HasFlag(RegionFlag.RiverAny)) {
				//if this river is strictly north / south
				if (r.FlagsShape.HasFlag(RegionFlag.RiverNS) && !r.FlagsShape.HasFlag(RegionFlag.RiverEW)) {
					//This makes the river bend side-to-side
					switch ((r.grid_pos.X + r.grid_pos.Y) % 6) {
						case 0:
							offset.X += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.25f;
							break;
						case 1:
							offset.X -= (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.25f;
							break;
						case 2:
							offset.X += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.1f;
							break;
						case 3:
							offset.X -= (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.1f;
							break;
						case 4:
							offset.X += (float) Math.Sin(MathHelper.DegreesToRadians(offset.Y * 360)) * 0.1f;
							break;
						case 5:
							offset.X += (float) Math.Sin(MathHelper.DegreesToRadians(offset.Y * 360)) * 0.1f;
							break;
					}
				}
				//if this river is strictly east / west
				if (r.FlagsShape.HasFlag(RegionFlag.RiverEW) && !r.FlagsShape.HasFlag(RegionFlag.RiverNS)) {
					//This makes the river bend side-to-side
					switch ((r.grid_pos.X + r.grid_pos.Y) % 4) {
						case 0:
							offset.Y -= (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.25f;
							break;
						case 1:
							offset.Y += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.25f;
							break;
						case 2:
							offset.Y -= (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.10f;
							break;
						case 3:
							offset.Y += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.10f;
							break;
					}
				}
				//if this river curves around a bend
				if (r.FlagsShape.HasFlag(RegionFlag.RiverNW) && !r.FlagsShape.HasFlag(RegionFlag.RiverSE))
					offset.X = offset.Y = offset.Length;
				var new_off = new Vector2();
				if (r.FlagsShape.HasFlag(RegionFlag.RiverSE) && !r.FlagsShape.HasFlag(RegionFlag.RiverNW)) {
					new_off.X = 1.0f - offset.X;
					new_off.Y = 1.0f - offset.Y;
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}
				if (r.FlagsShape.HasFlag(RegionFlag.RiverNE) && !r.FlagsShape.HasFlag(RegionFlag.RiverSW)) {
					new_off.X = 1.0f - offset.X;
					new_off.Y = offset.Y;
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}
				if (r.FlagsShape.HasFlag(RegionFlag.RiverSW) && !r.FlagsShape.HasFlag(RegionFlag.RiverNE)) {
					new_off.X = offset.X;
					new_off.Y = 1.0f - offset.Y;
					new_off.X = new_off.Y = new_off.Length;
					offset = new_off;
				}
				var cen = new Vector2(
					Math.Abs((offset.X - 0.5f) * 2.0f),
					Math.Abs((offset.Y - 0.5f) * 2.0f));
				float strength = cen.Length;
				if (r.FlagsShape.HasFlag(RegionFlag.RiverN) && offset.Y < 0.5f)
					strength = Math.Min(strength, cen.X);
				if (r.FlagsShape.HasFlag(RegionFlag.RiverS) && offset.Y >= 0.5f)
					strength = Math.Min(strength, cen.X);
				if (r.FlagsShape.HasFlag(RegionFlag.RiverW) && offset.X < 0.5f)
					strength = Math.Min(strength, cen.Y);
				if (r.FlagsShape.HasFlag(RegionFlag.RiverE) && offset.X >= 0.5f)
					strength = Math.Min(strength, cen.Y);
				if (strength < (r.river_width / 2)) {
					strength *= 1.0f / (r.river_width / 2);
					float delta = (val - water) + 4.0f * r.river_width;
					val -= (delta) * (1.0f - strength);
				}
			}
			return val;
		}

		///<summary>
		///	This takes the given properties and generates a Math.Single unit of elevation data,
		///	according to the local region rules.
		///	Water is the water level.  Detail is the height of the rolling hills. Bias
		///	is a direct height added on to these.
		///</summary>
		private float DoHeight(Region r, Vector2 offset, float water, float detail, float bias) {
			//Modify the detail values before they are applied
			if (r.FlagsShape.HasFlag(RegionFlag.Crater)) {
				if (detail > 0.5f)
					detail = 0.5f;
			}
			if (r.FlagsShape.HasFlag(RegionFlag.Tiered)) {
				if (detail < 0.2f)
					detail += 0.2f;
				else
				if (detail < 0.5f)
					detail -= 0.2f;
			}
			if (r.FlagsShape.HasFlag(RegionFlag.Crack)) {
				if (detail > 0.2f && detail < 0.3f)
					detail = 0.0f;
			}
			if (r.FlagsShape.HasFlag(RegionFlag.Sinkhole)) {
				float x = Math.Abs(offset.X - 0.5f);
				float y = Math.Abs(offset.Y - 0.5f);
				if (detail > Math.Max(x, y))
					detail /= 4.0f;
			}

			//Soften up the banks of a river 
			if (r.FlagsShape.HasFlag(RegionFlag.RiverAny)) {
				var cen = new Vector2(
					Math.Abs((offset.X - 0.5f) * 2.0f),
					Math.Abs((offset.Y - 0.5f) * 2.0f));
				float strength = Math.Max(Math.Min(cen.X, cen.Y), 0.1f);
				detail *= strength;
			}

			//Apply the values!
			float val = water + detail * r.geo_detail + bias;
			if (r.climate == Climate.Swamp) {
				val -= r.geo_detail / 2.0f;
				val = Math.Max(val, r.geo_water - 0.5f);
			}
			//Modify the final value.
			if (r.FlagsShape.HasFlag(RegionFlag.Mesas)) {
				float x = Math.Abs(offset.X - 0.5f) / 5;
				float y = Math.Abs(offset.Y - 0.5f) / 5;
				if ((detail + 0.01f) < (x + y)) {
					val += 5;
				}
			}
			if (r.FlagsShape.HasFlag(RegionFlag.CanyonNS)) {
				float x = Math.Abs(offset.X - 0.5f) * 2.0f;
				if (x + detail < 0.5f)
					val -= Math.Min(r.geo_detail, 10.0f);
			}
			if (r.FlagsShape.HasFlag(RegionFlag.Beach) && val < r.cliff_threshold && val > 0.0f) {
				val /= r.cliff_threshold;
				val *= val;
				val *= r.cliff_threshold;
				val += 0.2f;
			}
			if (r.FlagsShape.HasFlag(RegionFlag.BeachCliff) && val < r.cliff_threshold && val > -0.1f) {
				val -= Math.Min(r.cliff_threshold, 10.0f);
			}
			//if a point dips below the water table, make sure it's not too close to the water,
			//to avoid ugly z-fighting
			//if (val < bias)
			//val = Math.Min (val, bias - 2.5f);
			return val;
		}

		private void BuildTrees() {
			int rotator = 0;
			for (int m = 0; m < TREE_TYPES; m++) {
				for (int t = 0; t < TREE_TYPES; t++) {
					bool is_canopy;
					if ((m == TREE_TYPES / 2) && (t == TREE_TYPES / 2)) {
						is_canopy = true;
						CanopyTree = m + t * TREE_TYPES;
					} else
						is_canopy = false;
					tree[m, t].Create(is_canopy, (float) m / TREE_TYPES, (float) t / TREE_TYPES, rotator++);
				}
			}
		}

		private void BuildMapTexture() {
			if (Map == 0)
				Map = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, Map);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);

			byte[] buffer = new byte[WORLD_GRID * WORLD_GRID * 3];
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					//Flip it vertically, because the OpenGL texture coord system is retarded.
					int yy = (WORLD_GRID - 1) - y;
					Region r = map[x, yy];
					int pos = (x + y * WORLD_GRID) * 3;
					buffer[pos] = (byte) (r.color_map.R * 255.0f);
					buffer[pos + 1] = (byte) (r.color_map.G * 255.0f);
					buffer[pos + 2] = (byte) (r.color_map.B * 255.0f);
				}
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, WORLD_GRID, WORLD_GRID, 0,
				PixelFormat.Rgb, PixelType.Byte, buffer);
		}
		#endregion

		public float WaterLevel(int world_x, int world_y) {
			world_x += REGION_HALF;
			world_y += REGION_HALF;
			var origin = new Coord(
				MathHelper.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				MathHelper.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));
			var offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);
			//Four corners: upper left, upper right, etc.
			Region rul = GetRegion(origin.X, origin.Y);
			Region rur = GetRegion(origin.X + 1, origin.Y);
			Region rbl = GetRegion(origin.X, origin.Y + 1);
			Region rbr = GetRegion(origin.X + 1, origin.Y + 1);
			return MathUtils.InterpolateQuad(rul.geo_water, rur.geo_water, rbl.geo_water, rbr.geo_water, offset, ((origin.X + origin.Y) % 2) == 0);
		}

		public float BiasLevel(int world_x, int world_y) {
			world_x += REGION_HALF;
			world_y += REGION_HALF;
			var origin = new Coord(
				MathHelper.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				MathHelper.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));
			var offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);
			//Four corners: upper left, upper right, etc.
			Region rul = GetRegion(origin.X, origin.Y);
			Region rur = GetRegion(origin.X + 1, origin.Y);
			Region rbl = GetRegion(origin.X, origin.Y + 1);
			Region rbr = GetRegion(origin.X + 1, origin.Y + 1);
			return MathUtils.InterpolateQuad(rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset, ((origin.X + origin.Y) % 2) == 0);
		}

		public Cell GetCell(int world_x, int world_y) {
			float detail = Entropy(world_x, world_y);
			float bias = BiasLevel(world_x, world_y);
			float water = WaterLevel(world_x, world_y);
			var origin = new Coord(
				MathHelper.Clamp(world_x / REGION_SIZE, 0, WORLD_GRID - 1),
				MathHelper.Clamp(world_y / REGION_SIZE, 0, WORLD_GRID - 1));
			bool left = ((origin.X + origin.Y) % 2) == 0;
			//Get our offset from the region origin as a pair of scalars.
			var blend = new Vector2(
				(float) (world_x % BLEND_DISTANCE) / BLEND_DISTANCE,
				(float) (world_y % BLEND_DISTANCE) / BLEND_DISTANCE);
			var offset = new Vector2(
				(float) ((world_x) % REGION_SIZE) / REGION_SIZE,
				(float) ((world_y) % REGION_SIZE) / REGION_SIZE);
			var result = new Cell();
			result.Detail = detail;
			result.WaterLevel = water;

			Region rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.
			float eul, eur, ebl, ebr;

			//Upper left and bottom-right corners
			var ul = new Coord(origin);
			var br = new Coord(
				(world_x + BLEND_DISTANCE) / REGION_SIZE,
				(world_y + BLEND_DISTANCE) / REGION_SIZE);

			if (ul == br) {
				rul = GetRegion(ul.X, ul.Y);
				result.Elevation = DoHeight(rul, offset, water, detail, bias);
				result.Elevation = DoHeightNoBlend(result.Elevation, rul, offset, water);
				return result;
			}
			rul = GetRegion(ul.X, ul.Y);
			rur = GetRegion(br.X, ul.Y);
			rbl = GetRegion(ul.X, br.Y);
			rbr = GetRegion(br.X, br.Y);

			eul = DoHeight(rul, offset, water, detail, bias);
			eur = DoHeight(rur, offset, water, detail, bias);
			ebl = DoHeight(rbl, offset, water, detail, bias);
			ebr = DoHeight(rbr, offset, water, detail, bias);
			result.Elevation = MathUtils.InterpolateQuad(eul, eur, ebl, ebr, blend, left);
			result.Elevation = DoHeightNoBlend(result.Elevation, rul, offset, water);
			return result;
		}

		public int TreeType(float moisture, float temperature) {
			int m = (int) (moisture * TREE_TYPES);
			int t = (int) (temperature * TREE_TYPES);
			m = MathHelper.Clamp(m, 0, TREE_TYPES - 1);
			t = MathHelper.Clamp(t, 0, TREE_TYPES - 1);
			return m + t * TREE_TYPES;
		}

		public CTree Tree(int id) {
			int m = id % TREE_TYPES;
			int t = (id - m) / TREE_TYPES;
			return tree[m, t];
		}

		public string LocationName(int world_x, int world_y) {
			world_x /= REGION_SIZE;
			world_y /= REGION_SIZE;
			world_x -= WORLD_GRID_CENTER;
			world_y -= WORLD_GRID_CENTER;

			if (world_x == 0 && world_y == 0)
				return "Equatorial meridian";

			string lng;
			if (world_x == 0)
				lng = "meridian";
			else if (world_x < 0)
				lng = Math.Abs(world_x).Tostring() + " west";
			else
				lng = world_x.Tostring() + " east";

			string lat;
			if (world_y == 0)
				lat = "Equator";
			else if (world_y < 0)
				lat = Math.Abs(world_y).Tostring() + " north";
			else
				lat = world_y.Tostring() + " south";

			return lat + ", " + lng;
		}

		public void Init() {
			//Fill in the dither table - a table of random offsets
			seed = RandomSeed.Time();
			var random = new MersenneTwister(seed);
			for (int y = 0; y < DITHER_SIZE; y++) {
				for (int x = 0; x < DITHER_SIZE; x++) {
					dithermap[x, y].X = random.Next(DITHER_SIZE) + random.Next(DITHER_SIZE);
					dithermap[x, y].Y = random.Next(DITHER_SIZE) + random.Next(DITHER_SIZE);
				}
			}
		}

		public float NoiseFloat(int index) {
			index = Math.Abs(index % NOISE_BUFFER);
			return noiseFloat[index];
		}

		public int NoiseInt(int index) {
			index = Math.Abs(index % NOISE_BUFFER);
			return noiseInt[index];
		}

		public void Save() {
			//FILE* f;
			//WHeader header;

			//return;

			//string filename = Path.Combine(GameDirectory(), "world.sav");
			//if (!(f = fopen(filename, "wb"))) {
			//	ConsoleLog("WorldSave: Could not open file %s", filename);
			//	return;
			//}
			//header.version = FILE_VERSION;
			//header.seed = planet.seed;
			//header.world_grid = WORLD_GRID;
			//header.noise_buffer = NOISE_BUFFER;
			//header.map_bytes = sizeof(planet);
			//header.tree_types = TREE_TYPES;
			//fwrite(&header, sizeof(header), 1, f);
			//fwrite(&planet, sizeof(planet), 1, f);
			//fclose(f);
			//ConsoleLog("WorldSave: '%s' saved.", filename);
		}

		public void Load(int seed_in) {
			//FILE* f;
			//char filename[256];
			//WHeader header;

			//sprintf(filename, "%sworld.sav", GameDirectory());
			//if (!(f = fopen(filename, "rb"))) {
			//	ConsoleLog("WorldLoad: Could not open file %s", filename);
			//	Generate(seed_in);
			//	return;
			//}
			//fread(&header, sizeof(header), 1, f);
			//fread(&planet, sizeof(planet), 1, f);
			//fclose(f);
			//ConsoleLog("WorldLoad: '%s' loaded.", filename);
			//build_trees();
			//build_map_texture();
		}

		public void Generate(int seed_in) {
			var random = new MersenneTwister(seed_in);
			seed = seed_in;

			for (int x = 0; x < NOISE_BUFFER; x++) {
				noiseInt[x] = random.Next();
				noiseFloat[x] = (float) random.NextDouble();
			}
			BuildTrees();
			windFromWest = random.NextBoolean();
			northernHemisphere = random.NextBoolean();
			riverCount = random.Next(4, 7);
			lakeCount = random.Next(1, 4);
			TerraformPrepare();
			TerraformOceans();
			TerraformCoast();
			TerraformClimate();
			TerraformRivers(riverCount);
			TerraformLakes(lakeCount);
			TerraformClimate();//Do climate a second time now that rivers are in
			TerraformZones();
			TerraformClimate();//Now again, Math.Since we have added climate-modifying features (Mountains, etc.)
			TerraformFill();
			TerraformAverage();
			TerraformFlora();
			TerraformColors();
			BuildMapTexture();
		}

		public Region GetRegionFromPosition(int world_x, int world_y) {
			world_x = Math.Max(world_x, 0);
			world_y = Math.Max(world_y, 0);
			world_x += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].X;
			world_y += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].Y;
			world_x /= REGION_SIZE;
			world_y /= REGION_SIZE;
			if (world_x >= WORLD_GRID || world_y >= WORLD_GRID)
				return map[0, 0];
			return map[world_x, world_y];
		}

		public Region RegionFromPosition(float world_x, float world_y) {
			return GetRegionFromPosition((int) world_x, (int) world_y);
		}

		public Color4 GetColor(int world_x, int world_y, SurfaceColor c) {
			int x = Math.Max(world_x % DITHER_SIZE, 0);
			int y = Math.Max(world_y % DITHER_SIZE, 0);
			world_x += dithermap[x, y].X;
			world_y += dithermap[x, y].Y;
			var offset = new Vector2(
				(float) (world_x % REGION_SIZE) / REGION_SIZE,
				(float) (world_y % REGION_SIZE) / REGION_SIZE);
			var origin = new Coord(
				world_x / REGION_SIZE,
				world_y / REGION_SIZE);
			Region r0 = GetRegion(origin.X, origin.Y);
			Region r1 = GetRegion(origin.X + 1, origin.Y);
			Region r2 = GetRegion(origin.X, origin.Y + 1);
			Region r3 = GetRegion(origin.X + 1, origin.Y + 1);
			Color4 c0, c1, c2, c3;
			switch (c) {
				case SurfaceColor.Dirt:
					c0 = r0.color_dirt;
					c1 = r1.color_dirt;
					c2 = r2.color_dirt;
					c3 = r3.color_dirt;
					break;
				case SurfaceColor.Rock:
					c0 = r0.color_rock;
					c1 = r1.color_rock;
					c2 = r2.color_rock;
					c3 = r3.color_rock;
					break;
				case SurfaceColor.Sand:
					return new Color4(0.98f, 0.82f, 0.42f, 1);
				case SurfaceColor.Grass:
				default:
					c0 = r0.color_grass;
					c1 = r1.color_grass;
					c2 = r2.color_grass;
					c3 = r3.color_grass;
					break;
			}
			var result = new Color4(
				r: MathUtils.InterpolateQuad(c0.R, c1.R, c2.R, c3.R, offset),
				g: MathUtils.InterpolateQuad(c0.G, c1.G, c2.G, c3.G, offset),
				b: MathUtils.InterpolateQuad(c0.B, c1.B, c2.B, c3.B, offset),
				a: 1);
			return result;
		}

		public void PurgeTexture() {
			for (int m = 0; m < TREE_TYPES; m++) {
				for (int t = 0; t < TREE_TYPES; t++) {
					tree[m, t].TexturePurge();
				}
			}
			BuildMapTexture();
		}

		public string DirectionFromAngle(float angle) {
			string direction = "North";
			if (angle < 22.5f)
				direction = "North";
			else if (angle < 67.5f)
				direction = "Northwest";
			else if (angle < 112.5f)
				direction = "West";
			else if (angle < 157.5f)
				direction = "Southwest";
			else if (angle < 202.5f)
				direction = "South";
			else if (angle < 247.5f)
				direction = "Southeast";
			else if (angle < 292.5f)
				direction = "East";
			else if (angle < 337.5f)
				direction = "Northeast";
			return direction;
		}

		/*
		string WorldDirectory () {
			string dir;

			sprintf (dir, "saves//seed%d//", planet.seed);
			return dir;
		}
		*/

		/*

		struct WHeader {
			int version;
			int seed;
			int world_grid;
			int noise_buffer;
			int tree_types;
			int map_bytes;
		};

		*/

	}
}

