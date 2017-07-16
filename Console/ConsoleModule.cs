namespace FrontierSharp.Console {
    using Ninject.Modules;

    using Common;

    public class ConsoleModule : NinjectModule {
        public override void Load() {
            Bind<IConsole>().To<DummyConsole>();
        }
    }
}