namespace FrontierSharp {
    using NLog;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System;

    using Interfaces;

    internal class Frontier : GameWindow {

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Modules
        private readonly IParticles particles;
        private readonly IRenderer renderer;
        private readonly IEnvironment environment;

        public Frontier(IParticles particles, IRenderer renderer, IEnvironment environment) {
            this.particles = particles;
            this.renderer = renderer;
            this.environment = environment;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Info("Begin startup");

            Title = "Frontier";

            //ConsoleInit();
            //this.particles.Init();
            //ilInit(); // TODO: what is this?
            //RandomInit(11);
            //SdlInit();
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

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            var projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

    }
}
