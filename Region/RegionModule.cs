namespace FrontierSharp.Region {
    using Ninject.Modules;

    using Common.Region;

    public class RegionModule : NinjectModule {
        private readonly bool useDummy;

        public RegionModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<IRegion>().To<DummyRegion>().InSingletonScope();
            } else {
                Bind<IRegion>().To<RegionImpl>().InSingletonScope();
            }
        }
    }
}
