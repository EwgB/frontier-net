namespace FrontierSharp {
    using Ninject;

    using Common;
    using Common.Animation;
    using Common.Avatar;
    using Common.Environment;
    using Common.Game;
    using Common.Particles;
    using Common.Renderer;
    using Common.Scene;
    using Common.Shaders;
    using Common.Textures;
    using Common.World;

    using Avatar;
    using DummyModules;
    using Environment;
    using Game;
    using Renderer;
    using Scene;
    using Textures;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies

                // Modules
                kernel.Bind<IAvatar>().To<AvatarImpl>().InSingletonScope();
                kernel.Bind<IConsole>().To<DummyConsole>().InSingletonScope();
                kernel.Bind<IEnvironment>().To<EnvironmentImpl>().InSingletonScope();
                kernel.Bind<IGame>().To<GameImpl>().InSingletonScope();
                kernel.Bind<IParticles>().To<DummyParticles>().InSingletonScope();
                kernel.Bind<IPlayer>().To<DummyPlayer>().InSingletonScope();
                kernel.Bind<IRenderer>().To<RendererImpl>().InSingletonScope();
                kernel.Bind<IScene>().To<SceneImpl>().InSingletonScope();
                kernel.Bind<IShaders>().To<DummyShaders>().InSingletonScope();
                kernel.Bind<ISky>().To<DummySky>().InSingletonScope();
                kernel.Bind<IText>().To<DummyText>().InSingletonScope();
                kernel.Bind<ITextures>().To<TexturesImpl>().InSingletonScope();
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
