namespace FrontierSharp.Input {
    using Ninject.Modules;

    using Common.Input;

    public class InputModule : NinjectModule {
        public override void Load() {
            Bind<IInput>().To<DummyInput>().InSingletonScope();
        }
    }
}