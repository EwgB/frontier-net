namespace FrontierSharp {
    using Ninject;

    using Common;
    using Common.Input;
    using Common.Shaders;

    using Animation;
    using Avatar;
    using DummyModules;
    using Environment;
    using Game;
    using Ninject.Modules;
    using Particles;
    using Renderer;
    using Scene;
    using Textures;
    using World;

    internal class Program {
        private static void Main() {
            var modules = new INinjectModule[] {
                new AnimationModule(true),
                new AvatarModule(true),
                new EnvironmentModule(true), 
                new GameModule(true), 
                new ParticlesModule(true), 
                new RendererModule(true), 
                new SceneModule(true), 
                new TexturesModule(true),
                new WorldModule()
            };

            using (IKernel kernel = new StandardKernel(modules)) {

                // Set up dependecies

                // Modules
                kernel.Bind<ICache>().To<DummyCache>().InSingletonScope();
                kernel.Bind<IConsole>().To<DummyConsole>().InSingletonScope();
                kernel.Bind<IInput>().To<DummyInput>().InSingletonScope();
                kernel.Bind<IPlayer>().To<DummyPlayer>().InSingletonScope();
                kernel.Bind<IShaders>().To<DummyShaders>().InSingletonScope();
                kernel.Bind<ISky>().To<DummySky>().InSingletonScope();
                kernel.Bind<IText>().To<DummyText>().InSingletonScope();
                kernel.Bind<IWater>().To<DummyWater>().InSingletonScope();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
