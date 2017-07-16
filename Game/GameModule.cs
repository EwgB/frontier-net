namespace FrontierSharp.Game {
    using Ninject.Modules;

    using Common.Game;

    public class GameModule : NinjectModule {
        private readonly bool useDummy;

        public GameModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<IGame>().To<DummyGame>();
            } else {
                Bind<IGame>().To<GameImpl>();
            }
        }
    }
}