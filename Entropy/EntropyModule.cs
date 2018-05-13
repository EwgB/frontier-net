namespace FrontierSharp.Entropy{
    using Ninject.Modules;

    using Common;

    public class EntropyModule : NinjectModule {
        public override void Load() {
            Bind<IEntropy>().To<EntropyImpl>().InSingletonScope();
        }
    }
}