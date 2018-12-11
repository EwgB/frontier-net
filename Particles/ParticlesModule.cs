namespace FrontierSharp.Particles {
    using Ninject.Modules;

    using Common.Particles;

    public class ParticlesModule : NinjectModule {
        private readonly bool useDummy;

        public ParticlesModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<IParticles>().To<DummyParticles>().InSingletonScope();
            } else {
                Bind<IParticles>().To<ParticlesImpl>().InSingletonScope();
            }
        }
    }
}