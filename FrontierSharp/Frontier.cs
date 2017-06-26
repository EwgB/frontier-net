namespace FrontierSharp {
    using System;
    using System.Drawing;

    using NLog;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Interfaces;
    using Interfaces.Environment;
    using Interfaces.Particles;
    using Interfaces.Renderer;
    using OpenTK.Input;

    internal class Frontier : GameWindow {

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Modules
        private readonly IAvatar avatar;
        private readonly IConsole console;
        private readonly IEnvironment environment;
        private readonly IGame game;
        private readonly IParticles particles;
        private readonly IPlayer player;
        private readonly IRenderer renderer;
        private readonly IScene scene;
        private readonly IShaders shaders;
        private readonly ISky sky;
        private readonly IText text;
        private readonly ITexture texture;
        private readonly IWorld world;

        // Constants
        private const float MOUSE_SCALING = 0.01f;

        public Frontier(IAvatar avatar,
                        IConsole console,
                        IEnvironment environment,
                        IGame game,
                        IParticles particles,
                        IPlayer player,
                        IRenderer renderer,
                        IScene scene,
                        IShaders shaders,
                        ISky sky,
                        IText text,
                        ITexture texture,
                        IWorld world) {
            this.avatar = avatar;
            this.console = console;
            this.environment = environment;
            this.game = game;
            this.particles = particles;
            this.player = player;
            this.renderer = renderer;
            this.scene = scene;
            this.shaders = shaders;
            this.sky = sky;
            this.text = text;
            this.texture = texture;
            this.world = world;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Info("OnLoad: Begin startup");

            base.Title = "Frontier";
            //Icon = new Icon("Resources/icon.bmp");
            base.Size = new Size(1400, 800);

            base.Keyboard.KeyRepeat = true;

            this.console.Init();
            this.particles.Init();
            //ilInit(); // TODO: what is this?
            this.environment.Init();
            this.renderer.Init();
            this.shaders.Init();
            this.game.Init();
            this.player.Init();
            this.avatar.Init();
            this.texture.Init();
            this.world.Init();
            this.scene.Init();
            this.sky.Init();
            this.text.Init();

            Log.Info("Init done.");
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            var projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            base.OnRenderFrame(e);
            Log.Trace("OnRender");

            this.renderer.Render();

            SwapBuffers();

            //ConsoleUpdate();
            //SdlUpdate();
            //GameUpdate();
            //AvatarUpdate();
            //PlayerUpdate();
            //EnvUpdate();
            //SkyUpdate();
            //SceneUpdate(stop);
            //CacheUpdate(stop);
            //ParticleUpdate();
            //RenderUpdate();
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            base.OnUpdateFrame(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e) {
            base.OnKeyDown(e);

            if (Key.Tab == e.Key) {
                this.renderer.ToggleShowMap();
            }
        }

    }
}
