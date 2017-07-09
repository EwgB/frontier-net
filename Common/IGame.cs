namespace FrontierSharp.Common {
    using System;

    /// <summary>Handles the launching of new games, quitting games, etc.</summary>
    public interface IGame : IModule, IDisposable {
        float Time { get; }
        bool IsRunning { get; }
        
        void Quit();
        void New(uint seed);

        /* From Game.h
        bool GameCmd(vector<string>* args);
        char* GameDirectory();
        */
    }
}
