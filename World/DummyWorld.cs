namespace FrontierSharp.World {
    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.World;

    internal class DummyWorld : IWorld {
        public IProperties Properties { get; }

        public bool WindFromWest { get; set; }

        public uint MapId => 0;

        public DummyWorld(IRegion region) {
            this.region = region;
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Generate(uint seed) { /* Do nothing */ }
        public void Save() { /* Do nothing */ }
        public void Load(uint seed) { /* Do nothing */ }

        public float GetWaterLevel(Vector2 coord) => 0;
        public float GetWaterLevel(float x, float y) => 0;

        private readonly IRegion region;
        public IRegion GetRegion(int x, int y) => this.region;
    }
}