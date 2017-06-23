namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Properties;

    internal class DummyWorldImpl : IWorld {
        private Properties properties;
        public Properties Properties {
            get { return this.properties; }
        }

        public uint MapId { get { return 0; } }

        public float GetWaterLevel(Vector2 coord) {
            return 0;
        }
    }
}