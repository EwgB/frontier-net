namespace FrontierSharp {
    using NLog;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System;
    using System.Drawing;

    using Interfaces;

    internal class Frontier : GameWindow {

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Modules
        readonly IParticles particles;

        public Frontier(IParticles particles) {
            this.particles = particles;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            Log.Info("Begin startup");

            Title = "Frontier";
            GL.ClearColor(Color.CornflowerBlue);

        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            base.OnRenderFrame(e);
            Log.Trace("OnRender");

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Begin(PrimitiveType.Triangles);
            {
                GL.Color3(1.0f, 0.0f, 0.0f);
                GL.Vertex3(-1.0f, -1.0f, 4.0f);

                GL.Color3(0.0f, 1.0f, 0.0f);
                GL.Vertex3(1.0f, -1.0f, 4.0f);

                GL.Color3(0.0f, 0.0f, 1.0f);
                GL.Vertex3(0.0f, 1.0f, 4.0f);
            }
            GL.End();

            SwapBuffers();
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
