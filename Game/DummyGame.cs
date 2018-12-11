namespace FrontierSharp.Game {
    using System;
    using System.IO;

    using Common.Game;
    using Common.Property;

    internal class DummyGame : IGame {
        public IGameProperties GameProperties => new GameProperties();
        public IProperties Properties => GameProperties;

        public bool IsRunning { get; private set; }

        public string GameDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FrontierSharp", "saves");

        public void Init() { IsRunning = true; }
        public void Quit() { IsRunning = false; }

        public void Update() { /* Do nothing */ }
        public void New(int seedIn) { /* Do nothing */ }
        public void Load(int seedIn) { /* Do nothing */ }
        public void Save() { /* Do nothing */ }
        public void Dispose() { /* Do nothing */ }
    }
}