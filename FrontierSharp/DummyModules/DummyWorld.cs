namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common;
    using Common.Property;

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
    }
}