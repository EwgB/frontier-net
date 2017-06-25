namespace FrontierSharp {
    using System;
    using System.Drawing;

    using NLog;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Interfaces.Environment;
    using Interfaces.Particles;
    using Interfaces.Renderer;

    internal class Frontier : GameWindow {

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Modules
        private readonly IParticles particles;
        private readonly IRenderer renderer;
        private readonly IEnvironment environment;

        // Constants
        private const float MOUSE_SCALING = 0.01f;

        public Frontier(IParticles particles, IRenderer renderer, IEnvironment environment) {
            this.particles = particles;
            this.renderer = renderer;
            this.environment = environment;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Info("OnLoad: Begin startup");

            base.Title = "Frontier";
            //Icon = new Icon("Resources/icon.bmp");
            base.Size = new Size(1400, 800);

            base.Keyboard.KeyRepeat = true;

            //ConsoleInit();
            //this.particles.Init();

            //ilInit(); // TODO: what is this?

            this.environment.Init();
            this.renderer.Init();
            //CgInit();
            //GameInit();
            //PlayerInit();
            //AvatarInit();
            //TextureInit();
            //WorldInit();
            //SceneInit();
            //SkyInit();
            //TextInit();

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

    }
}
