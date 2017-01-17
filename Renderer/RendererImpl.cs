﻿namespace FrontierSharp.Renderer {
    using Interfaces;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using System.Drawing;

    public class RendererImpl : IRenderer {
        private readonly IAvatar avatar;

        public RendererImpl(IAvatar avatar) {
            this.avatar = avatar;
        }

        public void Init() {
            GL.ClearColor(Color.CornflowerBlue);
        }

        public void Render() {
            Vector3 pos = this.avatar.GetCameraPosition();


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
        }
    }
}
