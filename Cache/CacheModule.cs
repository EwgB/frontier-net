namespace FrontierSharp.Cache {
    using Ninject.Modules;

    using Common;

    public class CacheModule : NinjectModule {
        public override void Load() {
            Bind<ICache>().To<DummyCache>().InSingletonScope();
        }
    }
}