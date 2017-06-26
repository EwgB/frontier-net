namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Interfaces.Property;
    using System;

    class DummyAvatar : IAvatar {

        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public Vector3 GetCameraPosition() {
            return new Vector3(1, 1, 0);
        }

        public Vector3 GetCameraAngle() {
            return Vector3.UnitX;
        }
    }
}
