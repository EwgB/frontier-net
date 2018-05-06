namespace FrontierSharp.Region {
    using OpenTK;

    using Common.Region;
    using Common.Util;
    using Common.World;

    internal class DummyRegion : IRegion {
        public string Title { get => "DUMMY_REGION"; set { } }
        public float GeoBias { get; set; }
        public float GeoDetail { get; set; }
        public int MountainHeight => 0;
        public Vector2 GridPosition => Vector2.Zero;
        public float TreeThreshold => 0.15f;
        public float GeoScale => 0;
        public float GeoWater { get => 0; set { } }
        public Color3 ColorAtmosphere { get => Color3.Blue; set { } }
        public Color3 ColorMap { get => Color3.Black; set { } }
        public ClimateType Climate { get => ClimateType.Invalid; set { } }
        public float CliffThreshold { get => 0; set { } }
        public Color3 ColorDirt { get => Color3.Brown; set { } }
        public Color3 ColorGrass { get => Color3.Green; set { } }
        public Color3 ColorRock { get => Color3.DarkSlateGray; set { } }
        public RegionFlags ShapeFlags { get => RegionFlags.Beach; set { } }
        public bool HasFlowers { get => false; set { } }
        public float Moisture { get => 0; set { } }
        public int RiverId => 0;
        public int RiverSegment => 1;
        public float RiverWidth => 1;
        public float Temperature {
            get => (this.GridPosition.Y - WorldUtils.WORLD_GRID / 4f) / WorldUtils.WORLD_GRID_CENTER;
            set { }
        }
        public ushort TreeType => 3;
        public Flower[] Flowers => new[] {
            new Flower{ Color = Color3.Red, Shape = 1},
            new Flower{ Color = Color3.Green, Shape = 1},
            new Flower{ Color = Color3.Blue, Shape = 1}
        };
    }
}
