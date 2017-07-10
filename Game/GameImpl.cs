namespace FrontierSharp.Game {
    using System;
    using System.Collections.Generic;

    using NLog;
    using OpenTK;
    using OpenTK.Input;

    using Common;
    using Common.Game;
    using Common.Input;
    using Common.Property;

    public class GameImpl : IGame {

        #region Constants

        private const bool AUTO_LOAD = true;
        private const int TIME_SCALE = 1000;  //how many milliseconds per in-game minute

        #endregion

        #region Modules

        private readonly IConsole console;
        private readonly GameWindow gameWindow;
        private readonly IInput input;
        private readonly IText text;

        #endregion

        #region Properties

        public IGameProperties GameProperties { get; } = new GameProperties();
        public IProperties Properties => this.GameProperties;

        public bool IsRunning { get; private set; }

        #endregion

        #region Private members

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private uint seed;
        private bool loadedPrevious;

        #endregion

        public GameImpl(IConsole console, GameWindow gameWindow, IInput input, IText text) {
            this.console = console;
            this.gameWindow = gameWindow;
            this.input = input;
            this.text = text;
        }

        public void Init() { /* Do nothing */ }

        public void Update() {
            if (!this.loadedPrevious && AUTO_LOAD) {
                this.loadedPrevious = true;
                this.seed = this.GameProperties.LastPlayed;
                if (this.seed != 0)
                    Load(this.seed);
                this.GameProperties.LastPlayed = this.seed;
            }
            if (!this.IsRunning) {
                return;
            }

            var days = this.GameProperties.GameTime.Days;
            var hours = this.GameProperties.GameTime.Hours;
            var minutes = this.GameProperties.GameTime.Minutes;
            var seconds = this.GameProperties.GameTime.Seconds;
            
            if (this.input.KeyPressed(Key.BracketRight)) {
                hours++;
            } else if (this.input.KeyPressed(Key.BracketLeft)) {
                hours--;
            }

            seconds += (int)Math.Round(this.gameWindow.UpdateTime * TIME_SCALE);
            if (seconds >= TIME_SCALE) {
                seconds -= TIME_SCALE;
                minutes++;
            }
            if (minutes >= 60) {
                minutes -= 60;
                hours++;
            }
            if (hours < 0) {
                hours += 24;
            } else if (hours >= 24) {
                hours -= 24;
                days++;
            }
            this.GameProperties.GameTime = new TimeSpan(days, hours, minutes, seconds);

            this.text.Print("Day {0}: {1}:{2}", days + 1, hours, minutes);
        }

        public void New(uint seedIn) {
            if (seedIn == 0) {
                Quit();
                return;
            }

            this.GameProperties.GameTime = new TimeSpan(days: 0, hours: 6, minutes: 30, seconds: 0);
            this.IsRunning = true;
            if (this.console.IsOpen) {
                this.console.ToggleConsole();
            }
            this.seed = seedIn;
            Log.Info("Beginning new game with seed {0}.", this.seed);
            /* TODO
            IWorld w;
            int start, end, step;
            int region_x;
            IRegion region;
            IRegion region_neighbor;
            Vector3 av_pos;
            Coord world_pos;
            float elevation;
            int points_checked;

            FileMakeDirectory(GameDirectory());
            SceneClear();
            CachePurge();
            WorldGenerate(seed);
            WorldSave();
            //Now the world is ready.  Look for a good starting point.
            //Start in the center
            w = WorldPtr();
            if (w->wind_from_west) {
                start = WORLD_GRID_CENTER;
                end = 1;
                step = -1;
            } else {
                start = WORLD_GRID_CENTER;
                end = WORLD_GRID - 1;
                step = 1;
            }
            region_x = WORLD_GRID_CENTER;
            for (var x = start; x != end; x += step) {
                region = WorldRegionGet(x, WORLD_GRID_CENTER);
                region_neighbor = WorldRegionGet(x + step, WORLD_GRID_CENTER);
                if (region.climate == CLIMATE_COAST && region_neighbor.climate == CLIMATE_OCEAN) {
                    region_x = x;
                    break;
                }
            }
            //now we've found our starting coastal region. Push the player 1 more regain outward,
            //then begin scanning inward for dry land.
            world_pos.x = REGION_HALF + region_x * REGION_SIZE + step * REGION_SIZE;
            world_pos.x = clamp(world_pos.x, 0, WORLD_GRID * REGION_SIZE);
            world_pos.y = WORLD_GRID_CENTER * REGION_SIZE;
            //Set these values now just in case something goes wrong
            av_pos.x = (float) world_pos.x;
            av_pos.y = (float) world_pos.y;
            av_pos.z = 0.0f;
            step *= -1;//Now scan inward, towards the landmass
            points_checked = 0;
            while (points_checked < REGION_SIZE * 4 && !MainIsQuit()) {
                this.text.Print("Scanning %d", world_pos.x);
                loading(0.02f);
                if (!CachePointAvailable(world_pos.x, world_pos.y)) {
                    CacheUpdatePage(world_pos.x, world_pos.y, SDL_GetTicks() + 20);
                    continue;
                }
                points_checked++;
                elevation = CacheElevation(world_pos.x, world_pos.y);
                if (elevation > 0.0f) {
                    av_pos = Vector3((float) world_pos.x, (float) world_pos.y, elevation);
                    break;
                }
                world_pos.x += step;
            }
            ConsoleLog("GameNew: Found beach in %d moves.", points_checked);
            CVarUtils::SetCVar("last_played", seed);
            PlayerReset();
            PlayerPositionSet(av_pos);
            GameUpdate();
            precache();
            */
        }

        public void Load(uint seedIn) {
            if (seedIn == 0) {
                Log.Error("Load: Can't load a game without a valid seed.");
                return;
            }
            if (this.IsRunning) {
                Log.Error("Load: Can't load while a game is in progress.");
                return;
            }
            this.seed = seedIn;
            /* TODO
            string filename;
            List<string> sub_group;

            filename = GameDirectory();
            filename += "game.sav";
            if (!FileExists(filename.c_str())) {
                seed = 0;
                ConsoleLog("GameLoad: Error: File %s not found.", filename.c_str());
                return;
            }
            if (ConsoleIsOpen())
                ConsoleToggle();
            CVarUtils::SetCVar("last_played", seed);
            this.IsRunning = true;
            sub_group.push_back("game");
            sub_group.push_back("player");
            CVarUtils::Load(filename, sub_group);
            AvatarPositionSet(PlayerPositionGet());
            WorldLoad(seed);
            WorldSave();
            seconds = 0;
            GameUpdate();
            precache();
            */
        }

        public void Save() {
            /* TODO
            string filename;
            vector<string> sub_group;

            if (seed == 0) {
                ConsoleLog("GameSave: Error: No valid game to save.");
                return;
            }
            filename = GameDirectory();
            filename += "game.sav";
            sub_group.push_back("game");
            sub_group.push_back("player");
            CVarUtils::Save(filename, sub_group);
            */
        }

        public void Dispose() {
            /* TODO
            if (this.IsRunning && seed)
                GameSave();
            */
        }

        public void Quit() {
            /* TODO
            ConsoleLog("Quit Game");
            WorldSave();
            SceneClear();
            CachePurge();
            GameSave();
            seed = 0;
            this.IsRunning = false;
            */
        }
    }
}

/* From Game.cpp

static void loading(float progress) {
    SdlUpdate();
    RenderLoadingScreen(progress);
}

static void precache() {
    uint ready, total;

    SceneGenerate();
    PlayerUpdate();
    do {
        SceneProgress(&ready, &total);
        SceneUpdate(SDL_GetTicks() + 20);
        loading(((float) ready / (float) total) * 0.5f);
    } while (ready < total && !MainIsQuit());
    SceneRestartProgress();
    do {
        SceneProgress(&ready, &total);
        SceneUpdate(SDL_GetTicks() + 20);
        loading(0.5f + ((float) ready / (float) total) * 0.5f);
    } while (ready < total && !MainIsQuit());
}

bool GameCmd(vector<string>* args) {
    uint new_seed;

    if (args->empty()) {
        ConsoleLog(CVarUtils::GetHelp("game").data());
        return true;
    }
    if (!args->data()[0].compare("new")) {
        if (args->size() < 2)
            new_seed = SDL_GetTicks();
        else
            new_seed = atoi(args->data()[1].c_str());
        GameNew(new_seed);
        return true;
    }
    if (!args->data()[0].compare("load")) {
        if (args->size() > 1)
            new_seed = atoi(args->data()[1].c_str());
        GameLoad(new_seed);
        return true;
    }
    if (!args->data()[0].compare("quit")) {
        GameQuit();
        return true;
    }
    ConsoleLog(CVarUtils::GetHelp("game").data());
    return true;
}

char* GameDirectory() {
    static char dir[32];

    sprintf(dir, "saves//seed%d//", seed);
    return dir;

}
 */