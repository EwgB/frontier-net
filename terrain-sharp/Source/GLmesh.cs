namespace terrain_sharp.Source {
	using OpenTK;
	using OpenTK.Graphics;
  using OpenTK.Graphics.OpenGL;

	using System;
	using System.Collections.Generic;

	/// This class is used for storing groups of verts and polygons.
	class GLmesh {
		public GLbbox _bbox = new GLbbox();
		public List<int> _index = new List<int>();
		public List<Vector3> _vertex = new List<Vector3>();
		public List<Vector3> _normal = new List<Vector3>();
		public List<Color4> _color = new List<Color4>();
		public List<Vector2> _uv = new List<Vector2>();

		public int TriangleCount() { return _index.Count / 3; }

		public void PushTriangle(int i1, int i2, int i3) {
			_index.Add(i1);
			_index.Add(i2);
			_index.Add(i3);
		}

		public void PushQuad(int i1, int i2, int i3, int i4) {
			PushTriangle(i1, i2, i3);
			PushTriangle(i1, i3, i4);
		}

		public void PushVertex(Vector3 vert, Vector3 normal, Vector2 uv) {
			_bbox.ContainPoint(vert);
			_vertex.Add(vert);
			_normal.Add(normal);
			_uv.Add(uv);
		}

		public void PushVertex(Vector3 vert, Vector3 normal, Color4 color, Vector2 uv) {
			_bbox.ContainPoint(vert);
			_vertex.Add(vert);
			_normal.Add(normal);
			_color.Add(color);
			_uv.Add(uv);
		}

		public void Clear() {
			_bbox.Clear();
			_vertex.Clear();
			_normal.Clear();
			_uv.Clear();
			_index.Clear();
		}

		public void Render() {
			GL.Begin(PrimitiveType.Triangles);
			foreach (int item in _index) {
				GL.Normal3(_normal[item]);
				GL.TexCoord2(_uv[item]);
				GL.Vertex3(_vertex[item]);
			}
			GL.End();
		}

		public void RecalculateBoundingBox() {
			_bbox.Clear();
			_vertex.ForEach(vertex => _bbox.ContainPoint(vertex));
		}

		public void CalculateNormals() {
			//Clear any existing normals
			for (int i = 0; i < _normal.Count; i++)
				_normal[i] = new Vector3();

			//For each triangle... 
			for (int i = 0; i < TriangleCount(); i++) {
				int index = i * 3;
				int i0 = _index[index];
				int i1 = _index[index + 1];
				int i2 = _index[index + 2];

				// Convert the 3 edges of the polygon into vectors 
				var edge = new Vector3[3];
				edge[0] = _vertex[i0] - _vertex[i1];
				edge[1] = _vertex[i1] - _vertex[i2];
				edge[2] = _vertex[i2] - _vertex[i0];

				// Normalize the vectors 
				edge[0].Normalize();
				edge[1].Normalize();
				edge[2].Normalize();

				// Now get the normal from the cross product of any two of the edge vectors 
				Vector3 normal = Vector3.Cross(edge[2], edge[0] * -1);
				normal.Normalize();

				// Calculate the 3 internal angles of this triangle.
				float dot = Vector3.Dot(edge[2], edge[0]);
				var angle = new float[3];
				angle[0] = (float) Math.Acos(-dot);
				if (float.IsNaN(angle[0]))
					continue;
				angle[1] = (float) Math.Acos(-Vector3.Dot(edge[0], edge[1]));
				if (float.IsNaN(angle[1]))
					continue;
				angle[2] = (float) (Math.PI - (angle[0] + angle[1]));
				//Now weight each normal by the size of the angle so that the triangle 
				//with the largest angle at that vertex has the most influence over the 
				//direction of the normal.
				_normal[i0] = Vector3.Add(_normal[i0], Vector3.Multiply(normal, angle[0]));
				_normal[i1] = Vector3.Add(_normal[i1], Vector3.Multiply(normal, angle[1]));
				_normal[i2] = Vector3.Add(_normal[i2], Vector3.Multiply(normal, angle[2]));
			}

			//Re-normalize. Done.
			_normal.ForEach((v) => v.Normalize());
		}

		public void CalculateNormalsSeamless() {
			//Clear any existing normals
			var normals_merged = new List<Vector3>();
			_normal.ForEach(normal => normals_merged.Add(new Vector3()));

			// scan through the vert list, and make an alternate list where
			// verticies that share the same location are merged
			var merge_index = new List<int>();
			var verts_merged = new List<Vector3>();
			foreach (var vertex in _vertex) {
				int found = -1;
				//see if there is another vertex in the same position in the merged list
				for (int i = 0; i < merge_index.Count; i++) {
					if (vertex == _vertex[merge_index[i]]) {
						merge_index.Add(i);
						verts_merged.Add(vertex);
						found = i;
						break;
					}
				}
				//vertex not found, so add another
				if (found == -1) {
					merge_index.Add(verts_merged.Count);
					verts_merged.Add(vertex);
				}
			}

			//For each triangle... 
			for (int i = 0; i < TriangleCount(); i++) {
				int index = i * 3;
				int i0 = merge_index[_index[index]];
				int i1 = merge_index[_index[index + 1]];
				int i2 = merge_index[_index[index + 2]];
				// Convert the 3 edges of the polygon into vectors 
				var edge = new Vector3[3];
				edge[0] = verts_merged[i0] - verts_merged[i1];
				edge[1] = verts_merged[i1] - verts_merged[i2];
				edge[2] = verts_merged[i2] - verts_merged[i0];
				// normalize the vectors 
				edge[0].Normalize();
				edge[1].Normalize();
				edge[2].Normalize();
				// now get the normal from the cross product of any two of the edge vectors 
				Vector3 normal = Vector3.Cross(edge[2], edge[0] * -1);
				normal.Normalize();
				//calculate the 3 internal angles of this triangle.
				float dot = Vector3.Dot(edge[2], edge[0]);
				var angle = new float[3];
				angle[0] = (float) Math.Acos(-dot);
				if (float.IsNaN(angle[0]))
					continue;
				angle[1] = (float) Math.Acos(-Vector3.Dot(edge[0], edge[1]));
				if (float.IsNaN(angle[1]))
					continue;
				angle[2] = (float) (Math.PI - (angle[0] + angle[1]));
				//Now weight each normal by the size of the angle so that the triangle 
				//with the largest angle at that vertex has the most influence over the 
				//direction of the normal.
				normals_merged[i0] += normal * angle[0];
				normals_merged[i1] += normal * angle[1];
				normals_merged[i2] += normal * angle[2];
			}
			//Re-normalize. Done.
			for (int i = 0; i < _normal.Count; i++) {
				var normal = normals_merged[merge_index[i]];
				normal.Z *= NORMAL_SCALING;
				normal.Normalize();
				_normal[i] = normal;
			}
		}
	}
}