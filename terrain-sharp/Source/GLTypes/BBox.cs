namespace terrain_sharp.Source.GLTypes {
	using System;

	using OpenTK;
	using OpenTK.Graphics.OpenGL;

	///<summary>This module has a few functions useful for manipulating the bounding-box objects.</summary>
	class BBox {
		public Vector3 pmin;
		public Vector3 pmax;

		public Vector3 Center { get { return (pmin + pmax) / 2; } }
		public Vector3 Size { get { return pmax - pmin; } }

		public void ContainPoint(Vector3 point) {
			pmin.X = Math.Min(pmin.X, point.X);
			pmin.Y = Math.Min(pmin.Y, point.Y);
			pmin.Z = Math.Min(pmin.Z, point.Z);
			pmax.X = Math.Max(pmax.X, point.X);
			pmax.Y = Math.Max(pmax.Y, point.Y);
			pmax.Z = Math.Max(pmax.Z, point.Z);
		}

		public void Clear() {
			pmax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			pmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}

		public void Render() {
			//Bottom of box (Assuming z = up)
			GL.Begin(PrimitiveType.LineStrip);
			GL.Vertex3(pmin.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmin.Y, pmin.Z);
			GL.End();
	
			//Top of box
			GL.Begin(PrimitiveType.LineStrip);
			GL.Vertex3(pmin.X, pmin.Y, pmax.Z);
			GL.Vertex3(pmax.X, pmin.Y, pmax.Z);
			GL.Vertex3(pmax.X, pmax.Y, pmax.Z);
			GL.Vertex3(pmin.X, pmax.Y, pmax.Z);
			GL.Vertex3(pmin.X, pmin.Y, pmax.Z);
			GL.End();
		
			//Sides
			GL.Begin(PrimitiveType.Lines);
			GL.Vertex3(pmin.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmin.Y, pmax.Z);

			GL.Vertex3(pmax.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmin.Y, pmax.Z);

			GL.Vertex3(pmax.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmax.Y, pmax.Z);

			GL.Vertex3(pmin.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmax.Y, pmax.Z);
			GL.End();

		}
	}
}
