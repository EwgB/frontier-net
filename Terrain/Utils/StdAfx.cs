using System;
using System.Collections.Generic;

namespace Frontier {
	enum SurfaceColor { Black, Sand, Dirt, Grass, Rock, Snow }
	enum SurfaceType { Null, SandDark, Sand, DirtDark, Dirt, Forest, Edge, Grass, GrassEdge, DeepGrass, Rock, Snow }

	enum LOD { Low, Med, High }
	enum DIRS { North, South, East, West }

	struct Cell { public float elevation, water_level, detail; }
}
