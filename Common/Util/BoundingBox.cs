namespace FrontierSharp.Common.Util {
    using System;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    public struct BoundingBox : IRenderable {
        private const float MAX_VALUE = 999999999999999.9f;

        private Vector3 min;
        private Vector3 max;

        public Vector3 Center => (min + max) / 2.0f;
        public Vector3 Size => max - min;

        public void ContainPoint(Vector3 point) {
            min.X = Math.Min(min.X, point.X);
            min.Y = Math.Min(min.Y, point.Y);
            min.Z = Math.Min(min.Z, point.Z);
            max.X = Math.Max(max.X, point.X);
            max.Y = Math.Max(max.Y, point.Y);
            max.Z = Math.Max(max.Z, point.Z);
        }

        public void Clear() {
            max = new Vector3(-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
            min = new Vector3(MAX_VALUE, MAX_VALUE, MAX_VALUE);
        }

        public void Render() {

            //Bottom of box (Assuming z = up)
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(min.X, min.Y, min.Z);
            GL.Vertex3(max.X, min.Y, min.Z);
            GL.Vertex3(max.X, max.Y, min.Z);
            GL.Vertex3(min.X, max.Y, min.Z);
            GL.Vertex3(min.X, min.Y, min.Z);
            GL.End();

            //Top of box
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(min.X, min.Y, max.Z);
            GL.Vertex3(max.X, min.Y, max.Z);
            GL.Vertex3(max.X, max.Y, max.Z);
            GL.Vertex3(min.X, max.Y, max.Z);
            GL.Vertex3(min.X, min.Y, max.Z);
            GL.End();

            //Sides
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(min.X, min.Y, min.Z);
            GL.Vertex3(min.X, min.Y, max.Z);

            GL.Vertex3(max.X, min.Y, min.Z);
            GL.Vertex3(max.X, min.Y, max.Z);

            GL.Vertex3(max.X, max.Y, min.Z);
            GL.Vertex3(max.X, max.Y, max.Z);

            GL.Vertex3(min.X, max.Y, min.Z);
            GL.Vertex3(min.X, max.Y, max.Z);
            GL.End();
        }
    }
}
