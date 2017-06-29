namespace FrontierSharp {
    using Ninject;

    using Interfaces;
    using Interfaces.Environment;
    using Interfaces.Particles;
    using Interfaces.Renderer;

    using DummyModules;
    using Environment;
    using Renderer;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies

                // Modules
                kernel.Bind<IAvatar>().To<DummyAvatar>().InSingletonScope();
                kernel.Bind<IConsole>().To<DummyConsole>().InSingletonScope();
                kernel.Bind<IEnvironment>().To<EnvironmentImpl>().InSingletonScope();
                kernel.Bind<IGame>().To<DummyGame>().InSingletonScope();
                kernel.Bind<IParticles>().To<DummyParticles>().InSingletonScope();
                kernel.Bind<IPlayer>().To<DummyPlayer>().InSingletonScope();
                kernel.Bind<IRenderer>().To<RendererImpl>().InSingletonScope();
                kernel.Bind<IScene>().To<DummyScene>().InSingletonScope();
                kernel.Bind<IShaders>().To<DummyShaders>().InSingletonScope();
                kernel.Bind<ISky>().To<DummySky>().InSingletonScope();
                kernel.Bind<IText>().To<DummyText>().InSingletonScope();
                kernel.Bind<ITexture>().To<DummyTexture>().InSingletonScope();
                kernel.Bind<IWorld>().To<DummyWorld>().InSingletonScope();

                // Other types
                kernel.Bind<IRegion>().To<DummyRegion>();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
