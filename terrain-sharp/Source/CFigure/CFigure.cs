namespace terrain_sharp.Source.CFigure {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using OpenTK;
	using OpenTK.Graphics.OpenGL;

	using CAnim;

	///<summary>Animated models.</summary>
	class CFigure {
		public List<Bone> _bone;
		public Vector3 Position { get; set; }
		public Vector3 _rotation;
		public int[] _bone_index = new int[(int) BoneId.Count];
		public int _unknown_count;
		public GLmesh Skin { get; private set; }//The original, read only
		public GLmesh _skin_deform;//Altered
		public GLmesh _skin_render;//Updated every frame


		public CFigure() { Clear(); }

		private void RotateHierarchy(int id, Vector3 offset, GLmatrix m) {
			Bone b = _bone[_bone_index[id]];
			b.Position = glMatrixTransformPoint(m, b.Position - offset) + offset;
			RotatePoints(id, offset, m);
			b.Children.ForEach(child => {
				if (child != 0)
					RotateHierarchy(child, offset, m);
			});
		}

		private void RotatePoints(int id, Vector3 offset, GLmatrix m) {
			Bone b = _bone[_bone_index[id]];
			b.VertexWeights.ForEach(weight => {
				int index = weight._index;
				_skin_render._vertex[index] = glMatrixTransformPoint(m, _skin_render._vertex[index] - offset) + offset;
				/*
				from = _skin_render._vertex[index] - offset;
				to = glMatrixTransformPoint (m, from);
				//movement = movement - _skin_static._vertex[index]; 
				_skin_render._vertex[index] = glVectorInterpolate (from, to, b.VertexWeights[i]._weight) + offset;
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
				_bone_index[i] = (int) BoneId.Invalid;
			_unknown_count = 0;
			Skin.Clear();
			_skin_deform.Clear();
			_skin_render.Clear();
			_bone.Clear();
		}

		public bool LoadX(string filename) {
			FileXLoad(filename, this);
			Prepare();
			return true;
		}

		///<summary>We take a string and turn it into a BoneId, using unknowns as needed.</summary>
		public BoneId IdentifyBone(string name) {
			BoneId bid = CAnim.BoneFromString(name);
			//If CAnim couldn't make sense of the name, or if that id is already in use...
			if (bid == BoneId.Invalid || _bone_index[(int) bid] != (int) BoneId.Invalid) {
				Console.WriteLine("Couldn't id Bone '%s'.", name);
				bid = BoneId.Unknown0 + _unknown_count;
				_unknown_count++;
			}
			return bid;
		}

		public void SetRotation(Vector3 rot) {
			_bone[_bone_index[(int) BoneId.Root]].Rotation = rot;
		}

		public void RotateBone(BoneId id, Vector3 angle) {
			if (_bone_index[(int) id] != (int) BoneId.Invalid)
				_bone[_bone_index[(int) id]].Rotation = angle;
		}

		public void PushBone(BoneId id, BoneId parent, Vector3 pos) {
			_bone_index[(int) id] = _bone.Count;
			Bone b = new Bone();
			b.Id = id;
			b.IdParent = parent;
			b.Position = pos;
			b.Origin = pos;
			b.Rotation = Vector3.Zero;
			b.Children.Clear();
			b.Color = glRgbaUnique(id + 1);
			_bone.Add(b);
			_bone[_bone_index[(int) parent]].Children.Add((int) id);
		}

		public void PushBone(Figure.Bone bone) {
			PushBone(bone.id, bone.id_parent, bone.pos);
		}

		public void PushWeight(BoneId id, int index, float weight) {
			Bone.BWeight bw;
			bw._index = index;
			bw._weight = weight;
			_bone[_bone_index[(int) id]].VertexWeights.Add(bw);
		}

		public void Prepare() {
			_skin_deform = Skin;
		}

		public void BoneInflate(BoneId id, float distance, bool do_children) {
			Bone b = _bone[_bone_index[(int) id]];
			b.VertexWeights.Select(weight => weight._index).ToList()
				.ForEach(index => _skin_deform._vertex[index] = Skin._vertex[index] + Skin._normal[index] * distance);
			if (!do_children)
				return;
			b.Children.ForEach(child => BoneInflate((BoneId) child, distance, do_children));
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
			for (int i = 0; i < _bone.Count; i++) {
				int parent = _bone_index[(int) _bone[i].IdParent];
				if (parent == 0)
					continue;
				GL.Color4(_bone[i].Color);
				GL.Begin(PrimitiveType.Lines);
				Vector3 p = _bone[i].Position;
				GL.Vertex3(_bone[i].Position);
				GL.Vertex3(_bone[parent].Position);
				GL.End();
			}
			GL.LineWidth(1.0f);
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.DepthTest);
			GL.PopMatrix();
			CgUpdateMatrix();
		}

		public void Update() {
			_skin_render = _skin_deform;
			_bone.ForEach(bone => bone.Position = bone.Origin);
			for (int i = _bone.Count - 1; i >= 0; i--) {
				Bone b = _bone[i];
				if (b.Rotation == Vector3.Zero)
					continue;
				GLmatrix m = GLmatrix.Identity();
				m.Rotate(b.Rotation.X, 1, 0, 0);
				m.Rotate(b.Rotation.Z, 0, 0, 1);
				m.Rotate(b.Rotation.Y, 0, 1, 0);
				RotatePoints(b.Id, b.Rotation, m);
				for (int c = 0; c < b.Children.Count; c++) {
					//Root is self-parent, but shouldn't rotate self!
					if (b.Children[c] != 0)
						RotateHierarchy(b.Children[c], b.Position, m);
				}
			}
		}
	}
}
