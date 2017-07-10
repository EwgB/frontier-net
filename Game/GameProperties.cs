namespace FrontierSharp.Game {
    using System;

    using NLog;

    using Common.Game;

    using Properties;

    internal class GameProperties : Properties, IGameProperties {
        private const string GAME_TIME = "game_time";
        private const string LAST_PLAYED = "last_played";

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public TimeSpan GameTime {
            get { return GetProperty<TimeSpan>(GAME_TIME).Value; }
            set { GetProperty<TimeSpan>(GAME_TIME).Value = value; }
        }

        public uint LastPlayed {
            get { return GetProperty<uint>(LAST_PLAYED).Value; }
            set { GetProperty<uint>(LAST_PLAYED).Value = value; }
        }

        public GameProperties() {
            try {
                AddProperty(new Property<TimeSpan>(GAME_TIME, TimeSpan.Zero, "The in-game time of day."));
                AddProperty(new Property<uint>(LAST_PLAYED, 0));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
