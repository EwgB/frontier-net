namespace terrain_sharp.Source.CFigure {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using OpenTK;
	using OpenTK.Graphics.OpenGL;

	using CAnim;
	using GLTypes;

	///<summary>Animated models.</summary>
	class CFigure {
		public List<Bone> Bones = new List<Bone>();
		public Vector3 Position { get; set; }
		public Vector3 _rotation;
		public BoneId[] BoneIndices = new BoneId[(int) BoneId.Count];
		public int _unknown_count;
		public Mesh Skin { get; private set; }//The original, read only
		public Mesh _skin_deform;//Altered
		public Mesh _skin_render;//Updated every frame


		public CFigure() { Clear(); }

		private void RotateHierarchy(BoneId id, Vector3 offset, Matrix4 m) {
			Bone b = Bones[(int) BoneIndices[(int) id]];
			b.Position = Vector3.TransformPosition(b.Position - offset, m) + offset;
			RotatePoints(id, offset, m);
			b.Children.ForEach(child => {
				if (child != BoneId.Root)
					RotateHierarchy(child, offset, m);
			});
		}

		private void RotatePoints(BoneId id, Vector3 offset, Matrix4 m) {
			Bone b = Bones[(int) BoneIndices[(int) id]];
			b.VertexWeights.ForEach(weight => {
				int index = weight._index;
				_skin_render.Vertices[index] = Vector3.TransformPosition(_skin_render.Vertices[index] - offset, m) + offset;
				/*
				from = _skin_render.Vertices[index] - offset;
				to = Vector3.TransformPosition(from, m);
				//movement = movement - _skin_static.Vertices[index]; 
				_skin_render.Vertices[index] = glVectorInterpolate (from, to, b.VertexWeights[i]._weight) + offset;
				*/
			});
		}

		public void Animate(CAnim anim, float delta) {
			if (delta > 1.0f)
				delta -= (int) delta;
			AnimJoint aj = anim.GetFrame(delta);
			for (int i = 0; i < anim.Joints(); i++)
				RotateBone(aj[i].id, aj[i].rotation);
		}

		public void Clear() {
			for (int i = 0; i < (int) BoneId.Count; i++)
				BoneIndices[i] = BoneId.Invalid;
			_unknown_count = 0;
			Skin.Clear();
			_skin_deform.Clear();
			_skin_render.Clear();
			Bones.Clear();
		}

		public bool LoadX(string filename) {
			FileXLoad(filename, this);
			Prepare();
			return true;
		}

		///<summary>We take a string and turn it into a BoneId, using unknowns as needed.</summary>
		public BoneId IdentifyBone(string name) {
			BoneId bid = CAnim.BoneFromstring(name);
			//If CAnim couldn't make sense of the name, or if that id is already in use...
			if (bid == BoneId.Invalid || BoneIndices[(int) bid] != BoneId.Invalid) {
				Console.WriteLine("Couldn't id Bone '%s'.", name);
				bid = BoneId.Unknown0 + _unknown_count;
				_unknown_count++;
			}
			return bid;
		}

		public void SetRotation(Vector3 rot) {
			Bones[(int) BoneIndices[(int) BoneId.Root]].Rotation = rot;
		}

		public void RotateBone(BoneId id, Vector3 angle) {
			if (BoneIndices[(int) id] != BoneId.Invalid)
				Bones[(int) BoneIndices[(int) id]].Rotation = angle;
		}

		public void PushBone(BoneId id, BoneId parent, Vector3 pos) {
			BoneIndices[(int) id] = (BoneId) Bones.Count;
			Bone b = new Bone();
			b.Id = id;
			b.IdParent = parent;
			b.Position = pos;
			b.Origin = pos;
			b.Rotation = Vector3.Zero;
			b.Children.Clear();
			b.Color = glRgbaUnique(id + 1);
			Bones.Add(b);
			Bones[(int) BoneIndices[(int) parent]].Children.Add(id);
		}

		public void PushBone(Figure.Bone bone) {
			PushBone(bone.id, bone.id_parent, bone.pos);
		}

		public void PushWeight(BoneId id, int index, float weight) {
			Bone.BWeight bw;
			bw._index = index;
			bw._weight = weight;
			Bones[(int) BoneIndices[(int) id]].VertexWeights.Add(bw);
		}

		public void Prepare() {
			_skin_deform = Skin;
		}

		public void BoneInflate(BoneId id, float distance, bool do_children) {
			Bone b = Bones[(int) BoneIndices[(int) id]];
			b.VertexWeights.Select(weight => weight._index).ToList()
				.ForEach(index => _skin_deform.Vertices[index] = Skin.Vertices[index] + Skin.Normals[index] * distance);
			if (!do_children)
				return;
			b.Children.ForEach(child => BoneInflate(child, distance, do_children));
		}

		public void Render() {
			GL.Color3(1, 1, 1);
			GL.PushMatrix();
			CgSetOffset(Position);
			GL.Translate(Position.X, Position.Y, Position.Z);
			CgUpdateMatrix();
			_skin_render.Render();
			CgSetOffset(Vector3.Zero);
			GL.PopMatrix();
			CgUpdateMatrix();
		}
		
		public void RenderSkeleton() {
			GL.LineWidth(12);
			GL.PushMatrix();
			GL.Translate(Position.X, Position.Y, Position.Z);
			CgUpdateMatrix();
			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Lighting);
			Bones.ForEach(bone => {
				BoneId parent = BoneIndices[(int) bone.IdParent];
				if (parent != BoneId.Root) {
					GL.Color4(bone.Color);
					GL.Begin(PrimitiveType.Lines);
					Vector3 p = bone.Position;
					GL.Vertex3(bone.Position);
					GL.Vertex3(Bones[(int) parent].Position);
					GL.End();
				}
			});
			GL.LineWidth(1.0f);
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.DepthTest);
			GL.PopMatrix();
			CgUpdateMatrix();
		}

		public void Update() {
			_skin_render = _skin_deform;
			Bones.ForEach(bone => bone.Position = bone.Origin);
			foreach (var bone in Bones) {
				if (bone.Rotation == Vector3.Zero)
					continue;

				var m = Matrix4.CreateRotationX(bone.Rotation.X);
				m = Matrix4.Mult(m, Matrix4.CreateRotationZ(bone.Rotation.Z));
				m = Matrix4.Mult(m, Matrix4.CreateRotationY(bone.Rotation.Y));
				RotatePoints(bone.Id, bone.Rotation, m);
				bone.Children.ForEach(child => {
					//Root is self-parent, but shouldn't rotate self!
					if (child != BoneId.Root)
						RotateHierarchy(child, bone.Position, m);
				});
			}
		}
	}
}
