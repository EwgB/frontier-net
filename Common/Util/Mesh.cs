namespace FrontierSharp.Common.Util {
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using World;

    /// <summary>
    /// Used for storing groups of verts and polygons.
    /// </summary>
    public class Mesh {
        public BoundingBox BoundingBox { get; }
        public IList<int> Indices { get; }
        public IList<Vector3> Vertices { get; }
        public IList<Vector3> Normals { get; private set; }
        public IList<Color> Colors { get; }
        public IList<Vector2> UVs { get; }

        public int TriangleCount => Indices.Count / 3;

        public Mesh() {
            Indices = new List<int>();
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Colors = new List<Color>();
            UVs = new List<Vector2>();
        }

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
            BoundingBox.ContainPoint(vert);
            Vertices.Add(vert);
            Normals.Add(normal);
            UVs.Add(uv);
        }

        public void PushVertex(Vector3 vert, Vector3 normal, Color color, Vector2 uv) {
            BoundingBox.ContainPoint(vert);
            Vertices.Add(vert);
            Normals.Add(normal);
            Colors.Add(color);
            UVs.Add(uv);
        }

        public void Clear() {
            BoundingBox.Clear();
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Indices.Clear();
        }

        public void Render() {
            GL.Begin(PrimitiveType.Triangles);
            foreach (var index in Indices) {
                GL.Normal3(Normals[index]);
                GL.TexCoord2(UVs[index]);
                GL.Vertex3(Vertices[index]);
            }

            GL.End();
        }

        public void RecalculateBoundingBox() {
            BoundingBox.Clear();
            foreach (var vertex in Vertices)
                BoundingBox.ContainPoint(vertex);
        }

        public void CalculateNormals() {
            // Clear any existing normals
            Normals = Enumerable.Repeat(Vector3.Zero, Normals.Count).ToList();

            //For each triangle... 
            for (var i = 0; i < TriangleCount; i++) {
                var index = i * 3;
                var indices = (
                    i0: Indices[index],
                    i1: Indices[index + 1],
                    i2: Indices[index + 2]
                );

                var normals = CalculateNormalsForTriangle(indices, Vertices);
                if (normals.HasValue) {
                    Normals[indices.i0] += normals.Value.normal0;
                    Normals[indices.i1] += normals.Value.normal1;
                    Normals[indices.i2] += normals.Value.normal2;
                }
            }

            //Re-normalize. Done.
            foreach (var normal in Normals)
                normal.Normalize();
        }

        public void CalculateNormalsSeamless() {
            // Initialize list of merged normals
            var mergedNormals = Enumerable.Repeat(Vector3.Zero, Normals.Count).ToList();

            // Scan through the vert list, and make an alternate list where
            // vertices that share the same location are merged
            var mergedIndices = new List<int>();
            var mergedVertices = new List<Vector3>();
            foreach (var vertex in Vertices) {
                var found = -1;

                // See if there is another vertex in the same position in the merged list
                for (var j = 0; j < mergedIndices.Count; j++) {
                    if (vertex == Vertices[mergedIndices[j]]) {
                        mergedIndices.Add(j);
                        mergedVertices.Add(vertex);
                        found = j;
                        break;
                    }
                }

                //vertex not found, so add another
                if (found == -1) {
                    mergedIndices.Add(mergedVertices.Count);
                    mergedVertices.Add(vertex);
                }
            }

            // For each triangle... 
            for (var i = 0; i < TriangleCount; i++) {
                var index = i * 3;
                var indices = (
                    i0: mergedIndices[Indices[index]],
                    i1: mergedIndices[Indices[index + 1]],
                    i2: mergedIndices[Indices[index + 2]]
                );

                var normals = CalculateNormalsForTriangle(indices, mergedVertices);
                if (normals.HasValue) {
                    mergedNormals[indices.i0] += normals.Value.normal0;
                    mergedNormals[indices.i1] += normals.Value.normal1;
                    mergedNormals[indices.i2] += normals.Value.normal2;
                }
            }

            //Re-normalize. Done.
            for (var i = 0; i < Normals.Count; i++) {
                var normal = mergedNormals[mergedIndices[i]];
                normal.Z *= WorldUtils.NORMAL_SCALING;
                normal.Normalize();
                Normals[i] = normal;
            }
        }

        private static (Vector3 normal0, Vector3 normal1, Vector3 normal2)? CalculateNormalsForTriangle(
            (int i0, int i1, int i2) indices, IList<Vector3> vertices) {

            // Convert the 3 edges of the polygon into vectors 
            var (edge0, edge1, edge2) = (
                vertices[indices.i0] - vertices[indices.i1],
                vertices[indices.i1] - vertices[indices.i2],
                vertices[indices.i2] - vertices[indices.i0]
            );

            // Normalize the vectors 
            edge0.Normalize();
            edge1.Normalize();
            edge2.Normalize();

            // Now get the normal from the cross product of any two of the edge vectors 
            var normal = Vector3.Cross(edge2, edge0 * -1);
            normal.Normalize();

            // Calculate the 3 internal angles of this triangle.
            var dot = Vector3.Dot(edge2, edge0);
            var angle0 = (float) Math.Acos(-dot);
            if (float.IsNaN(angle0))
                return null;
            var angle1 = (float) Math.Acos(-Vector3.Dot(edge0, edge1));
            if (float.IsNaN(angle1))
                return null;
            var angle2 = MathHelper.Pi - (angle0 + angle1);

            // Now weight each normal by the size of the angle so that the triangle 
            // with the largest angle at that vertex has the most influence over the 
            // direction of the normal.
            var normals = (
                normal0: normal * angle0,
                normal1: normal * angle1,
                normal2: normal * angle2
            );
            return normals;
        }
    }
}
