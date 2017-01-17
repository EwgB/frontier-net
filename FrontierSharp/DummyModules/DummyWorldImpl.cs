namespace FrontierSharp.DummyModules {
    using System;
    using Interfaces;
    using OpenTK;

    internal class DummyWorldImpl : IWorld {
        public float GetWaterLevel(Vector2 coord) {
            return 0;
        }
    }
}