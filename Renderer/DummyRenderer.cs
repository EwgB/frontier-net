namespace FrontierSharp.Renderer {
    using System.Drawing;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common.Property;
    using Common.Renderer;

    internal class DummyRenderer : IRenderer {
        public IProperties Properties => this.RendererProperties;
        public IRendererProperties RendererProperties { get; }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Update() { /* Do nothing */ }

        public void Render() {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Begin(PrimitiveType.Triangles);
            {
                GL.Color3(Color.Red);
                GL.Vertex3(-1.0f, -1.0f, 4.0f);

                GL.Color3(Color.Green);
                GL.Vertex3(1.0f, -1.0f, 4.0f);

                GL.Color3(Color.Blue);
                GL.Vertex3(0.0f, 1.0f, 4.0f);
            }
            GL.End();
        }

        public void RequestLoadingScreen(float progress) { /* Do nothing */ }
        public void ToggleShowMap() { /* Do nothing */ }

    }
}
