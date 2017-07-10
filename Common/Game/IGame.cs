namespace FrontierSharp.Common.Game {
    using System;
    using Property;

    /// <summary>Handles the launching of new games, quitting games, etc.</summary>
    public interface IGame : IModule, IDisposable, IHasProperties {
        IGameProperties GameProperties { get; }

        bool IsRunning { get; }
        
        void New(uint seedIn);
        void Load(uint seedIn);
        void Save();
        void Quit();

        /* From Game.h
        bool GameCmd(vector<string>* args);
        char* GameDirectory();
        */
    }
}
