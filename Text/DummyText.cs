namespace FrontierSharp.Text {
    using NLog;

    using Common;

    class DummyText : IText {
        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void Init() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }

        public void Print(string format, params object[] args) {
            Log.Info(format, args);
        }
    }
}