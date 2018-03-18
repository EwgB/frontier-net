namespace FrontierSharp.Cache {
    using Ninject.Modules;

    using Common;

    public class CacheModule : NinjectModule {
        private readonly bool useDummy;

        public CacheModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            Bind<CachePageFactory>().To<CachePageFactory>();
            if (this.useDummy) {
                Bind<ICache>().To<DummyCache>().InSingletonScope();
            }
            else {
                Bind<ICache>().To<CacheImpl>().InSingletonScope();
            }
        }
    }
}
