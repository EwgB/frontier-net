namespace FrontierSharp.Animation {
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    using Common;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common.Animation;
    using Common.Util;

    using Ninject.Infrastructure.Language;

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
            for (var i = 0; i < animation.Joints(); i++)
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
                //skinRender.Vertices[index] = Vector3Interpolate (from, to, b.VertexWeights[i]._weight) + offset;
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
                    BoneInflate(childBone, distance, doChildren: true);
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
                .Split(Delimiters);

            ParseFrames(tokens, this);
            ParseMesh(tokens, this);
            ParseNormals(tokens, this);
            ParseUVs(tokens, this);
            ParseWeights(tokens, this);

            Prepare();
            return true;
        }


        #region Utility functions for reading figure data from a file

        private static void ParseFrames(string[] tokens, Figure fig) {
            string token;
            string find;
            bool done;
            GLmatrix matrix;
            GLvector pos;
            vector<GLmatrix> matrix_stack;
            vector<BoneId> bone_stack;
            BoneId queued_bone;
            BoneId queued_parent;
            unsigned i;
            unsigned depth;

            depth = 0;
            done = false;
            matrix.Identity();
            matrix_stack.push_back(matrix);
            token = strtok(NULL, DELIMIT);
            while (strcmp(token, "FRAME"))
                token = strtok(NULL, DELIMIT);
            while (!done) {
                if (find = strstr(token, "}")) {
                    depth--;
                    bone_stack.pop_back();
                    matrix_stack.pop_back();
                    if (depth < 2)
                        done = true;
                }

                if (find = strstr(token, "FRAMETRANSFORMMATRIX")) {
                    //eat the opening brace
                    token = strtok(NULL, DELIMIT);
                    matrix.Identity();
                    for (int x = 0; x < 4; x++) {
                        for (int y = 0; y < 4; y++) {
                            token = strtok(NULL, DELIMIT);
                            matrix.elements[x][y] = (float) atof(token);
                        }
                    }

                    matrix_stack.push_back(matrix);
                    matrix.Identity();
                    for (i = 0; i < matrix_stack.size(); i++)
                        matrix = glMatrixMultiply(matrix, matrix_stack[i]);
                    pos = glMatrixTransformPoint(matrix, glVector(0.0f, 0.0f, 0.0f));
                    pos.x = -pos.x;
                    fig->PushBone(queued_bone, queued_parent, pos);
                    //Now plow through until we find the closing brace
                    while (!(find = strstr(token, "}")))
                        token = strtok(NULL, DELIMIT);
                }

                if (find = strstr(token, "FRAME")) {
                    //Grab the name
                    token = strtok(NULL, DELIMIT);
                    queued_bone = fig->IdentifyBone(token);
                    //eat the open brace
                    token = strtok(NULL, DELIMIT);
                    depth++;
                    bone_stack.push_back(queued_bone);
                    matrix.Identity();
                    for (i = 0; i < matrix_stack.size(); i++)
                        matrix = glMatrixMultiply(matrix, matrix_stack[i]);
                    pos = glMatrixTransformPoint(matrix, glVector(0.0f, 0.0f, 0.0f));
                    //Find the last valid bone in the chain.
                    vector<BoneId>::reverse_iterator rit;
                    queued_parent = BONE_ROOT;
                    for (rit = bone_stack.rbegin(); rit < bone_stack.rend(); ++rit) {
                        if (*rit != BONE_INVALID && *rit != queued_bone) {
                            queued_parent = *rit;
                            break;
                        }
                    }
                }

                token = strtok(NULL, DELIMIT);
            }


        }

        private static void ParseMesh(string[] tokens, Figure fig) {
            string token;
            int count;
            int poly;
            int i;
            GLvector pos;
            int i1, i2, i3, i4;

            token = strtok(NULL, DELIMIT);
            while (strcmp(token, "MESH"))
                token = strtok(NULL, DELIMIT);
            //eat the open brace
            token = strtok(NULL, DELIMIT);
            //get the vert count
            token = strtok(NULL, DELIMIT);
            count = atoi(token);
            //We begin reading the vertex positions
            for (i = 0; i < count; i++) {
                token = strtok(NULL, DELIMIT);
                pos.x = -(float) atof(token);
                token = strtok(NULL, DELIMIT);
                pos.y = (float) atof(token);
                token = strtok(NULL, DELIMIT);
                pos.z = (float) atof(token);
                fig->_skin_static.PushVertex(pos, glVector(0.0f, 0.0f, 0.0f), glVector(0.0f, 0.0f));
            }

            //Directly after the verts are the polys
            token = strtok(NULL, DELIMIT);
            count = atoi(token);
            for (i = 0; i < count; i++) {
                token = strtok(NULL, DELIMIT);
                poly = atoi(token);
                if (poly == 3) {
                    i1 = atoi(strtok(NULL, DELIMIT));
                    i2 = atoi(strtok(NULL, DELIMIT));
                    i3 = atoi(strtok(NULL, DELIMIT));
                    fig->_skin_static.PushTriangle(i1, i2, i3);
                } else if (poly == 4) {
                    i1 = atoi(strtok(NULL, DELIMIT));
                    i2 = atoi(strtok(NULL, DELIMIT));
                    i3 = atoi(strtok(NULL, DELIMIT));
                    i4 = atoi(strtok(NULL, DELIMIT));
                    fig->_skin_static.PushQuad(i1, i2, i3, i4);
                }
            }

        }

        private static void ParseNormals(string[] tokens, Figure fig) {
            string token;
            int count;
            int i;
            GLvector pos;

            token = strtok(NULL, DELIMIT);
            while (strcmp(token, "MESHNORMALS"))
                token = strtok(NULL, DELIMIT);
            //eat the open brace
            token = strtok(NULL, DELIMIT);
            //get the vert count
            token = strtok(NULL, DELIMIT);
            count = atoi(token);
            //We begin reading the normals
            for (i = 0; i < count; i++) {
                token = strtok(NULL, DELIMIT);
                pos.x = -(float) atof(token);
                token = strtok(NULL, DELIMIT);
                pos.y = (float) atof(token);
                token = strtok(NULL, DELIMIT);
                pos.z = (float) atof(token);
                fig->_skin_static._normal[i] = pos;
            }
        }

        private static void ParseUVs(string[] tokens, Figure fig) {
            string token;
            int count;
            int i;
            GLvector2 pos;

            token = strtok(NULL, DELIMIT);
            while (strcmp(token, "MESHTEXTURECOORDS"))
                token = strtok(NULL, DELIMIT);
            //eat the open brace
            token = strtok(NULL, DELIMIT);
            //get the vert count
            token = strtok(NULL, DELIMIT);
            count = atoi(token);
            //We begin reading the normals
            for (i = 0; i < count; i++) {
                token = strtok(NULL, DELIMIT);
                pos.x = (float) atof(token);
                token = strtok(NULL, DELIMIT);
                pos.y = -(float) atof(token);
                fig->_skin_static._uv[i] = pos;
            }
        }

        private static void ParseWeights(string[] tokens, Figure fig) {
            string token;
            unsigned index;
            int count;
            int i;
            BoneId bid;
            vector<BWeight> bw_list;
            BWeight bw;
            vector<PWeight> weights;

            weights.resize(fig->_skin_static._vertex.size());
            for (i = 0; i < (int) weights.size(); i++) {
                PWeight pw;
                pw._bone = BONE_ROOT;
                pw._weight = 0.0f;
                weights[i] = pw;
            }

            while (true) {
                token = strtok(NULL, DELIMIT);
                while (strcmp(token, "SKINWEIGHTS")) {
                    token = strtok(NULL, DELIMIT);
                    if (token == NULL)
                        break;
                }

                if (token == NULL)
                    break;
                //eat the open brace
                token = strtok(NULL, DELIMIT);
                //get the name of this bone
                token = strtok(NULL, DELIMIT);
                bid = CAnim::BoneFromString(token);
                if (bid == BONE_INVALID)
                    continue;
                //get the vert count
                token = strtok(NULL, DELIMIT);
                count = atoi(token);
                bw_list.clear();
                //get the indicies
                for (i = 0; i < count; i++) {
                    token = strtok(NULL, DELIMIT);
                    bw._index = atoi(token);
                    bw_list.push_back(bw);
                }

                //get the weights
                for (i = 0; i < count; i++) {
                    token = strtok(NULL, DELIMIT);
                    bw_list[i]._weight = (float) atof(token);
                }

                /*    
              //Store them
              for (i = 0; i < count; i++) {
                //if (bw_list[i]._weight < 0.9f)
                  //continue;

                //if (bw_list[i]._weight < 0.001f)
                  //continue;
                //fig->_bone[fig->_bone_index[bid]]._vertex_weights.push_back (bw_list[i]);
                if (bw_list[i]._weight < 0.5f)
                  continue;
                bw_list[i]._weight = 1.0f;
                fig->_bone[fig->_bone_index[bid]]._vertex_weights.push_back (bw_list[i]);
              }
              */
                //Now we have a list of all weights for this joint. Find the highest values for each point.
                for (i = 0; i < count; i++) {
                    index = bw_list[i]._index;
                    if (bw_list[i]._weight > weights[index]._weight) {
                        weights[index]._weight = bw_list[i]._weight;
                        weights[index]._bone = bid;
                    }
                }
            }

            //Now we have a list which links each vert to its joint of strongest influence
            for (i = 0; i < (int) weights.size(); i++) {
                bid = weights[i]._bone;
                bw._index = i;
                bw._weight = 1.0f;
                fig->_bone[fig->_bone_index[bid]]._vertex_weights.push_back(bw);
            }

        }

        #endregion
    }
}
