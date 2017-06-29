namespace FrontierSharp.Interfaces.Region {
    using OpenTK;
    using OpenTK.Graphics;

    public interface IRegion {
        string Title { get; }
        uint TreeType { get; }
        RegionFlag ShapeFlags { get; }
        Climate Climate { get; }
        Vector2 GridPos { get; }
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
        Color4 ColorMap { get; }
        Color4 ColorRock { get; }
        Color4 ColorDirt { get; }
        Color4 ColorGrass { get; }
        Color4 ColorAtmosphere { get; }
        Color4[] ColorFlowers { get; }
        uint[] FlowerShape { get; }
        bool HasFlowers { get; }
    }
}
