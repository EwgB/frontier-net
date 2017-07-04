namespace FrontierSharp {
    using Ninject;

    using Common;
    using Common.Animation;
    using Common.Avatar;
    using Common.Environment;
    using Common.Particles;
    using Common.Renderer;
    using Common.Scene;
    using Common.Textures;

    using Avatar;
    using DummyModules;
    using Environment;
    using Renderer;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies

                // Modules
                kernel.Bind<IAvatar>().To<AvatarImpl>().InSingletonScope();
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
                kernel.Bind<ITextures>().To<DummyTextures>().InSingletonScope();
                kernel.Bind<IWorld>().To<DummyWorld>().InSingletonScope();

                // Other dependencies
                kernel.Bind<IFigure>().To<DummyFigure>();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
