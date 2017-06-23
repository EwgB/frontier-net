namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Properties;

    class DummyAvatarImpl : IAvatar {

        private Properties properties;
        public Properties Properties {
            get { return this.properties; }
            set { this.properties = value; }
        }

        public Vector3 GetCameraPosition() {
            return new Vector3(1, 1, 0);
        }

        public Vector3 GetCameraAngle() {
            return Vector3.UnitX;
        }
    }
}
