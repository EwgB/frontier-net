using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

namespace Frontier {
	class FWorld {
		enum Climate {
		  Invalid,		Ocean,		Coast,	Mountain,		River,		RiverBank,
		  Swamp,			Rocky,		Lake,		Desert,			Field,		Plains,
		  Canyon,			Forest,		Types,
		};

		struct Region {
		  string    title;
		  Climate   climate;
		  Coord     grid_pos;
		  bool      has_flowers;

			int
				tree_type,
				flags_shape,
				mountain_height,
				river_id,
				river_segment;

			int[] flower_shape = new int[FLOWERS];

			float
				tree_threshold,
				river_width,
				geo_scale,			//Number from -1 to 1, lowest to highest elevation. 0 is sea level
				geo_water,
				geo_detail,
				geo_bias,
				temperature,
				moisture,
				cliff_threshold;

			Color4
				color_map,
				color_rock,
				color_dirt,
				color_grass,
				color_atmosphere;

			Color4[] color_flowers = new Color4[FLOWERS];

		};

		//Only one of these is ever instanced.  This is everything that goes into a "save file".
		//Using only this, the entire world can be re-created.
		struct World {
		  int[] noisei = new int[NOISE_BUFFER];
		  int seed, river_count, lake_count;

			bool wind_from_west, northern_hemisphere;
		  
			float[] noisef = new float[NOISE_BUFFER];
		  Region[,] map = new Region[WORLD_GRID, WORLD_GRID];
		};

		#region Constants
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

			// We keep a list of random numbers so we can have deterministic "randomness". 
			// This is the size of that list.
			NOISE_BUFFER            = 1024,

			// This is the size of the grid of trees. The total number of tree species in the world
			// is the square of this value, minus one. ("tree zero" is actually "no trees at all".)
			TREE_TYPES              = 6;
		#endregion

		Cell WorldCell (int world_x, int world_y);
		Color4 WorldColorGet(int world_x, int world_y, SurfaceColor c);
		char* WorldLocationName (int world_x, int world_y);
		Region WorldRegionFromPosition (int world_x, int world_y);
		Region WorldRegionFromPosition (int world_x, int world_y);
		float WorldWaterLevel (int world_x, int world_y);

		void WorldGenerate (int seed);
		int WorldCanopyTree ();
		char* WorldDirectionFromAngle (float angle);
		//char* WorldDirectory ();
		void WorldInit ();
		void WorldLoad (int seed);
		int WorldMap ();
		int     WorldNoisei (int index);
		float WorldNoisef (int index);
		World WorldPtr ();
		Region WorldRegionGet (int index_x, int index_y);
		void WorldRegionSet (int index_x, int index_y, Region val);
		void WorldSave ();
		int WorldTreeType (float moisture, float temperature);
		static Tree WorldTree (int id);
		void WorldTexturePurge ();
	}
}
