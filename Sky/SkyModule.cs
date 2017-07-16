namespace FrontierSharp.Sky {
    using Ninject.Modules;

    using Common;

    public class SkyModule : NinjectModule {
        public override void Load() {
            Bind<ISky>().To<DummySky>();
        }
    }
}