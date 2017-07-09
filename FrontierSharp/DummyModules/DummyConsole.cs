namespace FrontierSharp.DummyModules {
    using NLog;

    using OpenTK.Input;

    using Common;

    internal class DummyConsole : IConsole {
        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool IsOpen { get; private set; }

        public void Init() {
            Log.Trace("Init");
            this.IsOpen = false;
        }

        public void ProcessKey(KeyboardKeyEventArgs e) {
            Log.Info("Key " + e.Key.ToString() + " sent to console.");
        }

        public void ToggleConsole() {
            Log.Trace("ToggleConsole");

            if (this.IsOpen) {
                Log.Info("Console closed");
                this.IsOpen = false;
            } else {
                Log.Info("Console opened");
                this.IsOpen = true;
            }
        }

        public void Update() {
            // Do nothing
        }
    }
}