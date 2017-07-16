namespace FrontierSharp.Avatar {
    using Ninject;
    using Ninject.Modules;
    using NLog;

    using Common.Avatar;
    using Common.Region;

    using Region;

    public class AvatarModule : NinjectModule {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly bool useDummy;

        public AvatarModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                var kernel = this.Kernel;
                if (kernel == null) {
                    Log.Error("Kernel should not be null.");
                } else {
                    kernel.Load(new RegionModule(true));
                    var region = kernel.Get<IRegion>();
                    Bind<IAvatar>().To<DummyAvatar>()
                        .InSingletonScope()
                        .WithConstructorArgument(region);
                }
            } else {
                Bind<IAvatar>().To<AvatarImpl>().InSingletonScope();
            }
        }
    }
}
