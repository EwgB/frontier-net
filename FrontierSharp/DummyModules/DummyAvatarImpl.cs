﻿namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Interfaces.Property;

    class DummyAvatarImpl : IAvatar {

        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public Vector3 GetCameraPosition() {
            return new Vector3(1, 1, 0);
        }

        public Vector3 GetCameraAngle() {
            return Vector3.UnitX;
        }
    }
}
