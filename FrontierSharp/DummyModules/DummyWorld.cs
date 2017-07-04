namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common;
    using Common.Property;
    using Common.Region;

    internal class DummyWorld : IWorld {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public uint MapId { get { return 0; } }

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
    }
}