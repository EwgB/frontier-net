﻿namespace FrontierSharp.Common {
    public interface IGame : IModule {
        float Time { get; }
        bool IsRunning { get; }
        
        void Quit();
        
        /* From Game.h
        bool GameCmd(vector<string>* args);
        char* GameDirectory();
        void GameNew(unsigned seed_in);
        void GameTerm();
        */
    }
}
