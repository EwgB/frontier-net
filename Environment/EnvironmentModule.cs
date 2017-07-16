namespace FrontierSharp.Environment {
    using Ninject.Modules;

    using Common.Environment;

    public class EnvironmentModule : NinjectModule {
        private readonly bool useDummy;

        public EnvironmentModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<IEnvironment>().To<DummyEnvironment>();
            } else {
                Bind<IEnvironment>().To<EnvironmentImpl>();
            }
        }
    }
}