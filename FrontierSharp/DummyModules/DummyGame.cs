namespace FrontierSharp.DummyModules {
    using Interfaces;

    internal class DummyGame : IGame {
        public float Time {  get { return 6.5f; } }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }
    }
}