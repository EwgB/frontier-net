namespace FrontierSharp.Common.Region {
    using Util;

    public struct Flower {
        public Color3 Color { get; set; }
        public int Shape { get; set; }
    }

    /// <summary>
    /// Holds the region grid, which is the main table of information from
    /// which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
    /// the resulting data is not STORED here. Regions are sets of rules and
    /// properties. You crank numbers through them, and it creates the world.
    /// 
    /// This output data is stored and managed elsewhere. (See IPage.cs) TODO
    /// </summary>
    public struct Region {
        public const int FLOWERS = 3;

        public string Title { get; set; }
        public int TreeType { get; set; }
        public RegionFlags ShapeFlags { get; set; }
        public ClimateType Climate { get; set; }
        public Coord GridPosition { get; set; }
        public int MountainHeight { get; set; }
        public int RiverId { get; set; }
        public int RiverSegment { get; set; }
        public float TreeThreshold { get; set; }
        public float RiverWidth { get; set; }
        /// <summary>Number from -1 to 1, lowest to highest elevation. 0 is sea level.</summary>
        public float GeoScale { get; set; }
        public float GeoWater { get; set; }
        public float GeoDetail { get; set; }
        public float GeoBias { get; set; }
        public float Temperature { get; set; }
        public float Moisture { get; set; }
        public float CliffThreshold { get; set; }
        public Color3 ColorMap { get; set; }
        public Color3 ColorRock { get; set; }
        public Color3 ColorDirt { get; set; }
        public Color3 ColorGrass { get; set; }
        public Color3 ColorAtmosphere { get; set; }
        public Flower[] Flowers { get; set; }
        public bool HasFlowers { get; set; }
    }
}
