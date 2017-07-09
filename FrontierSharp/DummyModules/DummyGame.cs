namespace FrontierSharp.DummyModules {
    using Common;

    internal class DummyGame : IGame {
        public bool IsRunning { get; private set; }

        public float Time => 6.5f;

        public void Init() {
            this.IsRunning = true;
        }

        public void Quit() {
            this.IsRunning = false;
        }

        public void Update() { /* Do nothing */ }

        public void New(uint seed) { /* Do nothing */ }

        public void Dispose() { /* Do nothing */ }
    }
}