using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frontier {
	enum SurfaceColor {
		SURFACE_COLOR_BLACK,
		SURFACE_COLOR_SAND,
		SURFACE_COLOR_DIRT,
		SURFACE_COLOR_GRASS,
		SURFACE_COLOR_ROCK,
		SURFACE_COLOR_SNOW,
	};

	enum SurfaceType {
		SURFACE_NULL,
		SURFACE_SAND_DARK,
		SURFACE_SAND,
		SURFACE_DIRT_DARK,
		SURFACE_DIRT,
		SURFACE_FOREST,
		SURFACE_EDGE,
		SURFACE_GRASS,
		SURFACE_GRASS_EDGE,
		SURFACE_DEEPGRASS,
		SURFACE_ROCK,
		SURFACE_SNOW,
		SURFACE_TYPES
	};

	enum LOD {
		LOD_LOW,
		LOD_MED,
		LOD_HIGH,
		LOD_LEVELS
	};

	enum DIRS {
		NORTH,
		SOUTH,
		EAST,
		WEST
	};

	struct Cell {
		float elevation;
		float water_level;
		float detail;
	};
}
