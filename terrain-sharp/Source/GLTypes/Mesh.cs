namespace terrain_sharp.Source.GLTypes {
	using OpenTK;
	using OpenTK.Graphics;
  using OpenTK.Graphics.OpenGL;

	using System;
	using System.Collections.Generic;

	using StdAfx;

	/// This class is used for storing groups of verts and polygons.
	class Mesh {
		public BBox _bbox = new BBox();
		public List<int> Indices = new List<int>();
		public List<Vector3> Vertices = new List<Vector3>();
		public List<Vector3> Normals = new List<Vector3>();
		public List<Color4> Colors = new List<Color4>();
		public List<Vector2> UVs = new List<Vector2>();

		public int TriangleCount { get { return Indices.Count / 3; } }

		public void PushTriangle(int i1, int i2, int i3) {
			Indices.Add(i1);
			Indices.Add(i2);
			Indices.Add(i3);
		}

		public void PushQuad(int i1, int i2, int i3, int i4) {
			PushTriangle(i1, i2, i3);
			PushTriangle(i1, i3, i4);
		}

		public void PushVertex(Vector3 vert, Vector3 normal, Vector2 uv) {
			_bbox.ContainPoint(vert);
			Vertices.Add(vert);
			Normals.Add(normal);
			UVs.Add(uv);
		}

		public void PushVertex(Vector3 vert, Vector3 normal, Color4 color, Vector2 uv) {
			_bbox.ContainPoint(vert);
			Vertices.Add(vert);
			Normals.Add(normal);
			Colors.Add(color);
			UVs.Add(uv);
		}

		public void Clear() {
			_bbox.Clear();
			Vertices.Clear();
			Normals.Clear();
			UVs.Clear();
			Indices.Clear();
		}

		public void Render() {
			GL.Begin(PrimitiveType.Triangles);
			foreach (int item in Indices) {
				GL.Normal3(Normals[item]);
				GL.TexCoord2(UVs[item]);
				GL.Vertex3(Vertices[item]);
			}
			GL.End();
		}

		public void RecalculateBoundingBox() {
			_bbox.Clear();
			Vertices.ForEach(vertex => _bbox.ContainPoint(vertex));
		}

		public void CalculateNormals() {
			//Clear any existing normals
			for (int i = 0; i < Normals.Count; i++)
				Normals[i] = Vector3.Zero;

			//For each triangle... 
			for (int i = 0; i < TriangleCount; i++) {
				int index = i * 3;
				int i0 = Indices[index];
				int i1 = Indices[index + 1];
				int i2 = Indices[index + 2];

				// Convert the 3 edges of the polygon into vectors 
				var edge = new Vector3[3];
				edge[0] = Vertices[i0] - Vertices[i1];
				edge[1] = Vertices[i1] - Vertices[i2];
				edge[2] = Vertices[i2] - Vertices[i0];

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
				Normals[i0] = Vector3.Add(Normals[i0], Vector3.Multiply(normal, angle[0]));
				Normals[i1] = Vector3.Add(Normals[i1], Vector3.Multiply(normal, angle[1]));
				Normals[i2] = Vector3.Add(Normals[i2], Vector3.Multiply(normal, angle[2]));
			}

			//Re-normalize. Done.
			Normals.ForEach((v) => v.Normalize());
		}

		public void CalculateNormalsSeamless() {
			//Clear any existing normals
			var normals_merged = new List<Vector3>();
			Normals.ForEach(normal => normals_merged.Add(Vector3.Zero));

			// scan through the vert list, and make an alternate list where
			// verticies that share the same location are merged
			var merge_index = new List<int>();
			var verts_merged = new List<Vector3>();
			foreach (var vertex in Vertices) {
				int found = -1;
				//see if there is another vertex in the same position in the merged list
				for (int i = 0; i < merge_index.Count; i++) {
					if (vertex == Vertices[merge_index[i]]) {
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
			for (int i = 0; i < TriangleCount; i++) {
				int index = i * 3;
				int i0 = merge_index[Indices[index]];
				int i1 = merge_index[Indices[index + 1]];
				int i2 = merge_index[Indices[index + 2]];
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
			for (int i = 0; i < Normals.Count; i++) {
				var normal = normals_merged[merge_index[i]];
				normal.Z *= StdAfx.NORMAL_SCALING;
				normal.Normalize();
				Normals[i] = normal;
			}
		}
	}
}