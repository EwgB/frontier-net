namespace FrontierSharp.Environment {
    using Ninject.Modules;

    using Common.Environment;

    public class EnvironmentModule : NinjectModule {
        private readonly bool useDummy;

        public EnvironmentModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<IEnvironment>().To<DummyEnvironment>().InSingletonScope();
            } else {
                Bind<IEnvironment>().To<EnvironmentImpl>().InSingletonScope();
            }
        }
    }
}