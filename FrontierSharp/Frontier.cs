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
        readonly IParticles particles;
        readonly IRenderer renderer;

        public Frontier(IParticles particles, IRenderer renderer) {
            this.particles = particles;
            this.renderer = renderer;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Info("Begin startup");

            Title = "Frontier";

            this.renderer.Init();
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
