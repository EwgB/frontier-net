namespace FrontierSharp {
    using Ninject;

    using Interfaces;
    using Interfaces.Environment;
    using Interfaces.Particles;
    using Interfaces.Renderer;

    using DummyModules;
    using Renderer;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies
                kernel.Bind<IAvatar>().To<DummyAvatarImpl>().InSingletonScope();
                kernel.Bind<IConsole>().To<DummyConsoleImpl>().InSingletonScope();
                kernel.Bind<IEnvironment>().To<DummyEnvironmentImpl>().InSingletonScope();
                kernel.Bind<IGame>().To<DummyGameImpl>().InSingletonScope();
                kernel.Bind<IParticles>().To<DummyParticlesImpl>().InSingletonScope();
                kernel.Bind<IPlayer>().To<DummyPlayerImpl>().InSingletonScope();
                kernel.Bind<IRenderer>().To<RendererImpl>().InSingletonScope();
                kernel.Bind<IScene>().To<DummySceneImpl>().InSingletonScope();
                kernel.Bind<IShaders>().To<DummyShadersImpl>().InSingletonScope();
                kernel.Bind<IText>().To<DummyTextImpl>().InSingletonScope();
                kernel.Bind<ITexture>().To<DummyTextureImpl>().InSingletonScope();
                kernel.Bind<IWorld>().To<DummyWorldImpl>().InSingletonScope();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
