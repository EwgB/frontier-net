namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Region;
    using Common.Util;

    using World;

    class DummyRegion : IRegion {
        public string Title { get { return "DUMMY_REGION"; } }
        public float GeoBias { get { return 0; } }
        public float GeoDetail { get { return 0; } }
        public int MountainHeight { get { return 0; } }
        public Vector2 GridPosition { get; }
        public float TreeThreshold { get { return 0.15f; } }
        public float GeoScale { get { return 0; } }
        public float GeoWater { get { return 0; } }
        public Color3 ColorAtmosphere { get { return Color3.Blue; } }
        public Color3 ColorMap { get { return Color3.Black; } }
        public Climate Climate { get { return Climate.Invalid; } }
        public float CliffThreshold { get { return 0; } }
        public Color3 ColorDirt { get { return Color3.Brown; } }
        public Color3[] ColorFlowers { get { return new Color3[] { Color3.Red, Color3.Green, Color3.Blue }; } }
        public Color3 ColorGrass { get { return Color3.Green; } }
        public Color3 ColorRock { get { return Color3.DarkSlateGray; } }
        public RegionFlag ShapeFlags { get { return RegionFlag.Beach; } }
        public uint[] FlowerShape { get { return new uint[] { 1 }; } }
        public bool HasFlowers { get { return false; } }
        public float Moisture { get { return 0; } }
        public int RiverId { get { return 0; } }
        public int RiverSegment { get { return 1; } }
        public float RiverWidth { get { return 1; } }
        public float Temperature { get { return (this.GridPosition.Y - (WorldUtils.WORLD_GRID / 4)) / WorldUtils.WORLD_GRID_CENTER; } }
        public uint TreeType { get { return 3; } }

        public DummyRegion() {
            this.GridPosition = new Vector2(0, 0);
        }
    }
}
