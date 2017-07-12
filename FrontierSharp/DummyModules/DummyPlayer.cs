namespace FrontierSharp.DummyModules {
    using OpenTK;

    using Common;

    internal class DummyPlayer : IPlayer {
        public Vector3 Position { get; set; }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Reset() { /* Do nothing */ }
    }
}