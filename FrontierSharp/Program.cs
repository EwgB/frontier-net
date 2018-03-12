namespace FrontierSharp {
    using Ninject;
    using Ninject.Modules;

    using Animation;
    using Avatar;
    using Cache;
    using Console;
    using Environment;
    using Game;
    using Input;
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
                new AnimationModule(false),
                new AvatarModule(false),
                new CacheModule(false),
                new ConsoleModule(),
                new EnvironmentModule(false), 
                new GameModule(false),
                new InputModule(),
                new ParticlesModule(false),
                new PlayerModule(),
                new RendererModule(false),
                new SceneModule(false),
                new ShadersModule(),
                new SkyModule(),
                new TextModule(),
                new TexturesModule(false),
                new WaterModule(),
                new WorldModule(true)
            };

            using (IKernel kernel = new StandardKernel(modules)) {
                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
