namespace terrain_sharp.Source.World {
	using GLTypes;

	using OpenTK.Graphics;

	class Region {
		private const int FLOWERS = 3;

		public string title;
		public int tree_type;
		public RegionFlag FlagsShape { get; set; }
		public Climate climate;
		public Coord grid_pos;
		public int mountain_height;
		public int river_id;
		public int river_segment;
		public float tree_threshold;
		public float river_width;
		public float geo_scale; //Number from -1 to 1, lowest to highest elevation. 0 is sea level
		public float geo_water;
		public float geo_detail;
		public float geo_bias;
		public float temperature;
		public float moisture;
		public float cliff_threshold;
		public Color4 color_map;
		public Color4 color_rock;
		public Color4 color_dirt;
		public Color4 color_grass;
		public Color4 color_atmosphere;
		public Color4[] color_flowers = new Color4[FLOWERS];
		public int[] flower_shape = new int[FLOWERS];
		public bool has_flowers;
	}
}
