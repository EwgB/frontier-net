namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common.Avatar;
    using Common.Property;
    using Common.Region;
    using System;

    class DummyAvatar : IAvatar {

        private IAvatarProperties properties;
        public IProperties Properties { get { return this.properties; } }
        public IAvatarProperties AvatarProperties { get { return this.properties; } }

        public Vector3 CameraPosition { get { return new Vector3(1, 1, 0); } }

        public Vector3 CameraAngle { get { return Vector3.UnitX; } }

        private IRegion region = new DummyRegion();
        public IRegion Region { get { return this.region; } }

        public Vector3 Position { get; set; }

        public AnimType AnimationType { get { return AnimType.Idle; } }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public void Render() {
            // Do nothing
        }
    }
}
