namespace FrontierSharp.Player {
    using Ninject.Modules;

    using Common;

    public class PlayerModule : NinjectModule {
        public override void Load() {
            Bind<IPlayer>().To<DummyPlayer>().InSingletonScope();
        }
    }
}