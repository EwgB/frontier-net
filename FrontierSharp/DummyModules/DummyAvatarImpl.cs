﻿namespace FrontierSharp.DummyModules {
    using Interfaces;
    using OpenTK;

    class DummyAvatarImpl : IAvatar {
        public Vector3 GetCameraPosition() {
            return new Vector3(1, 1, 0);
        }
    }
}