namespace FrontierSharp.Water {
    using Ninject.Modules;

    using Common;

    public class WaterModule : NinjectModule {
        public override void Load() {
            Bind<IWater>().To<DummyWater>();
        }
    }
}