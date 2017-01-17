namespace FrontierSharp {
    using Ninject;

    using DummyModules;
    using Interfaces;
    using Renderer;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies
                kernel.Bind<IParticles>().To<DummyParticlesImpl>().InSingletonScope();
                kernel.Bind<IRenderer>().To<RendererImpl>().InSingletonScope();
                kernel.Bind<IAvatar>().To<DummyAvatarImpl>().InSingletonScope();
                kernel.Bind<IWorld>().To<DummyWorldImpl>().InSingletonScope();
                kernel.Bind<IEnvironment>().To<DummyEnvironmentImpl>().InSingletonScope();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
