namespace FrontierSharp.Common.Region {
    using OpenTK;

    using Common.Util;

    public interface IRegion {
        string Title { get; }
        uint TreeType { get; }
        RegionFlag ShapeFlags { get; }
        Climate Climate { get; }
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
