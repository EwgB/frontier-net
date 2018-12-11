namespace FrontierSharp.World {
    using Ninject.Modules;
    using NLog;

    using Common.World;

    public class WorldModule : NinjectModule {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly bool useDummy;

        public WorldModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            var kernel = Kernel;
            if (kernel == null) {
                Log.Error("Kernel should not be null.");
            } else if (useDummy) {
                Bind<IWorld>().To<DummyWorld>().InSingletonScope();
            } else {
                Bind<IWorld>().To<WorldImpl>().InSingletonScope();
            }
        }
    }
}