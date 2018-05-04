namespace FrontierSharp.Game {
    using System;
    using System.IO;

    using Ninject;
    using NLog;
    using OpenTK;
    using OpenTK.Input;

    using Common;
    using Common.Avatar;
    using Common.Game;
    using Common.Input;
    using Common.Property;
    using Common.Region;
    using Common.Renderer;
    using Common.Scene;
    using Common.Util;
    using Common.World;

    internal class GameImpl : IGame {

        #region Constants

        private const bool AUTO_LOAD = true;
        private const int TIME_SCALE = 1000;  //how many milliseconds per in-game minute

        #endregion

        #region Modules

        private readonly IKernel kernel;

        private IAvatar avatar;
        private IAvatar Avatar => this.avatar ?? (this.avatar = this.kernel.Get<IAvatar>());

        private ICache cache;
        private ICache Cache => this.cache ?? (this.cache = this.kernel.Get<ICache>());

        private IConsole console;
        private IConsole Console => this.console ?? (this.console = this.kernel.Get<IConsole>());

        private GameWindow gameWindow;
        private GameWindow GameWindow => this.gameWindow ?? (this.gameWindow = this.kernel.Get<GameWindow>());

        private IInput input;
        private IInput Input => this.input ?? (this.input = this.kernel.Get<IInput>());

        private IPlayer player;
        private IPlayer Player => this.player ?? (this.player = this.kernel.Get<IPlayer>());

        private IRenderer renderer;
        private IRenderer Renderer => this.renderer ?? (this.renderer = this.kernel.Get<IRenderer>());

        private IScene scene;
        private IScene Scene => this.scene ?? (this.scene = this.kernel.Get<IScene>());

        private IText text;
        private IText Text => this.text ?? (this.text = this.kernel.Get<IText>());

        private IWorld world;
        private IWorld World => this.world ?? (this.world = this.kernel.Get<IWorld>());

        #endregion

        #region Properties

        public IGameProperties GameProperties { get; } = new GameProperties();
        public IProperties Properties => this.GameProperties;

        public bool IsRunning { get; private set; }

        public string GameDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FrontierSharp", "saves", "seed", this.seed.ToString());
        #endregion

        #region Private members

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private uint seed;
        private bool loadedPrevious;

        #endregion

        public GameImpl(IKernel kernel) {
            this.kernel = kernel;
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
            
            if (this.Input.KeyPressed(Key.BracketRight)) {
                hours++;
            } else if (this.Input.KeyPressed(Key.BracketLeft)) {
                hours--;
            }

            seconds += (int)Math.Round(this.GameWindow.UpdateTime * TIME_SCALE);
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

            this.Text.Print("Day {0}: {1}:{2}", days + 1, hours, minutes);
        }

        public void New(uint seedIn) {
            // TODO: This might take some time, move to worker thread
            if (seedIn == 0) {
                Quit();
                return;
            }

            this.GameProperties.GameTime = new TimeSpan(days: 0, hours: 6, minutes: 30, seconds: 0);
            this.IsRunning = true;
            if (this.Console.IsOpen) {
                this.Console.ToggleConsole();
            }
            this.seed = seedIn;
            Log.Info("Beginning new game with seed {0}.", this.seed);

            Directory.CreateDirectory(this.GameDirectory);
            this.Scene.Clear();
            this.Cache.Purge();
            this.World.Generate(this.seed);
            this.World.Save();

            // Now the world is ready.  Look for a good starting point.

            // Start in the center
            var start = WorldUtils.WORLD_GRID_CENTER;
            int end, step;
            if (this.World.WindFromWest) {
                end = 1;
                step = -1;
            } else {
                end = WorldUtils.WORLD_GRID - 1;
                step = 1;
            }

            // Find starting coastal region, then begin scanning inward for dry land.
            var worldPosition = FindCoast(start, end, step);

            // Set these values now just in case something goes wrong
            var avatarPosition = new Vector3(worldPosition.X, worldPosition.Y, 0);
            step *= -1; // Now scan inward, towards the landmass
            var pointsChecked = 0;
            while (pointsChecked < WorldUtils.REGION_SIZE * 4) {
                this.Text.Print("Scanning {0}", worldPosition.X);
                this.Renderer.RequestLoadingScreen(0.02f);
                if (!this.Cache.IsPointAvailable(worldPosition.X, worldPosition.Y)) {
                    this.Cache.UpdatePage(worldPosition.X, worldPosition.Y, this.GameWindow.UpdateTime / 1000 + 20);
                    continue;
                }
                pointsChecked++;
                var elevation = this.Cache.GetElevation(worldPosition.X, worldPosition.Y);
                if (elevation > 0) {
                    avatarPosition = new Vector3(worldPosition.X, worldPosition.Y, elevation);
                    break;
                }
                worldPosition = new Coord(worldPosition.X + step, worldPosition.Y);
            }

            Log.Info("GameNew: Found beach in {0} moves.", pointsChecked);
            this.GameProperties.LastPlayed = this.seed;
            this.Player.Reset();
            this.Player.Position = avatarPosition;
            Update();
            Precache();
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
            var filename = Path.Combine(this.GameDirectory, "game.sav");
            if (File.Exists(filename)) {
                this.seed = 0;
                Log.Error("Load: File {0} not found.", filename);
                return;
            }
            if (this.Console.IsOpen)
                this.Console.ToggleConsole();

            this.GameProperties.LastPlayed = this.seed;
            this.IsRunning = true;

            /* TODO: Load game and player properties
            var subGroup = new List<string>();
            subGroup.Add("game");
            subGroup.Add("player");
            CVarUtils::Load(filename, subGroup);
            */
            this.Avatar.Position = this.Player.Position;
            this.World.Load(this.seed);
            this.World.Save();
            // Set seconds to 0
            var time = this.GameProperties.GameTime;
            this.GameProperties.GameTime = new TimeSpan(time.Days, time.Hours, time.Minutes, 0);
            Update();
            Precache();
        }

        public void Save() {
            if (this.seed == 0) {
                Log.Error("GameSave: Error: No valid game to save.");
                return;
            }
            /* TODO: Save game and player properties
            var filename = Path.Combine(this.GameDirectory, "game.sav");
            var subGroup = new List<string>();
            subGroup.Add("game");
            subGroup.Add("player");
            CVarUtils::Save(filename, subGroup);
            */
        }

        public void Dispose() {
            if (this.IsRunning && this.seed != 0)
                Save();
        }

        public void Quit() {
            Log.Info("Quit Game");
            this.World.Save();
            this.Scene.Clear();
            this.Cache.Purge();
            Save();
            this.seed = 0;
            this.IsRunning = false;
        }

        private Coord FindCoast(int start, int end, int step) {
            // Look for coast
            var regionX = WorldUtils.WORLD_GRID_CENTER;
            for (var x = start; x != end; x += step) {
                var region = this.World.GetRegion(x, WorldUtils.WORLD_GRID_CENTER);
                var regionNeighbor = this.World.GetRegion(x + step, WorldUtils.WORLD_GRID_CENTER);
                if (region.Climate == ClimateType.Coast && regionNeighbor.Climate == ClimateType.Ocean) {
                    regionX = x;
                    break;
                }
            }

            // Now we've found our starting coastal region. Push the player 1 more region outward
            return new Coord(
                MathHelper.Clamp(WorldUtils.REGION_HALF + regionX * WorldUtils.REGION_SIZE + step * WorldUtils.REGION_SIZE,
                                 0, WorldUtils.WORLD_GRID * WorldUtils.REGION_SIZE),
                WorldUtils.WORLD_GRID_CENTER * WorldUtils.REGION_SIZE);
        }

        private void Precache() {
            // TODO: Put in separate thread and add cancellation
            this.Scene.Generate();
            this.Player.Update();

            int ready;
            int total;
            do {
                this.Scene.Progress(out ready, out total);
                this.Scene.Update(this.GameWindow.UpdateTime / 1000 + 20);
                this.Renderer.RequestLoadingScreen((ready / (float)total) * 0.5f);
            } while (ready < total);
            this.Scene.RestartProgress();
            do {
                this.Scene.Progress(out ready, out total);
                this.Scene.Update(this.GameWindow.UpdateTime / 1000 + 20);
                this.Renderer.RequestLoadingScreen(0.5f + (ready / (float)total) * 0.5f);
            } while (ready < total);
        }
    }
}

/* From Game.cpp

bool GameCmd(vector<string>* args) {
    uint new_seed;

    if (args.empty()) {
        ConsoleLog(CVarUtils::GetHelp("game").data());
        return true;
    }
    if (!args.data()[0].compare("new")) {
        if (args.size() < 2)
            new_seed = SDL_GetTicks();
        else
            new_seed = atoi(args.data()[1].c_str());
        GameNew(new_seed);
        return true;
    }
    if (!args.data()[0].compare("load")) {
        if (args.size() > 1)
            new_seed = atoi(args.data()[1].c_str());
        GameLoad(new_seed);
        return true;
    }
    if (!args.data()[0].compare("quit")) {
        GameQuit();
        return true;
    }
    ConsoleLog(CVarUtils::GetHelp("game").data());
    return true;
}
 */