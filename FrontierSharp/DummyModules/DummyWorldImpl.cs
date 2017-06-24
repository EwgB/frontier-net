namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Interfaces.Property;

    internal class DummyWorldImpl : IWorld {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public uint MapId { get { return 0; } }

        public float GetWaterLevel(Vector2 coord) {
            return 0;
        }
    }
}