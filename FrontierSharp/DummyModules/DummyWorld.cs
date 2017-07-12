namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.World;

    internal class DummyWorld : IWorld {
        public IProperties Properties { get; }

        public uint MapId => 0;

        public bool WindFromWest { get; set; }

        public void Init() { /* Do nothing */ }

        public void Update() { /* Do nothing */ }

        public float GetWaterLevel(Vector2 coord) => 0;

        public float GetWaterLevel(float x, float y) => 0;

        public IRegion GetRegion(int x, int y) => new DummyRegion();

        public void Generate(uint seed) { /* Do nothing */ }

        public void Save() { /* Do nothing */ }
    }
}