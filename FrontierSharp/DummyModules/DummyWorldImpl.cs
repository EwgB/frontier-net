namespace FrontierSharp.DummyModules {
    using System;
    using Interfaces;
    using OpenTK;

    internal class DummyWorldImpl : IWorld {
        public uint MapId { get { return 0; } }

        public float GetWaterLevel(Vector2 coord) {
            return 0;
        }
    }
}