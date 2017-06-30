namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Interfaces;
    using Interfaces.Property;
    using Interfaces.Region;

    class DummyAvatar : IAvatar {

        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public Vector3 CameraPosition { get { return new Vector3(1, 1, 0); } }

        public Vector3 CameraAngle { get { return Vector3.UnitX; } }

        private IRegion region = new DummyRegion();
        public IRegion Region { get { return this.region; } }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }
    }
}
