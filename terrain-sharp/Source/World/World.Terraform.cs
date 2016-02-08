///<summary>
///	This module is a set of worker functions for World.cpp.
/// This system isn't connected to anything else and it's only used
///	when World.cpp is generating region data.
///</summary>
namespace terrain_sharp.Source.World {
	using System;
	using System.Collections.Generic;

	using MathNet.Numerics.Random;

	using OpenTK;
	using OpenTK.Graphics;

	using Extensions;
	using GLTypes;
	using StdAfx;
	using Utils;

	partial class World {
		///<summary>The number of regions around the edge which should be ocean.</summary>
		private const int OCEAN_BUFFER = (WORLD_GRID / 10);
		///<summary>This affects the mapping of the coastline.  Higher = busier, more repetitive coast.</summary>
		private const int FREQUENCY = 1;

		private readonly string[] directionName = new string[] { "Northern", "Southern", "Eastern", "Western" };

		private readonly Coord[] direction = new Coord[] {
			new Coord(0, -1), // North
			new Coord(0, 1),  // South
			new Coord(1, 0),  // East
			new Coord(-1, 0)  // West
		};

		private readonly Color4[] flower_palette = new Color4[] {
			Color4.White, Color4.White, Color4.White,
			new Color4(1, 0.3f, 0.3f, 1), new Color4(1, 0.3f, 0.3f, 1), //red
			Color4.Yellow, Color4.Yellow,
			new Color4(0.7f, 0.3f, 1, 1), // Violet
			new Color4(1, 0.5f, 1, 1), // Pink #1
			new Color4(1, 0.5f, 0.8f, 1), // Pink #2
			new Color4(1, 0, 0.5f, 1), //Maroon
		};

		#region Helper functions
		///<summary>In general, what part of the map is this coordinate in?</summary>
		private string get_direction_name(int x, int y) {
			var from_center = new Coord(
				Math.Abs(x - WORLD_GRID_CENTER),
				Math.Abs(y - WORLD_GRID_CENTER));
			if (from_center.X < from_center.Y) {
				if (y < WORLD_GRID_CENTER)
					return directionName[(int) Direction.North];
				else
					return directionName[(int) Direction.South];
			}
			if (x < WORLD_GRID_CENTER)
				return directionName[(int) Direction.West];
			return directionName[(int) Direction.East];
		}

		///<summary>In general, what part of the map is this coordinate in?</summary>
		private Coord get_map_side(int x, int y) {
			var from_center = new Coord(
				Math.Abs(x - WORLD_GRID_CENTER),
				Math.Abs(y - WORLD_GRID_CENTER));
			if (from_center.X < from_center.Y) {
				if (y < WORLD_GRID_CENTER)
					return direction[(int) Direction.North];
				else
					return direction[(int) Direction.South];
			}
			if (x < WORLD_GRID_CENTER)
				return direction[(int) Direction.West];
			return direction[(int) Direction.East];
		}

		///<summary>Test the given area and see if it contains the given climate.</summary>
		private bool is_climate_present(int x, int y, int radius, Climate c) {
			var start = new Coord(
				Math.Max(x - radius, 0),
				Math.Max(y - radius, 0));
			var end = new Coord(
				Math.Min(x + radius, WORLD_GRID - 1),
				Math.Min(y + radius, WORLD_GRID - 1));
			for (int xx = start.X; xx <= end.X; xx++) {
				for (int yy = start.Y; yy <= end.Y; yy++) {
					var r = GetRegion(xx, yy);
					if (r.climate == c)
						return true;
				}
			}
			return false;
		}

		///<summary>check the regions around the given one, see if they are unused</summary>
		private bool is_free(int x, int y, int radius) {
			for (int xx = -radius; xx <= radius; xx++) {
				for (int yy = -radius; yy <= radius; yy++) {
					if (x + xx < 0 || x + xx >= WORLD_GRID)
						return false;
					if (y + yy < 0 || y + yy >= WORLD_GRID)
						return false;
					var r = GetRegion(x + xx, y + yy);
					if (r.climate != Climate.Invalid)
						return false;
				}
			}
			return true;
		}

		///<summary>look around the map and find an unused area of the desired size</summary>
		private bool find_plot(int radius, out Coord result) {
			int cycles = 0;
			result = null;
			var random = MersenneTwister.Default;
			while (cycles < 20) {
				cycles++;
				var test = new Coord(
					random.Next(WORLD_GRID),
					random.Next(WORLD_GRID));
				if (is_free(test.X, test.Y, radius)) {
					result = test;
					return true;
				}
			}
			//couldn't find a spot. Map is full, or just bad dice rolls. 
			return false;
		}

		///<summary>Gives a 1 in 'odds' chance of adding flowers to the given region</summary>
		private void add_flowers(Region r, int odds) {
			var random = MersenneTwister.Default;
			r.has_flowers = random.Next(odds) == 0;
			int shape = random.Next();
			Color4 c = flower_palette[random.Next(flower_palette.Length)];
			for (int i = 0; i < flower_palette.Length; i++) {
				r.color_flowers[i] = c;
				r.flower_shape[i] = shape;
				if ((random.Next() % 15) == 0) {
					shape = random.Next();
					c = flower_palette[random.Next(flower_palette.Length)];
				}
			}
		}
		#endregion

		#region Functions to place individual climates
		///<summary>Place one mountain</summary>
		private void do_mountain(int x, int y, int mtn_size) {

			int step;

			for (int xx = -mtn_size; xx <= mtn_size; xx++) {
				for (int yy = -mtn_size; yy <= mtn_size; yy++) {
					var r = GetRegion(xx + x, yy + y);
					step = (Math.Max(Math.Abs(xx), Math.Abs(yy)));
					if (step == 0) {
						r.title = "Mountain Summit";
					} else if (step == mtn_size)
						r.title = "Mountain Foothills";
					else {
						r.title = "Mountain";
					}
					r.mountain_height = 1 + (mtn_size - step);
					r.geo_detail = 13 + r.mountain_height * 7;
					r.geo_bias = (NoiseFloat(xx + yy) * 0.5f + r.mountain_height) * REGION_SIZE / 2;
					r.FlagsShape = RegionFlag.NoBlend;
					r.climate = Climate.Mountain;
					SetRegion(xx + x, yy + y, r);
				}
			}
		}

		///<summary>Place a rocky wasteland</summary>
		private void do_rocky(int x, int y, int size) {
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var r = GetRegion(xx + x, yy + y);
					r.title = "Rocky Wasteland";
					r.geo_detail = 40;
					//r.FlagsShape = RegionFlag.NoBlend;
					r.climate = Climate.Rocky;
					SetRegion(x + xx, y + yy, r);
				}
			}

		}

		///<summary>Place some plains</summary>
		private void do_plains(int x, int y, int size) {
			var r = GetRegion(x, y);
			float water = r.geo_water;
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					r = GetRegion(xx + x, yy + y);
					r.title = "Plains";
					r.climate = Climate.Plains;
					r.color_atmosphere = new Color4(0.9f, 0.9f, 0.6f, 1);
					r.geo_water = water;
					r.geo_bias = 8;
					r.moisture = 1;
					r.tree_threshold = 0.1f + NoiseFloat(x + xx + (y + yy) * WORLD_GRID) * 0.2f;
					r.geo_detail = 1.5f + NoiseFloat(x + xx + (y + yy) * WORLD_GRID) * 2;
					add_flowers(r, 8);
					r.FlagsShape |= RegionFlag.NoBlend;
					SetRegion(x + xx, y + yy, r);
				}
			}
		}

		///<summary>Place a swamp</summary>
		private void do_swamp(int x, int y, int size) {
			float water;

			var r = GetRegion(x, y);
			water = r.geo_water;
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					r = GetRegion(xx + x, yy + y);
					r.title = "Swamp";
					r.climate = Climate.Swamp;
					r.color_atmosphere = new Color4(0.4f, 1, 0.6f, 1);
					r.geo_water = water;
					r.moisture = 1;
					r.geo_detail = 8;
					r.has_flowers = false;
					r.FlagsShape |= RegionFlag.NoBlend;
					SetRegion(x + xx, y + yy, r);
				}
			}
		}

		///<summary>Place a field of flowers</summary>
		private void do_field(int x, int y, int size) {
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var r = GetRegion(xx + x, yy + y);
					r.title = "Field";
					r.climate = Climate.Field;
					add_flowers(r, 4);
					r.color_atmosphere = new Color4(0.8f, 0.7f, 0.2f, 1);
					r.geo_detail = 8;
					r.FlagsShape |= RegionFlag.NoBlend;
					SetRegion(x + xx, y + yy, r);
				}
			}
		}

		///<summary>Place a forest</summary>
		private void do_forest(int x, int y, int size) {
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var r = GetRegion(xx + x, yy + y);
					r.title = "Forest";
					r.climate = Climate.Forest;
					r.color_atmosphere = new Color4(0, 0, 0.5f, 1);
					r.geo_detail = 8;
					r.tree_threshold = 0.66f;
					//r.FlagsShape |= RegionFlag.NoBlend;
					SetRegion(x + xx, y + yy, r);
				}
			}
		}

		///<summary>Place a desert</summary>
		private void do_desert(int x, int y, int size) {
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var r = GetRegion(xx + x, yy + y);
					r.title = "Desert";
					r.climate = Climate.Desert;
					r.color_atmosphere = new Color4(0.6f, 0.3f, 0.1f, 1);
					r.geo_detail = 8;
					r.geo_bias = 4;
					r.tree_threshold = 0;
					SetRegion(x + xx, y + yy, r);
				}
			}
		}

		///<summary>Place a canyon</summary>
		private void do_canyon(int x, int y, int radius) {
			for (int yy = -radius; yy <= radius; yy++) {
				var r = GetRegion(x, yy + y);
				float step = (float) Math.Abs(yy) / radius;
				step = 1 - step;
				r.title = "Canyon";
				r.climate = Climate.Canyon;
				r.geo_detail = 5 + step * 25;
				//r.geo_detail = 1;
				r.FlagsShape |= RegionFlag.CanyonNS | RegionFlag.NoBlend;
				SetRegion(x, y + yy, r);
			}
		}

		///<summary>Try to place a lake</summary>
		private bool try_lake(int try_x, int try_y, int id) {
			int size = 4;
			//if (!is_free (try_x, try_y, size)) 
			//return false;
			//Find the lowest water level in our lake
			float water_level = float.MaxValue;
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var r = GetRegion(xx + try_x, yy + try_y);
					if (r.climate != Climate.Invalid && r.climate != Climate.River && r.climate != Climate.RiverBank)
						return false;
					if (r.moisture < 0.5f)
						return false;
					water_level = Math.Min(water_level, r.geo_water);
				}
			}
			for (int xx = -size; xx <= size; xx++) {
				for (int yy = -size; yy <= size; yy++) {
					var to_center = new Vector2(xx, yy);
					float depth = to_center.Length;
					if (depth >= size)
						continue;
					depth = size - depth;
					var r = GetRegion(xx + try_x, yy + try_y);
					r.title = "Lake" + id.Tostring();
					r.geo_water = water_level;
					r.geo_detail = 2;
					r.geo_bias = -4 * depth;
					r.climate = Climate.Lake;
					r.FlagsShape |= RegionFlag.NoBlend;
					SetRegion(xx + try_x, yy + try_y, r);
				}
			}
			return true;
		}

		///<summary>Try to place a river</summary>
		private bool try_river(int start_x, int start_y, int id) {
			int x = start_x;
			int y = start_y;
			var path = new List<Coord>();
			Coord last_move = null;
			while (true) {
				var r = GetRegion(x, y);
				//If we run into the ocean, then we're done.
				if (r.climate == Climate.Ocean)
					break;
				if (r.climate == Climate.Mountain)
					return false;
				//If we run into a river, we've become a tributary.
				if (r.climate == Climate.River) {
					//don't become a tributary at the start of a river. Looks odd.
					if (r.river_segment < 7)
						return false;
					break;
				}
				float lowest = r.geo_water;
				Coord to_coast = get_map_side(x, y);
				//lowest = 999.9f;
				var selected = new Coord();

				for (int d = 0; d < 4; d++) {
					var neighbor = GetRegion(x + direction[d].X, y + direction[d].Y);
					//Don't reverse course into ourselves
					if (last_move == (direction[d] * -1))
						continue;
					//ALWAYS go for the ocean, if available
					if (neighbor.climate == Climate.Ocean) {
						selected = direction[d];
						lowest = neighbor.geo_water;
					}
					//Don't head directly AWAY from the coast
					if (direction[d] == to_coast * -1)
						continue;
					//Go whichever way is lowest
					if (neighbor.geo_water < lowest) {
						selected = direction[d];
						lowest = neighbor.geo_water;
					}
					//SetRegion (x + direction[d].X, y + direction[d].Y, neighbor);
				}
				//If everthing around us is above us, we can't flow downhill
				if ((selected.X == 0) && (selected.Y == 0)) //Let's just head for the edge of the map
					selected = to_coast;
				last_move = selected;
				x += selected.X;
				y += selected.Y;
				path.Add(selected);
			}
			//If the river is too short, ditch it.
			if (path.Count < (WORLD_GRID / 4))
				return false;
			//The river is good. Place it.
			x = start_x;
			y = start_y;
			float water_strength = 0.03f;
			float water_level = GetRegion(x, y).geo_water;
			for (int d = 0; d < path.Count; d++) {
				var r = GetRegion(x, y);
				if (d == 0)
					r.title = string.Format("River%d-Source", id);
				else if (d == path.Count - 1)
					r.title = string.Format("River%d-Mouth", id);
				else
					r.title = string.Format("River%d-%d", id, d);
				//A river should attain full strength after crossing 1/4 of the map
				water_strength += (1 / ((float) WORLD_GRID / 4));
				water_strength = Math.Min(water_strength, 1);
				r.FlagsShape |= RegionFlag.NoBlend;
				r.river_id = id;
				r.moisture = Math.Max(r.moisture, 0.5f);
				r.river_segment = d;
				//Rivers get flatter as they go, travel from rocky streams to wide river plains
				r.geo_detail = 28 - water_strength * 20;
				r.river_width = Math.Min(water_strength, 1);
				r.climate = Climate.River;
				water_level = Math.Min(r.geo_water, water_level);
				//We need to flatten out this space, as well as all of its neighbors.
				r.geo_water = water_level;
				Region neighbor;
				for (int xx = x - 1; xx <= x + 1; xx++) {
					for (int yy = y - 1; yy <= y + 1; yy++) {
						neighbor = GetRegion(xx, yy);
						if (neighbor.climate != Climate.Invalid)
							continue;
						if (xx == 0 && yy == 0)
							continue;
						neighbor.geo_water = Math.Min(neighbor.geo_water, water_level);
						neighbor.geo_bias = r.geo_bias;
						neighbor.geo_detail = r.geo_detail;
						neighbor.climate = Climate.RiverBank;
						neighbor.FlagsShape |= RegionFlag.NoBlend;
						neighbor.title = string.Format("River%d-Banks", id);
						SetRegion(xx, yy, neighbor);
					}
				}
				Coord selected = path[d];
				//neighbor = &continent[x + selected.X, y + selected.Y];
				neighbor = GetRegion(x + selected.X, y + selected.Y);
				if (selected.Y == -1) {//we're moving north
					neighbor.FlagsShape |= RegionFlag.RiverS;
					r.FlagsShape |= RegionFlag.RiverN;
				}
				if (selected.Y == 1) {//we're moving south
					neighbor.FlagsShape |= RegionFlag.RiverN;
					r.FlagsShape |= RegionFlag.RiverS;
				}
				if (selected.X == -1) {//we're moving west
					neighbor.FlagsShape |= RegionFlag.RiverE;
					r.FlagsShape |= RegionFlag.RiverW;
				}
				if (selected.X == 1) {//we're moving east
					neighbor.FlagsShape |= RegionFlag.RiverW;
					r.FlagsShape |= RegionFlag.RiverE;
				}
				SetRegion(x, y, r);
				SetRegion(x + selected.X, y + selected.Y, neighbor);
				x += selected.X;
				y += selected.Y;
			}
			return true;
		}
		#endregion

		#region  The following functions are used when building a new world.
		///<summary>pass over the map, calculate the temp & moisture</summary>
		private void TerraformClimate() {
			float rainfall = 1;
			var walk = new Coord();
			do {
				//Wind (and thus rainfall) come from west.
				int x;
				if (windFromWest)
					x = walk.X;
				else
					x = (WORLD_GRID - 1) - walk.X;
				int y = walk.Y;
				var r = GetRegion(x, y);
				//************   TEMPERATURE *******************//
				//The north 25% is Math.Max cold.  The south 25% is all tropical
				//On a southern hemisphere map, this is reversed.
				float temp;
				if (northernHemisphere)
					temp = ((float) y - (WORLD_GRID / 4)) / WORLD_GRID_CENTER;
				else
					temp = ((float) (WORLD_GRID - y) - (WORLD_GRID / 4)) / WORLD_GRID_CENTER;
				//Mountains are cooler at the top
				if (r.mountain_height != 0)
					temp -= r.mountain_height * 0.15f;
				//We add a slight bit of heat to the center of the map, to
				//round off climate boundaries.
				Vector2 from_center = new Vector2(x - WORLD_GRID_CENTER, x - WORLD_GRID_CENTER);
				float distance = from_center.Length / WORLD_GRID_CENTER;
				temp += distance * 0.2f;
				temp = MathHelper.Clamp(temp, StdAfx.MIN_TEMP, StdAfx.MAX_TEMP);
				//************  RAINFALL *******************//
				//Oceans are ALWAYS WET.
				if (r.climate == Climate.Ocean)
					rainfall = 1;
				float rain_loss = 0;
				//We lose rainfall as we move inland.
				if (r.climate != Climate.Ocean && r.climate != Climate.Coast && r.climate != Climate.Lake)
					rain_loss = 1.0f / WORLD_GRID_CENTER;
				//We lose rainfall more slowly as it gets colder.
				if (temp < 0.5f)
					rain_loss *= temp;
				rainfall -= rain_loss;
				//Mountains block rainfall
				if (r.climate == Climate.Mountain)
					rainfall -= 0.1f * r.mountain_height;
				r.moisture = Math.Max(rainfall, 0);
				//Rivers always give some moisture
				if (r.climate == Climate.River || r.climate == Climate.RiverBank) {
					r.moisture = Math.Max(r.moisture, 0.75f);
					rainfall += 0.05f;
					rainfall = Math.Min(rainfall, 1);
				}
				//oceans have a moderating effect on climate
				if (r.climate == Climate.Ocean)
					temp = (temp + 0.5f) / 2;
				r.temperature = temp;
				//r.moisture = Math.Min (1, r.moisture + NoiseFloat (walk.X + walk.Y * WORLD_GRID) * 0.1f);
				//r.temperature = Math.Min (1, r.temperature + NoiseFloat (walk.X + walk.Y * WORLD_GRID) * 0.1f);
				SetRegion(x, y, r);
			} while (!walk.Walk(WORLD_GRID));
		}

		///<summary>Figure out what plant life should grow here.</summary>
		private void TerraformFlora() {
			var walk = new Coord();
			do {
				var r = GetRegion(walk.X, walk.Y);
				r.tree_type = TreeType(r.moisture, r.temperature);
				if (r.climate == Climate.Forest)
					r.tree_type = CanopyTree;
				SetRegion(walk.X, walk.Y, r);
			} while (!walk.Walk(WORLD_GRID));
		}

		private Color4 TerraformColorGenerate(SurfaceColor c, float moisture, float temperature, int seed) {
			float fade;
			switch (c) {
				case SurfaceColor.Grass:
					var wet_grass = new Color4(
						NoiseFloat(seed++) * 0.3f,
						0.4f + NoiseFloat(seed++) * 0.6f,
						NoiseFloat(seed++) * 0.3f,
						1);
					//Dry grass is mostly reds and oranges
					var dry_grass = new Color4(
						0.7f + NoiseFloat(seed++) * 0.3f,
						0.5f + NoiseFloat(seed++) * 0.5f,
						NoiseFloat(seed++) * 0.3f,
						1);
					//Dead grass is pale beige
					var dead_grass = new Color4(0.7f, 0.6f, 0.5f, 1);
					dead_grass = dead_grass.Scale(0.7f + NoiseFloat(seed++) * 0.3f);
					Color4 warm_grass;
          if (moisture < 0.5f) {
						fade = moisture * 2;
						warm_grass = Color4Utils.Interpolate(dead_grass, dry_grass, fade);
					} else {
						fade = (moisture - 0.5f) * 2;
						warm_grass = Color4Utils.Interpolate(dry_grass, wet_grass, fade);
					}
					//cold grass is pale and a little blue
					var cold_grass = new Color4(
						0.5f + NoiseFloat(seed++) * 0.2f,
						0.8f + NoiseFloat(seed++) * 0.2f,
						0.7f + NoiseFloat(seed++) * 0.2f,
						1);
					if (temperature < StdAfx.TEMP_COLD)
						return Color4Utils.Interpolate(cold_grass, warm_grass, temperature / StdAfx.TEMP_COLD);
					return warm_grass;
				case SurfaceColor.Dirt:
					//Devise a random but plausible dirt color

					//Dry dirts are mostly reds, oranges, and browns
					var dry_dirt = new Color4(
						0.4f + NoiseFloat(seed++) * 0.6f,
						0.4f + NoiseFloat(seed++) * 0.6f,
						0.2f + NoiseFloat(seed++) * 0.4f,
						1);
					dry_dirt.G = Math.Min(dry_dirt.G, dry_dirt.R);
					dry_dirt.G = 0.1f + NoiseFloat(seed++) * 0.5f;
					dry_dirt.B = Math.Min(dry_dirt.B, dry_dirt.G);

					//wet dirt is various browns
					fade = NoiseFloat(seed++) * 0.6f;
					var wet_dirt = new Color4(
						0.2f + fade,
						0.1f + fade,
						fade / 2,
						1);
					wet_dirt.G += NoiseFloat(seed++) * 0.1f;
	
					//cold dirt is pale
					var cold_dirt = Color4Utils.Interpolate(wet_dirt, Color4Utils.FromLuminance(0.7f), 0.5f);
					//warm dirt us a fade from wet to dry
					var warm_dirt = Color4Utils.Interpolate(dry_dirt, wet_dirt, moisture);
					fade = MathUtils.Scalar(temperature, StdAfx.FREEZING, 1);
					return Color4Utils.Interpolate(cold_dirt, warm_dirt, fade);
				case SurfaceColor.Rock:
					//Devise a rock color
					fade = MathUtils.Scalar(temperature, StdAfx.FREEZING, 1);
					//Warm rock is red
					var random = MersenneTwister.Default;
          var warm_rock = new Color4(
						1,
						1 - (float) random.NextDouble() * 0.6f,
						1 - (float) random.NextDouble() * 0.6f,
						1);
					//Cold rock is white or blue
					var cold_rock = new Color4(
						1 - (float) random.NextDouble() * 0.4f,
						1, 1, 1);
					cold_rock.G = cold_rock.R;
					return Color4Utils.Interpolate(cold_rock, warm_rock, fade);
			}
			//Shouldn't happen. Returns pink to flag the problem.
			return new Color4(1, 0, 1, 1);
		}

		///<summary>DeterMath.Mine the grass, dirt, rock, and other colors used by this region.</summary>
		private void TerraformColors() {
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					var r = GetRegion(x, y);
					r.color_grass = TerraformColorGenerate(SurfaceColor.Grass, r.moisture, r.temperature, r.grid_pos.X + r.grid_pos.Y * WORLD_GRID);
					r.color_dirt = TerraformColorGenerate(SurfaceColor.Dirt, r.moisture, r.temperature, r.grid_pos.X + r.grid_pos.Y * WORLD_GRID);
					r.color_rock = TerraformColorGenerate(SurfaceColor.Rock, r.moisture, r.temperature, r.grid_pos.X + r.grid_pos.Y * WORLD_GRID);
					//"atmosphere" is the overall color of the lighting & fog. 
					var warm_air = new Color4(0, 0.2f, 1, 1);
					var cold_air = new Color4(0.7f, 0.9f, 1, 1);
					//Only set the atmosphere color if it wasn't set elsewhere
					if (r.color_atmosphere == Color4.Black)
						r.color_atmosphere = Color4Utils.Interpolate(cold_air, warm_air, r.temperature);
					//Color the map
					switch (r.climate) {
						case Climate.Mountain:
							r.color_map = Color4Utils.FromLuminance(0.2f + (float) r.mountain_height / 4);
							r.color_map.Normalize();
							break;
						case Climate.Desert:
							r.color_map = new Color4(0.9f, 0.7f, 0.4f, 1);
							break;
						case Climate.Coast:
							if (r.FlagsShape.HasFlag(RegionFlag.BeachCliff))
								r.color_map = new Color4(0.3f, 0.3f, 0.3f, 1);
							else
								r.color_map = new Color4(0.9f, 0.7f, 0.4f, 1);
							break;
						case Climate.Ocean:
							r.color_map = new Color4(0, 1 + r.geo_scale * 2, 1 + r.geo_scale, 1);
							r.color_map.Clamp();
							break;
						case Climate.River:
						case Climate.Lake:
							r.color_map = new Color4(0, 0, 0.6f, 1);
							break;
						case Climate.RiverBank:
							r.color_map = r.color_dirt;
							break;
						case Climate.Field:
							r.color_map = r.color_grass.Add(new Color4(0.7f, 0.5f, 0.6f, 1));
							r.color_map.Normalize();
							break;
						case Climate.Plains:
							r.color_map = r.color_grass.Add(new Color4(0.5f, 0.5f, 0.5f, 1));
							r.color_map.Normalize();
							break;
						case Climate.Forest:
							r.color_map = r.color_grass.Add(new Color4(0, 0.3f, 0, 1)).Scale(0.5f);
							break;
						case Climate.Swamp:
							r.color_grass = r.color_grass.Scale(0.5f);
							r.color_map = r.color_grass.Scale(0.5f);
							break;
						case Climate.Rocky:
							r.color_map = r.color_grass.Scale(0.8f).Add(r.color_rock.Scale(0.2f));
							r.color_map.Normalize();
							r.color_map = r.color_rock;
							break;
						case Climate.Canyon:
							r.color_map = r.color_rock.Scale(0.3f);
							break;
						default:
							r.color_map = r.color_grass;
							break;
					}
					if (r.geo_scale >= 0)
						r.color_map = r.color_map.Scale(r.geo_scale * 0.5f + 0.5f);
					//if (r.geo_scale >= 0)
					//r.color_map = Color4Unique (r.tree_type);
					//r.color_map = r.color_atmosphere;
					SetRegion(x, y, r);
				}
			}
		}

		///<summary>
		///	Blur the region attributes by averaging each region with its
		///	neighbors.  This prevents overly harsh transitions.
		///</summary>
		private void TerraformAverage() {
			float[,] temp = new float[WORLD_GRID, WORLD_GRID];
			float[,] moist = new float[WORLD_GRID, WORLD_GRID];
			float[,] elev = new float[WORLD_GRID, WORLD_GRID];
			float[,] sm = new float[WORLD_GRID, WORLD_GRID];
			float[,] bias = new float[WORLD_GRID, WORLD_GRID];

			//Blur some of the attributes
			for (int passes = 0; passes < 2; passes++) {
				int radius = 2;
				for (int x = radius; x < WORLD_GRID - radius; x++) {
					for (int y = radius; y < WORLD_GRID - radius; y++) {
						temp[x, y] = 0;
						moist[x, y] = 0;
						elev[x, y] = 0;
						sm[x, y] = 0;
						bias[x, y] = 0;
						int count = 0;
						for (int xx = -radius; xx <= radius; xx++) {
							for (int yy = -radius; yy <= radius; yy++) {
								var r = GetRegion(x + xx, y + yy);
								temp[x, y] += r.temperature;
								moist[x, y] += r.moisture;
								elev[x, y] += r.geo_water;
								sm[x, y] += r.geo_detail;
								bias[x, y] += r.geo_bias;
								count++;
							}
						}
						temp[x, y] /= count;
						moist[x, y] /= count;
						elev[x, y] /= count;
						sm[x, y] /= count;
						bias[x, y] /= count;
					}
				}
				//Put the blurred values back into our table
				for (int x = radius; x < WORLD_GRID - radius; x++) {
					for (int y = radius; y < WORLD_GRID - radius; y++) {
						var r = GetRegion(x, y);
						//Rivers can get wetter through this process, but not drier.
						if (r.climate == Climate.River)
							r.moisture = Math.Max(r.moisture, moist[x, y]);
						else if (r.climate != Climate.Ocean)
							r.moisture = moist[x, y];//No matter how arid it is, the OCEANS STAY WET!
						if (!r.FlagsShape.HasFlag(RegionFlag.NoBlend)) {
							r.geo_detail = sm[x, y];
							r.geo_bias = bias[x, y];
						}
						SetRegion(x, y, r);
					}
				}
			}
		}

		///<summary>Indentify regions where geo_scale is negative.  These will be ocean.</summary>
		private void TerraformOceans() {
			//define the oceans at the edge of the world
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					var r = GetRegion(x, y);
					bool is_ocean = false;
					if (r.geo_scale <= 0)
						is_ocean = true;
					if (x == 0 || y == 0 || x == WORLD_GRID - 1 || y == WORLD_GRID - 1)
						is_ocean = true;
					if (is_ocean) {
						r.geo_bias = -10;
						r.geo_detail = 0.3f;
						r.moisture = 1;
						r.geo_water = 0;
						r.FlagsShape = RegionFlag.NoBlend;
						r.color_atmosphere = new Color4(0.7f, 0.7f, 1, 1);
						r.climate = Climate.Ocean;
						r.title = get_direction_name(x, y) + " Ocean";
						SetRegion(x, y, r);
					}
				}
			}
		}

		///<summary>Find existing ocean regions and place costal regions beside them.</summary>
		private void TerraformCoast() {
			int cliff_grid = WORLD_GRID / 8;
			//now define the coast 
			for (int pass = 0; pass < 2; pass++) {
				var queue = new List<Coord>();
				for (int x = 0; x < WORLD_GRID; x++) {
					for (int y = 0; y < WORLD_GRID; y++) {
						var r = GetRegion(x, y);
						//Skip already assigned places
						if (r.climate != Climate.Invalid)
							continue;
						bool is_coast = false;
						//On the first pass, we add beach adjoining the sea
						if (pass == 0 && is_climate_present(x, y, 1, Climate.Ocean))
							is_coast = true;
						//One the second pass, we add beach adjoining the beach we added on the previous step
						if (pass != 0 && is_climate_present(x, y, 1, Climate.Coast))
							is_coast = true;
						if (is_coast)
							queue.Add(new Coord(x, y));
					}
				}
				//Now we're done scanning the map.  Run through our list and make the new regions.
				for (int i = 0; i < queue.Count; i++) {
					Coord current = queue[i];
					var r = GetRegion(current.X, current.Y);
					bool is_cliff = (((current.X / cliff_grid) + (current.Y / cliff_grid)) % 2) != 0;
					if (pass == 0)
						r.title = get_direction_name(current.X, current.Y) + " beach";
					else
						r.title = get_direction_name(current.X, current.Y) + " coast";
					//beaches are low and partially submerged
					r.geo_detail = 5 + Entropy(current.X, current.Y) * 10;
					if (pass == 0) {
						r.geo_bias = -r.geo_detail * 0.5f;
						if (is_cliff)
							r.FlagsShape |= RegionFlag.BeachCliff;
						else
							r.FlagsShape |= RegionFlag.Beach;
					} else
						r.geo_bias = 0;
					r.cliff_threshold = r.geo_detail * 0.25f;
					r.moisture = 1;
					r.geo_water = 0;
					r.FlagsShape |= RegionFlag.NoBlend;
					r.climate = Climate.Coast;
					SetRegion(current.X, current.Y, r);
				}
			}
		}

		///<summary>Drop a point in the middle of the terrain and attempt to
		//place a river. </summary>
		private void TerraformRivers(int count) {
			int rivers = 0;
			int cycles = 0;
			int range = WORLD_GRID_CENTER / 3;
			var random = MersenneTwister.Default;
			while (rivers < count && cycles < 100) {
				int x = WORLD_GRID_CENTER + random.Next(range) - range / 2;
				int y = WORLD_GRID_CENTER + random.Next(range) - range / 2;
				if (try_river(x, y, rivers))
					rivers++;
				cycles++;
			}
		}

		///<summary>Search around for places to put lakes</summary>
		private void TerraformLakes(int count) {
			int lakes = 0;
			int cycles = 0;
			int range = WORLD_GRID_CENTER / 4;
			while (lakes < count && cycles < 100) {
				//Pick a random spot in the middle of the map
				int x = WORLD_GRID_CENTER + (NoiseInt(cycles) % range) - range / 2;
				int y = WORLD_GRID_CENTER + (NoiseInt(cycles * 2) % range) - range / 2;
				//Now push that point away from the middle
				Coord shove = get_map_side(x, y) * range;
				if (try_lake(x + shove.X, y + shove.Y, lakes))
					lakes++;
				cycles++;
			}
		}

		///<summary>Create zones of different climates.</summary>
		private void TerraformZones() {
			var walk = new Coord();
			int spinner = 0;
			do {
				int x = walk.X;
				int y = walk.Y;// + NoiseInt(walk.X + walk.Y * WORLD_GRID) % 4;
				int radius = 2 + NoiseInt(10 + walk.X + walk.Y * WORLD_GRID) % 9;
				if (is_free(x, y, radius)) {
					var r = GetRegion(x, y);
					var climates = new List<Climate>();
					//swamps only appear in wet areas that aren't cold.
					if (r.moisture > 0.8f && r.temperature > 0.5f)
						climates.Add(Climate.Swamp);
					//mountains only appear in the middle
					if (Math.Abs(x - WORLD_GRID_CENTER) < 10 && radius > 1)
						climates.Add(Climate.Mountain);
					//Deserts are HOT and DRY. Duh.
					if (r.temperature > StdAfx.TEMP_HOT && r.moisture < 0.05f && radius > 1)
						climates.Add(Climate.Desert);
					//fields should be not too hot or cold.
					if (r.temperature > StdAfx.TEMP_TEMPERATE && r.temperature < StdAfx.TEMP_HOT && r.moisture > 0.5f && radius == 1)
						climates.Add(Climate.Field);
					if (r.temperature > StdAfx.TEMP_TEMPERATE && r.temperature < StdAfx.TEMP_HOT && r.moisture > 0.25f && radius > 1)
						climates.Add(Climate.Plains);
					//Rocky wastelands favor cold areas
					if (r.temperature < StdAfx.TEMP_TEMPERATE)
						climates.Add(Climate.Rocky);
					if (radius > 1 && (NoiseInt(spinner++) % 10) == 0)
						climates.Add(Climate.Canyon);
					if (r.temperature > StdAfx.TEMP_TEMPERATE && r.temperature < StdAfx.TEMP_HOT && r.moisture > 0.5f)
						climates.Add(Climate.Forest);
					if (climates.Count == 0) {
						walk.Walk(WORLD_GRID);
						continue;
					}
					Climate c = climates[MersenneTwister.Default.Next(climates.Count)];
					switch (c) {
						case Climate.Rocky:
							do_rocky(x, y, radius);
							break;
						case Climate.Mountain:
							do_mountain(x, y, radius);
							break;
						case Climate.Canyon:
							do_canyon(x, y, radius);
							break;
						case Climate.Swamp:
							do_swamp(x, y, radius);
							break;
						case Climate.Field:
							do_field(x, y, radius);
							break;
						case Climate.Desert:
							do_desert(x, y, radius);
							break;
						case Climate.Plains:
							do_plains(x, y, radius);
							break;
						case Climate.Forest:
							do_forest(x, y, radius);
							break;
					}
				}
			} while (!walk.Walk(WORLD_GRID));
		}

		///<summary>This will fill in all previously un-assigned regions.</summary>
		private void TerraformFill() {
			var random = MersenneTwister.Default;
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					var r = GetRegion(x, y);
					//See if this is already ocean
					if (r.climate != Climate.Invalid)
						continue;
					r.title = "???";
					r.geo_water = r.geo_scale * 10;
					r.geo_detail = 20;
					//Have them trend more hilly in dry areas
					int rand = random.Next(8);
					if (r.moisture > 0.3f && r.temperature > 0.5f) {
						Color4 c;
						int shape;

						r.has_flowers = random.Next(4) == 0;
						shape = random.Next();
						c = flower_palette[random.Next(flower_palette.Length)];
						for (int i = 0; i < FLOWERS; i++) {
							r.color_flowers[i] = c;
							r.flower_shape[i] = shape;
							if (random.Next(15) == 0) {
								shape = random.Next();
								c = flower_palette[random.Next(flower_palette.Length)];
							}
						}
					}
					if (rand == 0) {
						r.FlagsShape |= RegionFlag.Mesas;
						r.title = "Mesas";
					} else if (rand == 1) {
						r.title = "Craters";
						r.FlagsShape |= RegionFlag.Crater;
					} else if (rand == 2) {
						r.title = "TEST";
						r.FlagsShape |= RegionFlag.Test;
					} else if (rand == 3) {
						r.title = "Sinkhole";
						r.FlagsShape |= RegionFlag.Sinkhole;
					} else if (rand == 4) {
						r.title = "Crack";
						r.FlagsShape |= RegionFlag.Crack;
					} else if (rand == 5) {
						r.title = "Tiered";
						r.FlagsShape |= RegionFlag.Tiered;
					} else if (rand == 6) {
						r.title = "Wasteland";
					} else {
						r.title = "Grasslands";
						//r.geo_detail /= 3;
						//r.geo_large /= 3;
					}
					SetRegion(x, y, r);
				}
			}
		}

		private void TerraformPrepare() {
			//Set some defaults
			var random = MersenneTwister.Default;
			var offset = new Coord(
				random.Next(1024),
				random.Next(1024));
			for (int x = 0; x < WORLD_GRID; x++) {
				for (int y = 0; y < WORLD_GRID; y++) {
					var r = new Region();
					r.title = "NOTHING";
					r.geo_bias = r.geo_detail = 0;
					r.mountain_height = 0;
					r.grid_pos.X = x;
					r.grid_pos.Y = y;
					r.tree_threshold = 0.15f;
					var from_center = new Coord(
						Math.Abs(x - WORLD_GRID_CENTER),
						Math.Abs(y - WORLD_GRID_CENTER));
					//Geo scale is a number from -1 to 1. -1 is lowest ocean. 0 is sea level. 
					//+1 is highest elevation on the island. This is used to guide other derived numbers.
					r.geo_scale = (new Vector2(from_center.X, from_center.Y)).Length;
					r.geo_scale /= (WORLD_GRID_CENTER - OCEAN_BUFFER);
					//Create a steep drop around the edge of the world
					if (r.geo_scale > 1)
						r.geo_scale = 1 + (r.geo_scale - 1) * 4;
					r.geo_scale = 1 - r.geo_scale;
					r.geo_scale += (Entropy((x + offset.X), (y + offset.Y)) - 0.5f);
					r.geo_scale += (Entropy((x + offset.X) * FREQUENCY, (y + offset.Y) * FREQUENCY) - 0.2f);
					r.geo_scale = MathHelper.Clamp(r.geo_scale, -1, 1);
					if (r.geo_scale > 0)
						r.geo_water = 1 + r.geo_scale * 16;
					r.color_atmosphere = new Color4();
					r.geo_bias = 0;
					r.geo_detail = 0;
					r.color_map = Color4.Black;
					r.climate = Climate.Invalid;
					SetRegion(x, y, r);
				}
			}
		}
		#endregion
	}
}