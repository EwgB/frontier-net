namespace FrontierSharp.DummyModules {
    using System;
    using Interfaces.Region;
    using OpenTK;
    using OpenTK.Graphics;

    class DummyRegion : IRegion {
        public string Title { get { return "DUMMY_REGION"; } }
        public float GeoBias { get { return 0; } }
        public float GeoDetail { get { return 0; } }
        public int MountainHeight { get { return 0; } }
        public Vector2 GridPos { get; private set; }
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
        public uint[] FlowerShape
        public bool HasFlowers
        public float Moisture
        public int RiverId
        public int RiverSegment
        public float RiverWidth
        public float Temperature
        public uint TreeType
    }
}
