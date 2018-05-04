namespace FrontierSharp.Common.Region {
    using OpenTK;

    using Util;

    /// <summary>
    /// Holds the region grid, which is the main table of information from
    /// which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
    /// the resulting data is not STORED here. Regions are sets of rules and
    /// properties. You crank numbers through them, and it creates the world.
    /// 
    /// This output data is stored and managed elsewhere. (See IPage.cs) TODO
    /// </summary>
    public interface IRegion {
        string Title { get; }
        ushort TreeType { get; }
        RegionFlags ShapeFlags { get; set; }
        ClimateType Climate { get; set; }
        Vector2 GridPosition { get; }
        int MountainHeight { get; }
        int RiverId { get; }
        int RiverSegment { get; }
        float TreeThreshold { get; }
        float RiverWidth { get; }
        /// <summary>Number from -1 to 1, lowest to highest elevation. 0 is sea level.</summary>
        float GeoScale { get; }
        float GeoWater { get; }
        float GeoDetail { get; }
        float GeoBias { get; }
        float Temperature { get; }
        float Moisture { get; }
        float CliffThreshold { get; }
        Color3 ColorMap { get; }
        Color3 ColorRock { get; }
        Color3 ColorDirt { get; }
        Color3 ColorGrass { get; }
        Color3 ColorAtmosphere { get; }
        Color3[] ColorFlowers { get; }
        uint[] FlowerShape { get; }
        bool HasFlowers { get; }
    }
}
