/*-----------------------------------------------------------------------------
  Figure.cs
  Animated models.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	#region Structs
	struct BWeight {
		public int Index { get; set; }
		public float Weight { get; set; }

		public BWeight(int index, float weight) { Index = index;	weight = Weight; }
	}

	struct PWeight {
		public BoneId Bone { get; set; }
		public float Weight { get; set; }

		public PWeight(BoneId bone, float weight) { Bone = bone;	weight = Weight; }
	}

	struct Bone {
		public BoneId Id { get; set; }
		public BoneId IdParent { get; set; }

		public Vector3 Origin { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }

		public Color4 Color { get; set; }
		public Matrix4 Matrix { get; set; }
		
		public List<int> Children { get; set; }
		public List<BWeight> VertexWeights { get; set; }

		public Bone(BoneId id, BoneId idParent, Vector3 origin, Vector3 position, Vector3 rotation, Color4 color) {
			Id = id;
			IdParent = idParent;
			Origin = origin;
			Position = position;
			Rotation = rotation;
			Color = color;
			Matrix = Matrix4.Identity;

			Children = new List<int>();
			VertexWeights = new List<BWeight>();
		}
	}

	struct BoneListElement {
		public Vector3 Pos { get; set; }
		public BoneId Id { get; set; }
		public BoneId IdParent { get; set; }

		public BoneListElement(Vector3 pos, BoneId id, BoneId idParent) {
			Pos = pos;
			Id = id;
			IdParent = idParent;
		}
		public BoneListElement(float x, float y, float z, BoneId id, BoneId idParent) {
			Pos = new Vector3(x, y, z);
			Id = id;
			IdParent = idParent;
		}
	}
	#endregion

	class Figure {
		#region Member fields, constants and properties
		protected const char NEWLINE = '\n';

		protected Figure fig, fig2;
		protected CAnim anim, animStand;
		protected int frame;
		protected bool stand, moveit = true;
		protected float nn;

		protected List<Bone> mBones;
		protected Vector3 mPosition, mRotation;
		protected BoneId[] mBoneIndices = new BoneId[CAnim.BoneIdCount];
		protected int mUnknownCount;

		protected Mesh
			mSkinStatic,		//The original, "read only"
			mSkinDeform,		//Altered
			mSkinRender;		//Vector3.UnitZdated every frame

		public Vector3 Position { get; set; }
		public Mesh Skin { get { return mSkinStatic; } }

		private static BoneListElement[] boneList = {
			new BoneListElement(  0.0f, 0.0f, 0.0f,     BoneId.Root,      BoneId.Root),
			new BoneListElement(  0.0f, 0.0f, 1.1f,     BoneId.Pelvis,    BoneId.Root),
			new BoneListElement(  0.1f, 0.0f, 1.0f,     BoneId.RHip,      BoneId.Pelvis),
			new BoneListElement(  0.1f, 0.0f, 0.5f,     BoneId.RKnee,     BoneId.RHip),
			new BoneListElement(  0.1f, 0.0f, 0.0f,     BoneId.RAnkle,    BoneId.RKnee),
			new BoneListElement(  0.1f,-0.1f, 0.0f,     BoneId.RToe,      BoneId.RAnkle),

			new BoneListElement( -0.1f, 0.0f, 1.0f,     BoneId.LHip,      BoneId.Pelvis),
			new BoneListElement( -0.1f, 0.0f, 0.5f,     BoneId.LKnee,     BoneId.LHip),
			new BoneListElement( -0.1f, 0.0f, 0.0f,     BoneId.LAnkle,    BoneId.LKnee),
			new BoneListElement( -0.1f,-0.1f, 0.0f,     BoneId.LToe,      BoneId.LAnkle),

			new BoneListElement(  0.0f, 0.0f, 1.55f,    BoneId.Spine1,    BoneId.Pelvis),

			new BoneListElement(  0.1f, 0.0f, 1.5f,     BoneId.RShoulder, BoneId.Spine1),
			new BoneListElement(  0.2f, 0.0f, 1.5f,     BoneId.RArm,      BoneId.RShoulder),
			new BoneListElement(  0.4f, 0.0f, 1.5f,     BoneId.RElbow,    BoneId.RArm),
			new BoneListElement(  0.8f, 0.0f, 1.5f,     BoneId.RWrist,    BoneId.RElbow),

			new BoneListElement( -0.1f, 0.0f, 1.5f,     BoneId.LShoulder, BoneId.Spine1),
			new BoneListElement( -0.2f, 0.0f, 1.5f,     BoneId.LArm,      BoneId.LShoulder),
			new BoneListElement( -0.4f, 0.0f, 1.5f,     BoneId.LElbow,    BoneId.LArm),
			new BoneListElement( -0.8f, 0.0f, 1.5f,     BoneId.LWrist,    BoneId.LElbow),

			new BoneListElement(  0.0f, 0.0f, 1.6f,     BoneId.Neck,      BoneId.Spine1),
			new BoneListElement(  0.0f, 0.0f, 1.65f,    BoneId.Head,      BoneId.Neck),
			new BoneListElement(  0.0f,-0.2f, 1.65f,    BoneId.Face,      BoneId.Head),
			new BoneListElement(  0.0f, 0.0f, 1.8f,     BoneId.Crown,     BoneId.Face)
		};

		#endregion

		#region Public methods
		public Figure() { Clear(); }

		public void Clear() {
			for (int i = 0; i < CAnim.BoneIdCount; i++)
				mBoneIndices[i] = BoneId.Invalid;
			mUnknownCount = 0;
			mSkinStatic.Clear();
			mSkinDeform.Clear();
			mSkinRender.Clear();
			mBones.Clear();
		}

		public void Prepare() { mSkinDeform = mSkinStatic; }

		public void Animate(CAnim anim, float delta) {
			if (delta > 1.0f)
				delta -= (float) ((int) delta);
			List<AnimJoint> aj = anim.GetFrame(delta);
			for (int i = 0; i < anim.JointCount; i++)
				RotateBone(aj[i].id, aj[i].rotation);
		}

		//We take a string and turn it into a BoneId, using unknowns as needed
		public BoneId IdentifyBone(string name) {
			BoneId bid = CAnim.BoneFromString(name);
			//If CAnim couldn't make sense of the name, or if that Id is already in use...
			if (bid == BoneId.Invalid || mBoneIndices[(int) bid] != BoneId.Invalid) {
				//ConsoleLog ("Couldn't Id Bone '%s'.", name);
				bid = BoneId.Unknown0 + mUnknownCount;
				mUnknownCount++;
			}
			return bid;
		}

		public void RotateBone(BoneId id, Vector3 angle) {
			if (mBoneIndices[(int) id] != BoneId.Invalid) {
				Bone bone = mBones[(int) mBoneIndices[(int) id]];
				bone.Rotation = angle;
			}
		}

		public void Update() {
			mSkinRender = mSkinDeform;
			for (int i = 1; i < mBones.Count; i++) {
				Bone b = mBones[i];
				b.Position = mBones[i].Origin;
			}

			foreach (Bone b in mBones) {
				if (!b.Rotation.Equals(Vector3.Zero)) {
					Matrix4 m = Matrix4.CreateRotationX(b.Rotation.X);
					m = Matrix4.Mult(m, Matrix4.CreateRotationZ(b.Rotation.Z));
					m = Matrix4.Mult(m, Matrix4.CreateRotationY(b.Rotation.Y));

					RotatePoints((int) b.Id, b.Position, m);
					for (int c = 0; c < b.Children.Count; c++)
						//Root is self-parent, but shouldn't rotate self!
						if (b.Children[c] != 0)
							RotateHierarchy(b.Children[c], b.Position, m);
				}
			}
		}

		public void PushWeight(int id, int index, float weight) {
			BWeight bw = new BWeight(index, weight);
			mBones[(int) mBoneIndices[id]].VertexWeights.Add(bw);
		}

		public void PushBone(BoneId id, BoneId parent, Vector3 pos) {
			mBoneIndices[(int) id] = (BoneId) mBones.Count;

			Bone b = new Bone(id, parent, pos, pos, Vector3.Zero, glRgbaUnique(id + 1));
			mBones.Add(b);
			mBones[(int) mBoneIndices[(int) parent]].Children.Add((int) id);
		}

		public void BoneInflate(BoneId id, float distance, bool doChildren) {
			Bone b = mBones[(int) mBoneIndices[(int) id]];
			for (int i = 0; i < b.VertexWeights.Count; i++) {
				int index = b.VertexWeights[i].Index;
				mSkinDeform.vertices[index] = mSkinStatic.vertices[index] + mSkinStatic.normals[index] * distance;
			}
			if (!doChildren)
				return;
			for (int c = 0; c < b.Children.Count; c++)
				BoneInflate((BoneId) b.Children[c], distance, doChildren);
		}

		public void RotationSet(Vector3 rot) {
			Bone b = mBones[(int) mBoneIndices[(int) BoneId.Root]];
			b.Rotation = rot;
		}

		public void Render() {
			GL.Color3(1, 1, 1);
			GL.PushMatrix();
			CgSetOffset(mPosition);
			GL.Translate(mPosition);
			CgVector3.UnitZdateMatrix();
			mSkinRender.Render();
			CgSetOffset(Vector3(0, 0, 0));
			GL.PopMatrix();
			CgVector3.UnitZdateMatrix();
		}

		public void RenderSkeleton() {
			GL.LineWidth(12);
			GL.PushMatrix();
			GL.Translate(mPosition);
			CgVector3.UnitZdateMatrix();
			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Lighting);
			for (int i = 0; i < mBones.Count; i++) {
				BoneId parent = mBoneIndices[(int) mBones[i].IdParent];
				if (parent == BoneId.Root)
					continue;
				GL.Color4(mBones[i].Color);
				GL.Begin(BeginMode.Lines);
				Vector3 p = mBones[i].Position;
				GL.Vertex3(mBones[i].Position);
				GL.Vertex3(mBones[(int) parent].Position);
				GL.End();
			}
			GL.LineWidth(1.0f);
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.DepthTest);
			GL.PopMatrix();
			CgVector3.UnitZdateMatrix();
		}

		public bool LoadX(string filename) {
			FileXLoad(filename, this);
			Prepare();
			return true;
		}

		public void FigureInit() {
			foreach (BoneListElement ble in boneList)
				fig.PushBone(ble.Id, ble.IdParent, ble.Pos);

			Mesh skin = fig.Skin;

			AddHull(fig, new Vector3(0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BoneId.RKnee);
			AddHull(fig, new Vector3(-0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BoneId.LKnee);

			AddHull(fig, new Vector3(0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BoneId.RHip);
			AddHull(fig, new Vector3(-0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BoneId.LHip);

			AddHull(fig, new Vector3(0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.RShoulder);
			AddHull(fig, new Vector3(0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.RElbow);

			AddHull(fig, new Vector3(-0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.LShoulder);
			AddHull(fig, new Vector3(-0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BoneId.LElbow);

			fig.Prepare();
			anim.LoadBvh("Anims//run.bvh");
			animStand.LoadBvh("Anims//stand.bvh");

			fig2.LoadX("models//male.X");
			//  fig2.BoneInflate (BoneId.HEAD, 0.01f);
			/*{	FILE*             file;
				file = fopen ("stand.bvh", "w+b");
				if (!file) 
					return;
				for (i = 0; i < fig._bone.size (); i++) {
					fprintf (file, "Joint %s\n", CAnim::NameFromBone (fig._bone[i]._id));
					fprintf (file, "CHANNELS 3\n");
				}
				fprintf (file, "Motion\n");
				fprintf (file, "Frames: 1\n");
				fprintf (file, "Frame Time: 1.0\n");
				for (i = 0; i < fig._bone.size (); i++) 
					fprintf (file, "0.0 0.0 0.0 ");
				fprintf (file, "\n");
				fclose (file);
			}*/
		}

		public void FigureRender() {
			if (moveit)
				nn += 0.03f;

			fig2.RotateBone(BoneId.Spine1, new Vector3(0.0f, 0.0f, (float) Math.Sin(nn * 3) * 25.0f));
			fig2.RotateBone(BoneId.RFingers1, new Vector3(0.0f, (float) -Math.Abs(Math.Sin(nn * 1)) * -80.0f, 0.0f));
			//fig2.RotateBone (BoneId.RELBOW, Vector3 (Math.Abs(Math.Cos(nn * 1)) * 45.0f, 0.0f, 0.0f));
			//fig2.RotateBone (BoneId.LSHOULDER, Vector3 (0.0f, Math.Abs(Math.Sin(nn * 3)) * 80.0f, 0.0f));

			//fig2.RotateBone (BoneId.LELBOW, Vector3 (0.0f, 0.0f, Math.Abs(Math.Cos(nn * 2)) * 90.0f));
			fig2.RotateBone(BoneId.LWrist, new Vector3(0.0f, (float) Math.Abs(Math.Cos(nn * 2)) * 90.0f, 0.0f));
			//fig2.RotateBone (BoneId.RHIP, Vector3 (Math.Sin(nn) * 25.0f, 0.0f,  0.0f));
			//fig2.RotateBone (BoneId.RKNEE, Vector3 (-Math.Abs(Math.Cos(nn * 2) * 45.0f), 0.0f,  0.0f));


			/* for (int i = 0; i < anim._frame[frame].joint.size (); i++) {
				//if (anim._frame[frame].joint[i].id > BoneId.PELVIS)
					fig.RotateBone (anim._frame[frame].joint[i].id, anim._frame[frame].joint[i].rotation);
			} */
			if (stand) {
				//fig.Animate (&animStand, nn);
				//fig2.Animate (&animStand, nn);
			} else {
				//fig.Animate (&anim, nn);
				//fig2.Animate (&anim, nn);
			}
			frame++;
			//frame %= anim._frame.size ();
			//fig.Vector3.UnitZdate ();
			//fig2.Vector3.UnitZdate ();
			if (InputKeyPressed(SDLK_f)) {
				fig.Position = Avatar.Position + new Vector3(0.0f, -2.0f, 0.0f);
				fig2.Position = Avatar.Position + new Vector3(0.0f, 2.0f, 0.0f);
			}
			if (InputKeyPressed(SDLK_g))
				moveit = !moveit;
			if (InputKeyPressed(SDLK_h))
				stand = !stand;

			GL.BindTexture(TextureTarget.Texture2D, 0);
			//glDisable (GL_LIGHTING);
			//fig.Render ();
			//fig2.Render ();
			//glEnable (GL_LIGHTING);
		}
		#endregion

		#region Private methods
		private void RotatePoints(int id, Vector3 offset, Matrix4 m) {
			Bone*       b;
			int    i;
			int    index;

			b = &mBones[mBoneIndices[id]];
			for (i = 0; i < b._vertex_weights.size(); i++) {
				index = b._vertex_weights[i]._index;
				mSkinRender._vertex[index] = Matrix4TransformPoint(m, mSkinRender._vertex[index] - offset) + offset;
				/*
				from = mSkinRender.mVertices[Index] - offset;
				to = Matrix4TransformPoint (m, from);
				//movement = movement - mSkinStatic.mVertices[Index]; 
				mSkinRender.mVertices[Index] = Vector3Interpolate (from, to, b.VertexWeights[i].Weight) + offset;
				*/
			}
		}

		private void RotateHierarchy(int id, Vector3 offset, Matrix4 m) {
			Bone b = mBones[(int) mBoneIndices[id]];
			b.Position = Matrix4TransformPoint(m, b.Position - offset) + offset;
			RotatePoints(id, offset, m);
			for (int i = 0; i < b.Children.Count; i++)
				if (b.Children[i] != 0)
					RotateHierarchy(b.Children[i], offset, m);
		}

		private void AddHull(Figure f, Vector3 p, float d, float h, BoneId id) {
			Mesh m = f.Skin;
			m.PushVertex(new Vector3(p.X, p.Y, p.Z),					Vector3.UnitZ, Vector2.Zero);
			m.PushVertex(new Vector3(p.X, p.Y + d, p.Z),			Vector3.UnitZ, Vector2.Zero);
			m.PushVertex(new Vector3(p.X, p.Y + d, p.Z + h),	Vector3.UnitZ, Vector2.Zero);
			m.PushVertex(new Vector3(p.X, p.Y, p.Z + h),			Vector3.UnitZ, Vector2.Zero);

			int skinBbase = m.VertexCount;
			m.PushQuad(skinBbase + 0, skinBbase + 1, skinBbase + 2, skinBbase + 3);
			m.PushQuad(skinBbase + 3, skinBbase + 2, skinBbase + 1, skinBbase + 0);
			f.PushWeight((int) id, skinBbase, 1.0f);
			f.PushWeight((int) id, skinBbase + 1, 1.0f);
			f.PushWeight((int) id, skinBbase + 2, 1.0f);
			f.PushWeight((int) id, skinBbase + 3, 1.0f);
		}
		#endregion
	}
}