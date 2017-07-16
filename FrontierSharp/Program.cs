namespace FrontierSharp {
    using Ninject;

    using Animation;
    using Avatar;
    using Cache;
    using Console;
    using Environment;
    using Game;
    using Input;
    using Ninject.Modules;
    using Particles;
    using Player;
    using Renderer;
    using Scene;
    using Shaders;
    using Sky;
    using Text;
    using Textures;
    using Water;
    using World;

    internal class Program {
        private static void Main() {
            var modules = new INinjectModule[] {
                new AnimationModule(true),
                new AvatarModule(true),
                new CacheModule(),
                new ConsoleModule(),
                new EnvironmentModule(true), 
                new GameModule(true),
                new InputModule(),
                new ParticlesModule(true),
                new PlayerModule(),
                new RendererModule(true), 
                new SceneModule(true),
                new ShadersModule(),
                new SkyModule(),
                new TextModule(),
                new TexturesModule(true),
                new WaterModule(),
                new WorldModule()
            };

            using (IKernel kernel = new StandardKernel(modules)) {
                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
