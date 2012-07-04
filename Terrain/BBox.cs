/*-----------------------------------------------------------------------------
  BBox.cs
  2006 Shamus Young
-------------------------------------------------------------------------------
  This module has a few functions useful for manipulating the bounding-box structs.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	struct BBox {
		private const float MAX_VALUE = 999999999999999.9f;

		public Vector3 pmin, pmax;

		public Vector3 Center { get { return (pmin + pmax) / 2.0f; } }
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
			pmax = new Vector3(-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
			pmin = new Vector3 (MAX_VALUE,  MAX_VALUE,  MAX_VALUE);
		}

		public void Render() {
			// Bottom of box (Assuming z = up)
			GL.Begin(BeginMode.LineStrip);
			GL.Vertex3(pmin.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmin.Y, pmin.Z);
			GL.Vertex3(pmax.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmax.Y, pmin.Z);
			GL.Vertex3(pmin.X, pmin.Y, pmin.Z);
			GL.End();

			// Top of box
			GL.Begin(BeginMode.LineStrip);
			GL.Vertex3(pmin.X, pmin.Y, pmax.Z);
			GL.Vertex3(pmax.X, pmin.Y, pmax.Z);
			GL.Vertex3(pmax.X, pmax.Y, pmax.Z);
			GL.Vertex3(pmin.X, pmax.Y, pmax.Z);
			GL.Vertex3(pmin.X, pmin.Y, pmax.Z);
			GL.End();

			//Sides
			GL.Begin(BeginMode.Lines);
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
