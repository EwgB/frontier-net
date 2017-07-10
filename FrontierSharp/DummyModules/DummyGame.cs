﻿namespace FrontierSharp.DummyModules {
    using Common.Game;
    using Common.Property;

    internal class DummyGame : IGame {
        public IGameProperties GameProperties { get; }
        public IProperties Properties => this.GameProperties;

        public bool IsRunning { get; private set; }

        public void Init() { this.IsRunning = true; }
        public void Quit() { this.IsRunning = false; }

        public void Update() { /* Do nothing */ }
        public void New(uint seedIn) { /* Do nothing */ }
        public void Load(uint seedIn) { /* Do nothing */ }
        public void Save() { /* Do nothing */ }
        public void Dispose() { /* Do nothing */ }
    }
}