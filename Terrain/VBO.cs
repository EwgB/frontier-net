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
		private int _id_vertex, _id_index, _size_vertex, _size_uv, _size_normal, _size_buffer, _polygon, _index_count, _size_color;
		private bool _use_color;

		public bool Ready { get; private set; }

		public VBO() {
			_id_vertex = _id_index = _size_vertex = _size_uv = _size_normal = _size_buffer = _index_count = 0;
			Ready = false;
			_id_vertex = 0;
			_id_index = 0;
			_use_color = false;
			_size_color = 0;
			_polygon = 0;
		}

		~VBO() {
			if (_id_index != 0) GL.DeleteBuffers(1, ref _id_index);
			if (_id_vertex != 0) GL.DeleteBuffers(1, ref _id_vertex);
		}

		public void Clear() {
			if (_id_vertex != 0) GL.DeleteBuffers(1, ref _id_vertex);
			if (_id_index != 0) GL.DeleteBuffers(1, ref _id_index);
			_id_vertex = 0;
			_id_index = 0;
			_use_color = false;
			_size_color = 0;
			_polygon = 0;
			Ready = false;
		}

		public void Create(int polygon, int index_count, int vert_count, int index_list,
								 Vector3 vert_list, Vector3 normal_list, Color4 color_list, Vector2 uv_list) {
			char*     buffer;

			if (_id_vertex != 0) GL.DeleteBuffers(1, ref _id_vertex);
			if (_id_index != 0) GL.DeleteBuffers(1, ref _id_index);
			_id_vertex = 0;
			_id_index = 0;
			if (index_count == 0 | vert_count == 0)
				return;

			_polygon = polygon;
			_use_color = (color_list != null);
			_size_vertex = _size_normal = Vector3.SizeInBytes * vert_count;
			_size_uv = Vector2.SizeInBytes * vert_count;
			_size_buffer = _size_vertex + _size_normal + _size_uv;

			if (_use_color) {
				_size_color = sizeof(Color4) * vert_count;
				_size_buffer += _size_color;
			} else
				_size_color = 0;

			// Allocate the array and pack the bytes into it.
			buffer = new char[_size_buffer];
			memcpy(buffer, vert_list, _size_vertex);
			memcpy(buffer + _size_vertex, normal_list, _size_normal);

			if (_use_color)
				memcpy(buffer + _size_vertex + _size_normal, color_list, _size_color);
			memcpy(buffer + _size_vertex + _size_normal + _size_color, uv_list, _size_uv);

			// Create and load the buffer
			GL.GenBuffers(1, out _id_vertex);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _id_vertex);			// Bind The Buffer
			GL.BufferData(BufferTarget.ArrayBuffer, _size_buffer, buffer, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);			// Unbind The Buffer

			// Create and load the indicies
			GL.GenBuffers(1, out _id_index);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _id_index);
			GL.BufferData(BufferTarget.ElementArrayBuffer, index_count * sizeof(int), index_list, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Unbind
			_index_count = index_count;
			delete[] buffer;
			Ready = true;
		}

		public void Create(Mesh m) {
			if (m.colors.Count != 0)
				Create(GL_TRIANGLES, m._index.size(), m.Vertices(), &m._index[0], &m._vertex[0], &m._normal[0], &m._color[0], &m._uv[0]);
			else
				Create(GL_TRIANGLES, m._index.size(), m.Vertices(), &m._index[0], &m._vertex[0], &m._normal[0], NULL, &m._uv[0]);
		}

		public void Render() {
			if (!Ready)
				return;

			// Bind VBOs for vertex array and index array
			GL.BindBuffer(BufferTarget.ArrayBuffer, _id_vertex);
			GL.EnableClientState(EnableCap.VertexArray);
			GL.EnableClientState(EnableCap.NormalArray);
			if (_use_color)
				GL.EnableClientState(EnableCap.ColorArray);
			else
				GL.DisableClientState(EnableCap.ColorArray);
			GL.EnableClientState(EnableCap.TextureCoordArray);

			GL.VertexPointer(3, VertexPointerType.Float, 0, 0);
			GL.NormalPointer(NormalPointerType.Float, 0, _size_vertex);
			if (_use_color)
				GL.ColorPointer(4, ColorPointerType.Float, 0, _size_vertex + _size_normal);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, _size_vertex + _size_normal + _size_color);

			// Draw it
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _id_index); // for indices
			GL.EnableClientState(EnableCap.VertexArray);             // activate vertex coords array
			GL.DrawElements(BeginMode.Polygon, _index_count, DrawElementsType.UnsignedInt, 0);

			// Deactivate vertex array and bind with 0, so, switch back to normal pointer operation
			GL.DisableClientState(EnableCap.VertexArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
	}
}