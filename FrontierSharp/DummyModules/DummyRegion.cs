namespace FrontierSharp.DummyModules {
    using OpenTK;
    using OpenTK.Graphics;

    using Interfaces.Region;

    using World;

    class DummyRegion : IRegion {
        public string Title { get { return "DUMMY_REGION"; } }
        public float GeoBias { get { return 0; } }
        public float GeoDetail { get { return 0; } }
        public int MountainHeight { get { return 0; } }
        public Vector2 GridPosition { get; private set; }
        public float TreeThreshold { get { return 0.15f; } }
        public float GeoScale { get { return 0; } }
        public float GeoWater { get { return 0; } }
        public Color4 ColorAtmosphere { get { return Color4.Blue; } }
        public Color4 ColorMap { get { return Color4.Black; } }
        public Climate Climate { get { return Climate.Invalid; } }
        public float CliffThreshold { get { return 0; } }
        public Color4 ColorDirt { get { return Color4.Brown; } }
        public Color4[] ColorFlowers { get { return new Color4[] { Color4.Red, Color4.Green, Color4.Blue }; } }
        public Color4 ColorGrass { get { return Color4.Green; } }
        public Color4 ColorRock { get { return Color4.DarkSlateGray; } }
        public RegionFlag ShapeFlags { get { return RegionFlag.Beach; } }
        public uint[] FlowerShape { get { return 1; } }
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
