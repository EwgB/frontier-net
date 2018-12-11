namespace FrontierSharp.Cache {
    using Ninject.Modules;
    using Ninject.Extensions.Factory;

    using Common;

    public class CacheModule : NinjectModule {
        private readonly bool useDummy;

        public CacheModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<ICache>().To<DummyCache>().InSingletonScope();
            }
            else {
                Bind<ICache>().To<CacheImpl>().InSingletonScope();
            }

            Bind<ICachePageFactory>().ToFactory();
        }
    }
}
