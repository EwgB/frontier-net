namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.World;

    internal class DummyWorld : IWorld {
        public IProperties Properties { get; }

        public uint MapId => 0;

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public float GetWaterLevel(Vector2 coord) {
            return 0;
        }

        public IRegion GetRegion(int x, int y) {
            return new DummyRegion();
        }

        public float GetWaterLevel(float x, float y) {
            return 0;
        }
    }
}