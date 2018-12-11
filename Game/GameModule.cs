namespace FrontierSharp.Game {
    using Ninject.Modules;

    using Common.Game;

    public class GameModule : NinjectModule {
        private readonly bool useDummy;

        public GameModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<IGame>().To<DummyGame>().InSingletonScope();
            } else {
                Bind<IGame>().To<GameImpl>().InSingletonScope();
            }
        }
    }
}