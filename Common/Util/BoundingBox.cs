namespace FrontierSharp.Common.Util {
    using System;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    public struct BoundingBox : IRenderable {
        private const float MAX_VALUE = 999999999999999.9f;

        private Vector3 min;
        private Vector3 max;

        public Vector3 Center => (this.min + this.max) / 2.0f;
        public Vector3 Size => this.max - this.min;

        public void ContainPoint(Vector3 point) {
            this.min.X = Math.Min(this.min.X, point.X);
            this.min.Y = Math.Min(this.min.Y, point.Y);
            this.min.Z = Math.Min(this.min.Z, point.Z);
            this.max.X = Math.Max(this.max.X, point.X);
            this.max.Y = Math.Max(this.max.Y, point.Y);
            this.max.Z = Math.Max(this.max.Z, point.Z);
        }

        public void Clear() {
            this.max = new Vector3(-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
            this.min = new Vector3(MAX_VALUE, MAX_VALUE, MAX_VALUE);
        }

        public void Render() {

            //Bottom of box (Assuming z = up)
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(this.min.X, this.min.Y, this.min.Z);
            GL.Vertex3(this.max.X, this.min.Y, this.min.Z);
            GL.Vertex3(this.max.X, this.max.Y, this.min.Z);
            GL.Vertex3(this.min.X, this.max.Y, this.min.Z);
            GL.Vertex3(this.min.X, this.min.Y, this.min.Z);
            GL.End();

            //Top of box
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(this.min.X, this.min.Y, this.max.Z);
            GL.Vertex3(this.max.X, this.min.Y, this.max.Z);
            GL.Vertex3(this.max.X, this.max.Y, this.max.Z);
            GL.Vertex3(this.min.X, this.max.Y, this.max.Z);
            GL.Vertex3(this.min.X, this.min.Y, this.max.Z);
            GL.End();

            //Sides
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(this.min.X, this.min.Y, this.min.Z);
            GL.Vertex3(this.min.X, this.min.Y, this.max.Z);

            GL.Vertex3(this.max.X, this.min.Y, this.min.Z);
            GL.Vertex3(this.max.X, this.min.Y, this.max.Z);

            GL.Vertex3(this.max.X, this.max.Y, this.min.Z);
            GL.Vertex3(this.max.X, this.max.Y, this.max.Z);

            GL.Vertex3(this.min.X, this.max.Y, this.min.Z);
            GL.Vertex3(this.min.X, this.max.Y, this.max.Z);
            GL.End();
        }
    }
}
