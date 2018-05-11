namespace FrontierSharp.Common.Region {
    using OpenTK;

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
    public interface IRegion {
        string Title { get; set; }
        int TreeType { get; set; }
        RegionFlags ShapeFlags { get; set; }
        ClimateType Climate { get; set; }
        Vector2 GridPosition { get; set; }
        int MountainHeight { get; set; }
        int RiverId { get; set; }
        int RiverSegment { get; set; }
        float TreeThreshold { get; set; }
        float RiverWidth { get; set; }
        /// <summary>Number from -1 to 1, lowest to highest elevation. 0 is sea level.</summary>
        float GeoScale { get; set; }
        float GeoWater { get; set; }
        float GeoDetail { get; set; }
        float GeoBias { get; set; }
        float Temperature { get; set; }
        float Moisture { get; set; }
        float CliffThreshold { get; set; }
        Color3 ColorMap { get; set; }
        Color3 ColorRock { get; set; }
        Color3 ColorDirt { get; set; }
        Color3 ColorGrass { get; set; }
        Color3 ColorAtmosphere { get; set; }
        Flower[] Flowers { get; }
        bool HasFlowers { get; set; }
    }
}
