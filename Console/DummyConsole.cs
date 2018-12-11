namespace FrontierSharp.Console {
    using NLog;

    using OpenTK.Input;

    using Common;

    internal class DummyConsole : IConsole {
        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool IsOpen { get; private set; }

        public void Init() {
            Log.Trace("Init");
            IsOpen = false;
        }

        public void ProcessKey(KeyboardKeyEventArgs e) {
            Log.Info("Key {0} sent to console.", e.Key);
        }

        public void ToggleConsole() {
            Log.Trace("ToggleConsole");

            if (IsOpen) {
                Log.Info("Console closed");
                IsOpen = false;
            } else {
                Log.Info("Console opened");
                IsOpen = true;
            }
        }

        public void Update() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }
    }
}