namespace FrontierSharp.Renderer {
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System.Drawing;

    using Interfaces.Renderer;
    using Interfaces.Property;
    using System;

    public class DummyRenderer : IRenderer {
        private IRendererProperties properties;
        public IProperties Properties { get { return this.properties; } }
        public IRendererProperties RendererProperties { get { return this.properties; } }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Update() {
            // Do nothing
        }

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

        public void ToggleShowMap() {
            // Do nothing
        }
    }
}
