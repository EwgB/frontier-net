namespace FrontierSharp.Console {
    using NLog;

    using OpenTK.Input;

    using Common;

    internal class DummyConsole : IConsole {
        // Logger
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool IsOpen { get; private set; }

        public void Init() {
            Logger.Trace("Init");
            IsOpen = false;
        }

        public void ProcessKey(KeyboardKeyEventArgs e) {
            Logger.Info("Key {0} sent to console.", e.Key);
        }

        public void Log(string msg) {
            Logger.Info(msg);
        }

        public void ToggleConsole() {
            Logger.Trace("ToggleConsole");

            if (IsOpen) {
                Logger.Info("Console closed");
                IsOpen = false;
            } else {
                Logger.Info("Console opened");
                IsOpen = true;
            }
        }

        public void Update() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }
    }
}