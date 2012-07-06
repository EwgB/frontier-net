/*-----------------------------------------------------------------------------
  Mesh.cs
  2011 Shamus Young
-------------------------------------------------------------------------------
  This class is used for storing groups of verts and polygons.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	struct Mesh {
		public BBox        bbox;
		public List<int>     indices;
		public List<Vector3> vertices, normals;
		public List<Color4>  colors;
		public List<Vector2> uvs;

		public int TriangleCount { get { return indices.Count / 3; } }
		public int VertexCount { get { return vertices.Count; } }
		public int NormalCount { get { return normals.Count; } }

		//public static Mesh operator +(Mesh a, Mesh b);

		public void PushTriangle(int i1, int i2, int i3) {
			indices.Add(i1);
			indices.Add(i2);
			indices.Add(i3);
		}

		public void PushQuad(int i1, int i2, int i3, int i4) {
			PushTriangle (i1, i2, i3);
			PushTriangle (i1, i3, i4);
		}

		public void PushVertex (Vector3 vert, Vector3 normal, Vector2 uv) {
			bbox.ContainPoint(vert);
			vertices.Add(vert);
			normals.Add(normal);
			uvs.Add(uv);
		}

		public void PushVertex (Vector3 vert, Vector3 normal, Color4 color, Vector2 uv) {
			bbox.ContainPoint(vert);
			vertices.Add(vert);
			normals.Add(normal);
			colors.Add(color);
			uvs.Add(uv);
		}

		public void Clear() {
			bbox.Clear();
			vertices.Clear();
			normals.Clear();
			uvs.Clear();
			indices.Clear();
		}

		public void Render() {
			GL.Begin(BeginMode.Triangles);
			for (int i = 0; i < indices.Count; i++) {
				GL.Normal3(normals[indices[i]]);
				GL.TexCoord2(uvs[indices[i]]);
				GL.Vertex3(vertices[indices[i]]);
			}
			GL.End();
		}

		public void RecalculateBoundingBox() {
			bbox.Clear();
			for (int i = 0; i < vertices.Count; i++)
				bbox.ContainPoint(vertices[i]);
		}

		public void CalculateNormals() {
			//Clear any existing normals
			for (int i = 0; i < normals.Count; i++)
				normals[i] = Vector3.Zero;

			//For each triangle... 
			for (int i = 0; i < TriangleCount; i++) {
				int index = i * 3;
				int i0 = indices[index];
				int i1 = indices[index + 1];
				int i2 = indices[index + 2];

				// Convert the 3 edges of the polygon into vectors 
				Vector3[] edge = new Vector3[3];
				edge[0] = vertices[i0] - vertices[i1];
				edge[1] = vertices[i1] - vertices[i2];
				edge[2] = vertices[i2] - vertices[i0];

				// Normalize the vectors 
				edge[0].Normalize();
				edge[1].Normalize();
				edge[2].Normalize();

				// Now get the normals from the cross product of any two of the edge vectors 
				Vector3 normal = Vector3.Cross(edge[2], edge[0] * -1);
				normal.Normalize();

				// Calculate the 3 internal angles of this triangle.
				float dot = Vector3.Dot(edge[2], edge[0]);
				double[] angle = new double[3];

				angle[0] = Math.Acos(-dot);
				if (Double.IsNaN(angle[0]))
					continue;

				angle[1] = Math.Acos(-Vector3.Dot(edge[0], edge[1]));
				if (Double.IsNaN(angle[1]))
					continue;

				angle[2] = Math.PI - (angle[0] + angle[1]);

				// Now weight each normal by the size of the angle so that the triangle with the largest angle
				// at that vertex has the most influence over the direction of the normals.
				normals[i0] += Vector3.Mult(normal, (float) angle[0]);
				normals[i1] += Vector3.Mult(normal, (float) angle[1]);
				normals[i2] += Vector3.Mult(normal, (float) angle[2]);
			}

			//Re-normalize. Done.
			for (int i = 0; i < normals.Count; i++)
				normals[i].Normalize();
		}

		public void CalculateNormalsSeamless() {
			// Clear any existing normals
			List<Vector3> normals_merged = new List<Vector3>();
			for (int i = 0; i < normals.Count; i++)
				normals_merged.Add(Vector3.Zero);

			// Scan through the vert list, and make an alternate list where
			// vertices that share the same location are merged
			List<int> merge_index = new List<int>();
			List<Vector3> verts_merged = new List<Vector3>();
			for (int i = 0; i < vertices.Count; i++) {
				int found = -1;

				// See if there is another vertices in the same position in the merged list
				for (int j = 0; j < merge_index.Count; j++) {
					if (vertices[i] == vertices[merge_index[j]]) {
						merge_index.Add(j);
						verts_merged.Add(vertices[i]);
						found = j;
						break;
					}
				}

				// VertexCount not found, so add another
				if (found == -1) {
					merge_index.Add(verts_merged.Count);
					verts_merged.Add(vertices[i]);
				}
			}

			//For each triangle... 
			for (int i = 0; i < TriangleCount; i++) {
				int index = i * 3;
				int i0 = merge_index[indices[index]];
				int i1 = merge_index[indices[index + 1]];
				int i2 = merge_index[indices[index + 2]];

				// Convert the 3 edges of the polygon into vectors 
				Vector3[] edge = new Vector3[3];
				edge[0] = verts_merged[i0] - verts_merged[i1];
				edge[1] = verts_merged[i1] - verts_merged[i2];
				edge[2] = verts_merged[i2] - verts_merged[i0];

				// Normalize the vectors 
				edge[0].Normalize();
				edge[1].Normalize();
				edge[2].Normalize();

				// Now get the normals from the cross product of any two of the edge vectors 
				Vector3 normal = Vector3.Cross(edge[2], edge[0] * -1);
				normal.Normalize();

				// Calculate the 3 internal angles of this triangle.
				float dot = Vector3.Dot(edge[2], edge[0]);
				double[] angle = new double[3];

				angle[0] = Math.Acos(-dot);
				if (Double.IsNaN(angle[0]))
					continue;

				angle[1] = Math.Acos(-Vector3.Dot(edge[0], edge[1]));
				if (Double.IsNaN(angle[1]))
					continue;
				
				angle[2] = Math.PI - (angle[0] + angle[1]);

				// Now weight each normals by the size of the angle so that the triangle with the largest angle
				// at that vertices has the most influence over the direction of the normals.
				normals_merged[i0] += Vector3.Mult(normal, (float)angle[0]);
				normals_merged[i1] += Vector3.Mult(normal, (float)angle[1]);
				normals_merged[i2] += Vector3.Mult(normal, (float)angle[2]);
			}

			//Re-normalize. Done.
			for (int i = 0; i < normals.Count; i++) {
				normals[i] = normals_merged[merge_index[i]];
				normals[i].Z *= NORMAL_SCALING;
				normals[i].Normalize();
			}
		}
	}
}
