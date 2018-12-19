﻿namespace FrontierSharp.Animation {
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    using Common;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common.Animation;
    using Common.Util;

    internal class Figure : IFigure {
        private readonly IDictionary<BoneId, Bone> bones = new Dictionary<BoneId, Bone>();
        private readonly Mesh skinStatic = new Mesh(); //The original, "read only"
        private Mesh skinDeform = new Mesh(); //Altered
        private Mesh skinRender; //Updated every frame
        private int unknownCount;

        private static readonly char[] Delimiters = {'\n', '\r', '\t', ' ', ';', ',', '\"'};

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Mesh Skin => skinStatic;


        #region Dependecies

        private IAnimation Animation { get; }
        private IConsole Console { get; }

        #endregion


        public Figure(IAnimation animation, IConsole console) {
            Animation = animation;
            Console = console;
        }

        public void Render() {
            // TODO
            GL.Color3(1, 1, 1);
            GL.PushMatrix();
            //CgSetOffset(Position);
            GL.Translate(Position);
            //CgUpdateMatrix();
            //skinRender.Render();
            //CgSetOffset(Vector3.Zero);
            GL.PopMatrix();
            //CgUpdateMatrix();
        }

        public void Animate(IAnimation animation, float delta) {
            // TODO
            if (delta > 1)
                delta -= (int) delta;
            var aj = animation.GetFrame(delta);
            for (var i = 0; i < animation.JointCount; i++)
                RotateBone(aj[i].Id, aj[i].Rotation);
        }

        private void RotateBone(BoneId id, Vector3 angle) {
            if (bones.ContainsKey(id)) {
                var bone = bones[id];
                bone.Rotation = angle;
                bones[id] = bone;
            }
        }

        public void RenderSkeleton() {
            // TODO
            GL.LineWidth(12);
            GL.PushMatrix();
            GL.Translate(Position);
            //CgUpdateMatrix();
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);

            foreach (var bone in bones.Values) {
                if (bones.TryGetValue(bone.IdParent, out var parent)) {
                    GL.Color3((Color) bone.Color);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(bone.Position);
                    GL.Vertex3(parent.Position);
                    GL.End();
                }
            }

            GL.LineWidth(1);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.PopMatrix();
            //CgUpdateMatrix();
        }

        public void Update() {
            skinRender = skinDeform;
            foreach (var (boneId, bone) in bones) {
                var newBone = bone;
                newBone.Position = newBone.Origin;
                bones[boneId] = newBone;
            }

            foreach (var bone in bones.Values) {
                if (bone.Rotation.Equals(Vector3.Zero))
                    continue;
                var rotationMatrix = Matrix3.CreateRotationX(bone.Rotation.X) *
                                     Matrix3.CreateRotationY(bone.Rotation.Y) *
                                     Matrix3.CreateRotationZ(bone.Rotation.Z);
                RotatePoints(bone.Id, bone.Position, rotationMatrix);
                foreach (var childBone in bone.Children) {
                    //Root is self-parent, but shouldn't rotate self!
                    if (childBone != BoneId.Root) {
                        RotateHierarchy(childBone, bone.Position, rotationMatrix);
                    }
                }
            }
        }

        private void Clear() {
            unknownCount = 0;
            skinStatic.Clear();
            skinDeform.Clear();
            skinRender.Clear();
            bones.Clear();
        }

        private void Prepare() {
            skinDeform = skinStatic;
        }

        private void RotatePoints(BoneId id, Vector3 offset, Matrix3 m) {
            var bone = bones[id];
            foreach (var weight in bone.VertexWeights) {
                var index = weight.Index;
                skinRender.Vertices[index] = m * (skinRender.Vertices[index] - offset) + offset;
                //from = skinRender.Vertices[index] - offset;
                //to = GL.MatrixTransformPoint (m, from);
                //movement = movement - skinStatic.Vertices[index]; 
                //skinRender.Vertices[index] = Vector3Interpolate (from, to, b.VertexWeights[i].Weight) + offset;
            }
        }

        private void RotateHierarchy(BoneId id, Vector3 offset, Matrix3 m) {
            var bone = bones[id];
            bone.Position = m * (bone.Position - offset) + offset;
            RotatePoints(id, offset, m);
            foreach (var childBone in bone.Children) {
                if (childBone != BoneId.Root) {
                    RotateHierarchy(childBone, offset, m);
                }
            }
        }

        private void SetRotation(Vector3 rotation) {
            if (!bones.TryGetValue(BoneId.Root, out var bone)) {
                bone = new Bone();
            }

            bone.Rotation = rotation;
            bones[BoneId.Root] = bone;
        }

        private void AddWeight(BoneId id, int index, float weight) =>
            bones[id].VertexWeights.Add(new BWeight {Index = index, Weight = weight});

        private void AddBone(BoneId id, BoneId parent, Vector3 pos) {
            var bone = new Bone {
                Id = id,
                IdParent = parent,
                Position = pos,
                Origin = pos,
                Rotation = Vector3.Zero,
                Children = new List<BoneId>(),
                Color = ColorUtils.UniqueColor((int) id + 1)
            };
            bones.Add(id, bone);
            bones[parent].Children.Add(id);
        }

        private void InflateBone(BoneId id, float distance, bool doChildren) {
            var bone = bones[id];
            foreach (var weight in bone.VertexWeights) {
                skinDeform.Vertices[weight.Index] =
                    skinStatic.Vertices[weight.Index] + skinStatic.Normals[weight.Index] * distance;
            }

            if (doChildren)
                foreach (var childBone in bone.Children) {
                    InflateBone(childBone, distance, doChildren: true);
                }
        }

        /// <summary>
        /// Takes a string and turn it into a BoneId, using unknowns as needed
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private BoneId IdentifyBone(string name) {
            var bid = Animation.BoneFromString(name);
            //If CAnim couldn't make sense of the name, or if that id is already in use...
            if (bid == BoneId.Invalid || bones.ContainsKey(bid)) {
                Console.Log($"Couldn't id Bone '{name}'.");
                bid = (BoneId) ((int) BoneId.Unknown0 + unknownCount);
                unknownCount++;
            }

            return bid;
        }

        private bool LoadFromFile(string filename) {
            Clear();
            var tokens = File
                .ReadAllText(filename)
                .ToUpperInvariant()
                .Split(Delimiters)
                .AsEnumerable()
                .GetEnumerator();

            ParseFrames(tokens, this);
            ParseMesh(tokens, this);
            ParseNormals(tokens, this);
            ParseUVs(tokens, this);
            ParseWeights(tokens, this);

            Prepare();
            return true;
        }


        #region Utility functions for reading figure data from a file

        private static string NextToken(IEnumerator<string> tokens) {
            return tokens.MoveNext() ? tokens.Current : null;
        }

        private static void ParseFrames(IEnumerator<string> tokens, Figure fig) {
            // Find first occurence of FRAME
            var token = NextToken(tokens);
            while (token != "FRAME")
                token = NextToken(tokens);

            var matrixStack = new Stack<Matrix4>();
            matrixStack.Push(Matrix4.Identity);

            var boneStack = new Stack<BoneId>();

            var depth = 0;
            var done = false;
            BoneId
                queuedBone = BoneId.Invalid,
                queuedParent = BoneId.Invalid;
            while (!done) {
                if (token.Contains("}")) {
                    depth--;
                    boneStack.Pop();
                    matrixStack.Pop();
                    if (depth < 2)
                        done = true;
                }

                if (token.Contains("FRAMETRANSFORMMATRIX")) {
                    // Eat the opening brace
                    NextToken(tokens);
                    var matrix = new Matrix4();
                    for (var x = 0; x < 4; x++) {
                        for (var y = 0; y < 4; y++) {
                            token = NextToken(tokens);
                            matrix[x, y] = float.Parse(token);
                        }
                    }
                    matrixStack.Push(matrix);

                    matrix = matrixStack
                        .Aggregate(Matrix4.Identity, (left, right) => left * right);
                    var pos = new Vector3(matrix.Column3);
                    pos.X = -pos.X;
                    fig.AddBone(queuedBone, queuedParent, pos);
                    // Now plow through until we find the closing brace
                    while (!token.Contains("}"))
                        token = NextToken(tokens);
                }

                if (token.Contains("FRAME")) {
                    // Grab the name
                    token = NextToken(tokens);
                    queuedBone = fig.IdentifyBone(token);
                    //eat the open brace
                    NextToken(tokens);
                    depth++;
                    boneStack.Push(queuedBone);

                    //Find the last valid bone in the chain.
                    queuedParent = boneStack
                        .Where(bone => bone != BoneId.Invalid && bone != queuedBone)
                        .DefaultIfEmpty(BoneId.Root)
                        .Last();
                }

                token = NextToken(tokens);
            }


        }

        private static void ParseMesh(IEnumerator<string> tokens, Figure fig) {
            var token = NextToken(tokens);
            while (token != "MESH")
                token = NextToken(tokens);

            // Eat the open brace
            NextToken(tokens);

            // Get the vert count
            token = NextToken(tokens);
            var count = int.Parse(token);
            // We begin reading the vertex positions
            for (var i = 0; i < count; i++) {
                Vector3 pos;
                pos.X = -float.Parse(NextToken(tokens));
                pos.Y = float.Parse(NextToken(tokens));
                pos.Z = float.Parse(NextToken(tokens));
                fig.skinStatic.PushVertex(pos, Vector3.Zero, Vector2.Zero);
            }

            // Directly after the verts are the polys
            token = NextToken(tokens);
            count = int.Parse(token);
            for (var i = 0; i < count; i++) {
                token = NextToken(tokens);
                var poly = int.Parse(token);

                if (poly == 3) {
                    var i1 = int.Parse(NextToken(tokens));
                    var i2 = int.Parse(NextToken(tokens));
                    var i3 = int.Parse(NextToken(tokens));
                    fig.skinStatic.PushTriangle(i1, i2, i3);
                } else if (poly == 4) {
                    var i1 = int.Parse(NextToken(tokens));
                    var i2 = int.Parse(NextToken(tokens));
                    var i3 = int.Parse(NextToken(tokens));
                    var i4 = int.Parse(NextToken(tokens));
                    fig.skinStatic.PushQuad(i1, i2, i3, i4);
                }
            }
        }

        private static void ParseNormals(IEnumerator<string> tokens, Figure fig) {
            var token = NextToken(tokens);
            while (token != "MESHNORMALS")
                token = NextToken(tokens);
            
            // Eat the open brace
            NextToken(tokens);
            
            // Get the vert count
            token = NextToken(tokens);
            var count = int.Parse(token);
            
            // We begin reading the normals
            for (var i = 0; i < count; i++) {
                Vector3 pos;
                pos.X = -float.Parse(NextToken(tokens));
                pos.Y = float.Parse(NextToken(tokens));
                pos.Z = float.Parse(NextToken(tokens));
                fig.skinStatic.Normals[i] = pos;
            }
        }

        private static void ParseUVs(IEnumerator<string> tokens, Figure fig) {
            var token = NextToken(tokens);
            while (!token.Equals("MESHTEXTURECOORDS"))
                token = NextToken(tokens);
            // Eat the open brace
            NextToken(tokens);
            
            // Get the vert count
            token = NextToken(tokens);
            var count = int.Parse(token);
            
            // We begin reading the UVs
            for (var i = 0; i < count; i++) {
                Vector2 pos;
                pos.X = float.Parse(NextToken(tokens));
                pos.Y = -float.Parse(NextToken(tokens));
                fig.skinStatic.UVs[i] = pos;
            }
        }

        private static void ParseWeights(IEnumerator<string> tokens, Figure fig) {
            var weights = new List<PWeight>(fig.skinStatic.Vertices.Count);
            for (var i = 0; i < weights.Count; i++) {
                var pw = new PWeight {Bone = BoneId.Root, Weight = 0};
                weights[i] = pw;
            }

            while (true) {
                var token = NextToken(tokens);
                while (!token.Equals("SKINWEIGHTS")) {
                    token = NextToken(tokens);
                    if (token == null)
                        break;
                }

                if (token == null)
                    break;
                
                // Eat the open brace
                NextToken(tokens);
                
                // Get the name of this bone
                token = NextToken(tokens);
                var bone = fig.Animation.BoneFromString(token);
                if (bone == BoneId.Invalid)
                    continue;
                
                // Get the vert count
                var count = int.Parse(NextToken(tokens));
                var boneWeightList = new List<BWeight>();
                
                // Get the indicies
                for (var i = 0; i < count; i++) {
                    boneWeightList.Add(new BWeight { Index = int.Parse(NextToken(tokens)) });
                }

                // Get the weights
                for (var i = 0; i < count; i++) {
                    var boneWeight = boneWeightList[i];
                    boneWeight.Weight = float.Parse(NextToken(tokens));
                    boneWeightList[i] = boneWeight;
                }

                // Store them
                //for (var i = 0; i < count; i++) {
                //    //if (bw_list[i].Weight < 0.9f)
                //    //continue;

                //    //if (bw_list[i].Weight < 0.001f)
                //    //continue;
                //    //fig.bones[fig.bonesIndex[bid]].VerticesWeights.Add (bw_list[i]);
                //    if (bw_list[i].Weight < 0.5f)
                //        continue;
                //    bw_list[i].Weight = 1.0f;
                //    fig.bones[fig.bonesIndex[bid]].VerticesWeights.Add(bw_list[i]);
                //}

                // Now we have a list of all weights for this joint.
                // Find the highest values for each point.
                for (var i = 0; i < count; i++) {
                    var index = boneWeightList[i].Index;
                    if (boneWeightList[i].Weight > weights[index].Weight) {
                        weights[index] = new PWeight {Bone = bone, Weight = boneWeightList[i].Weight};
                    }
                }
            }

            // Now we have a list which links each vert to its joint of strongest influence
            for(var i = 0; i < weights.Count; i++) {
                fig.bones[weights[i].Bone].VertexWeights.Add(new BWeight {Index = i, Weight = 1});
            }
        }

        #endregion
    }
}
