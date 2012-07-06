/*-----------------------------------------------------------------------------
  VBO.cpp
-------------------------------------------------------------------------------
  This class manages vertex buffer objects.  Take a list of verticies and 
  indexes, and store them in GPU memory for fast rendering.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class VBO {
		private int mIDVertex, mIDIndex, mSizeVertex, mSizeUV, mSizeNormal, mSizeBuffer, mIndexCount, mSizeColor;
		private bool mUseColor;

		private BeginMode mPolygon;

		public bool Ready { get; private set; }

		public VBO() {
			mIDVertex = mIDIndex = mSizeVertex = mSizeUV = mSizeNormal = mSizeBuffer = mIndexCount = 0;
			Ready = false;
			mIDVertex = 0;
			mIDIndex = 0;
			mUseColor = false;
			mSizeColor = 0;
			mPolygon = 0;
		}

		~VBO() {
			if (mIDIndex != 0) GL.DeleteBuffers(1, ref mIDIndex);
			if (mIDVertex != 0) GL.DeleteBuffers(1, ref mIDVertex);
		}

		public void Clear() {
			if (mIDVertex != 0) GL.DeleteBuffers(1, ref mIDVertex);
			if (mIDIndex != 0) GL.DeleteBuffers(1, ref mIDIndex);
			mIDVertex = 0;
			mIDIndex = 0;
			mUseColor = false;
			mSizeColor = 0;
			mPolygon = 0;
			Ready = false;
		}

		public void Create(BeginMode polygon, int indexCount, int vertCount, List<int> indexList, List<Vector3> vertList,
				List<Vector3> normalList, List<Color4> colorList, List<Vector2> uvList) {

			if (mIDVertex != 0) GL.DeleteBuffers(1, ref mIDVertex);
			if (mIDIndex != 0) GL.DeleteBuffers(1, ref mIDIndex);
			mIDVertex = 0;
			mIDIndex = 0;
			if (indexCount == 0 | vertCount == 0)
				return;

			mPolygon = polygon;
			mUseColor = (colorList != null);
			mSizeVertex = mSizeNormal = Vector3.SizeInBytes * vertCount;
			mSizeUV = Vector2.SizeInBytes * vertCount;
			mSizeBuffer = mSizeVertex + mSizeNormal + mSizeUV;

			if (mUseColor) {
				mSizeColor = 4 * vertCount;
				mSizeBuffer += mSizeColor;
			} else
				mSizeColor = 0;

			// Allocate the array and pack the bytes into it.
			float[] buffer = new float[mSizeBuffer];

			for (int i = 0, j = 0; j < vertList.Count; i += 3, j++) {
				buffer[i + 0] = vertList[j].X;
				buffer[i + 1] = vertList[j].Y;
				buffer[i + 2] = vertList[j].Z;
			}

			for (int i = mSizeVertex, j = 0; j < normalList.Count; i += 3, j++) {
				buffer[i + 0] = normalList[j].X;
				buffer[i + 1] = normalList[j].Y;
				buffer[i + 2] = normalList[j].Z;
			}

			if (mUseColor) {
				for (int i = mSizeVertex + mSizeNormal, j = 0; j < colorList.Count; i += 3, j++) {
					buffer[i + 0] = colorList[j].R;
					buffer[i + 1] = colorList[j].G;
					buffer[i + 2] = colorList[j].B;
				}
			}

			for (int i = mSizeVertex + mSizeNormal + (mUseColor ? mSizeColor : 0), j = 0; j < uvList.Count; i += 2, j++) {
				buffer[i + 0] = uvList[j].X;
				buffer[i + 1] = uvList[j].Y;
			}

			// Create and load the buffer
			GL.GenBuffers(1, out mIDVertex);
			GL.BindBuffer(BufferTarget.ArrayBuffer, mIDVertex);			// Bind The Buffer
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(mSizeBuffer), buffer, BufferUsageHint.StaticDraw);
			//GL.BindBuffer(BufferTarget.ArrayBuffer, 0);			// Unbind The Buffer

			// Create and load the indicies
			GL.GenBuffers(1, out mIDIndex);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIDIndex);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexCount * 4), indexList.ToArray(), BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Unbind
			mIndexCount = indexCount;

			Ready = true;
		}

		public void Create(Mesh m) {
			if (m.colors.Count != 0)
				Create(BeginMode.Triangles, m.indices.Count, m.VertexCount, m.indices, m.vertices, m.normals, m.colors, m.uvs);
			else
				Create(BeginMode.Triangles, m.indices.Count, m.VertexCount, m.indices, m.vertices, m.normals, null, m.uvs);
		}

		public void Render() {
			if (!Ready)
				return;

			// Bind VBOs for vertex array and index array
			GL.BindBuffer(BufferTarget.ArrayBuffer, mIDVertex);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			if (mUseColor)
				GL.EnableClientState(ArrayCap.ColorArray);
			else
				GL.DisableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);

			GL.VertexPointer(3, VertexPointerType.Float, 0, 0);
			GL.NormalPointer(NormalPointerType.Float, 0, mSizeVertex);
			if (mUseColor)
				GL.ColorPointer(4, ColorPointerType.Float, 0, mSizeVertex + mSizeNormal);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, mSizeVertex + mSizeNormal + mSizeColor);

			// Draw it
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, mIDIndex); // for indices
			GL.EnableClientState(ArrayCap.VertexArray);             // activate vertex coords array
			GL.DrawElements(BeginMode.Polygon, mIndexCount, DrawElementsType.UnsignedInt, 0);

			// Deactivate vertex array and bind with 0, so, switch back to normal pointer operation
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
	}
}