namespace FrontierSharp.Region {
    using OpenTK;

    using Common.Region;
    using Common.Util;
    using Common.World;

    internal class DummyRegion : IRegion {
        public string Title => "DUMMY_REGION";
        public float GeoBias => 0;
        public float GeoDetail => 0;
        public int MountainHeight => 0;
        public Vector2 GridPosition => Vector2.Zero;
        public float TreeThreshold => 0.15f;
        public float GeoScale => 0;
        public float GeoWater => 0;
        public Color3 ColorAtmosphere => Color3.Blue;
        public Color3 ColorMap => Color3.Black;
        public Climate Climate => Climate.Invalid;
        public float CliffThreshold => 0;
        public Color3 ColorDirt => Color3.Brown;
        public Color3[] ColorFlowers => new[] { Color3.Red, Color3.Green, Color3.Blue };
        public Color3 ColorGrass => Color3.Green;
        public Color3 ColorRock => Color3.DarkSlateGray;
        public RegionFlag ShapeFlags => RegionFlag.Beach;
        public uint[] FlowerShape => new uint[] { 1 };
        public bool HasFlowers => false;
        public float Moisture => 0;
        public int RiverId => 0;
        public int RiverSegment => 1;
        public float RiverWidth => 1;
        public float Temperature => (this.GridPosition.Y - WorldUtils.WORLD_GRID / 4f) / WorldUtils.WORLD_GRID_CENTER;
        public uint TreeType => 3;
    }
}
