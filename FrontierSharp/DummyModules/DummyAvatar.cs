namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Animation;
    using Common.Avatar;
    using Common.Property;
    using Common.Region;

    class DummyAvatar : IAvatar {
        public IProperties Properties => this.AvatarProperties;
        public IAvatarProperties AvatarProperties { get; }

        public Vector3 CameraPosition => new Vector3(1, 1, 0);

        public Vector3 CameraAngle => Vector3.UnitX;

        public IRegion Region { get; } = new DummyRegion();

        public Vector3 Position { get; set; }

        public AnimTypes AnimationType => AnimTypes.Idle;

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public void Render() {
            // Do nothing
        }

        public void Look(int x, int y) {
            // Do nothing
        }
    }
}
