namespace FrontierSharp.DummyModules {
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System.Drawing;

    using Interfaces;
    using Interfaces.Property;

    class DummySceneImpl : IScene {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public void Init() {
            // Do nothing
        }

        public void Render() {
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

        public void RenderDebug() {
            var modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Begin(PrimitiveType.Triangles);
            {
                GL.Color3(Color.Red);
                GL.Vertex3(-0.2f, -0.2f, 4.0f);

                GL.Color3(Color.Red);
                GL.Vertex3(0.2f, -0.2f, 4.0f);

                GL.Color3(Color.Red);
                GL.Vertex3(0.0f, 0.2f, 4.0f);
            }
            GL.End();
        }
    }
}
