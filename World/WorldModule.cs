namespace FrontierSharp.World {
    using Ninject;
    using Ninject.Modules;
    using NLog;

    using Common.Region;
    using Common.World;

    using Region;

    public class WorldModule : NinjectModule {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void Load() {
            var kernel = this.Kernel;
            if (kernel == null) {
                Log.Error("Kernel should not be null.");
            } else {
                if (!kernel.HasModule("FrontierSharp.Region.RegionModule")) {
                    kernel.Load(new RegionModule(true));
                }
                var region = kernel.Get<IRegion>();
                Bind<IWorld>().To<DummyWorld>()
                    .InSingletonScope()
                    .WithConstructorArgument(region);
            }
        }
    }
}