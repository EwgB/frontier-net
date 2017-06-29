namespace FrontierSharp.DummyModules {
    using Ninject;
    using OpenTK;

    using Interfaces;
    using Interfaces.Property;
    using Interfaces.Region;

    class DummyAvatar : IAvatar {

        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public Vector3 CameraPosition { get { return new Vector3(1, 1, 0); } }

        public Vector3 CameraAngle { get { return Vector3.UnitX; } }

        [Inject]
        public IRegion Region { get; private set; }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }
    }
}
