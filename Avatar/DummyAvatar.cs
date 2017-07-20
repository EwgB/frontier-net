namespace FrontierSharp.Avatar {
    using OpenTK;

    using Common.Animation;
    using Common.Avatar;
    using Common.Property;
    using Common.Region;

    internal class DummyAvatar : IAvatar {
        public IProperties Properties => this.AvatarProperties;
        public IAvatarProperties AvatarProperties { get; }

        public Vector3 CameraPosition => new Vector3(1, 1, 0);

        public Vector3 CameraAngle => Vector3.UnitX;

        public IRegion Region { get; }

        public Vector3 Position { get; set; }

        public AnimTypes AnimationType => AnimTypes.Idle;

        public DummyAvatar(IRegion region) {
            this.Region = region;
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }
        public void Look(int x, int y) { /* Do nothing */ }
    }
}
