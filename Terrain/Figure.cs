/*-----------------------------------------------------------------------------
  Figure.cs
  Animated models.
-----------------------------------------------------------------------------*/

using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

namespace Frontier {
	#region Structs
	struct BWeight {
		public int Index { get; set; }
		public float Weight { get; set; }
	}

	struct PWeight {
		public BoneId Bone { get; set; }
		public float Weight { get; set; }
	}

	struct Bone {
		public BoneId	Id { get; set; }
		public BoneId	IdParent { get; set; }

		public Vector3 Origin { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }

		public Color4 Color { get; set; }
		public List<int> Children { get; set; }
		public List<BWeight> VertexWeights { get; set; }
		public Matrix4 Matrix { get; set; }
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
		private const char NEWLINE = '\n';

		private List<Bone> mBones;
		private Vector3 mPosition, mRotation;
		private int[] mBoneIndices = new int[BONE_COUNT];
		private int mUnknownCount;

		private Mesh
			mSkinStatic,		//The original, "read only"
			mSkinDeform,		//Altered
			mSkinRender;		//Updated every frame

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
			for (int i = 0; i < BONE_COUNT; i++)
				mBoneIndices[i] = BONE_INVALID;
			mUnknownCount = 0;
			mSkinStatic.Clear();
			mSkinDeform.Clear();
			mSkinRender.Clear();
			mBones.Clear();
		}

		public void Animate(CAnim anim, float delta) {
			List<AnimJoint> aj;

			if (delta > 1.0f)
				delta -= (float) ((int) delta);
			aj = anim.GetFrame(delta);
			for (int i = 0; i < anim.JointCount; i++)
				RotateBone(aj[i].id, aj[i].rotation);
		}

		//We take a string and turn it into a BoneId, using unknowns as needed
		public BoneId IdentifyBone (string name) {
			BoneId    bid;

			bid = CAnim.BoneFromString (name);
			//If CAnim couldn't make sense of the name, or if that Id is already in use...
			if (bid == BONE_INVALID || mBoneIndices[bid] != BONE_INVALID) {
				//ConsoleLog ("Couldn't Id Bone '%s'.", name);
				bid = (BoneId)(BONE_UNKNOWN0 + mUnknownCount);
				mUnknownCount++;
			}
			return bid;
		}

		public void RotateBone(BoneId id, Vector3 angle) {
			if (mBoneIndices[id] != BONE_INVALID)
				mBones[mBoneIndices[id]].mRotation = angle;
		}

		public void Update() {
			int    i;
			int    c;
			Matrix4    m;
			Bone*       b;
			List<Bone>.reverse_iterator rit;

			mSkinRender = mSkinDeform;
			for (i = 1; i < mBones.size (); i++) 
				mBones[i].mPosition = mBones[i].Origin;
			for (rit = mBones.rbegin(); rit < mBones.rend(); ++rit) {
				b = &(*rit);
				if (b.mRotation == Vector3 (0.0f, 0.0f, 0.0f))
					continue;
				m.Identity ();
				m.Rotate (b.mRotation.x, 1.0f, 0.0f, 0.0f);
				m.Rotate (b.mRotation.z, 0.0f, 0.0f, 1.0f);
				m.Rotate (b.mRotation.y, 0.0f, 1.0f, 0.0f);
				RotatePoints (b._id, b.mPosition, m);
				for (c = 0; c < b._children.size (); c++) {
					//Root is self-parent, but shouldn't rotate self!
					if (b._children[c]) 
						RotateHierarchy (b._children[c], b.mPosition, m);
				}
			}
		}

		public void PushWeight(int id, int index, float weight) {
			BWeight   bw;

			bw.Index = index;
			bw.Weight = weight;
			mBones[mBoneIndices[id]].VertexWeights.Add(bw);
		}

		public void PushBone(BoneId id, int parent, Vector3 pos) {
			Bone    b;

			mBoneIndices[(int) id] = mBones.Count;
			b.Id = (BoneId) id;
			b.IdParent = (BoneId) parent;
			b.Position = pos;
			b.Origin = pos;
			b.Rotation = Vector3.Zero;
			b.Children.Clear();
			b.Color = glRgbaUnique(id + 1);
			mBones.Add(b);
			mBones[mBoneIndices[parent]].Children.Add((int) id);
		}

		public void BoneInflate(BoneId id, float distance, bool do_children) {
			Bone b = mBones[mBoneIndices[(int) id]];
			for (int i = 0; i < b.VertexWeights.Count; i++) {
				int index = b.VertexWeights[i].Index;
				mSkinDeform.vertices[index] = mSkinStatic.vertices[index] + mSkinStatic.normals[index] * distance;
			}
			if (!do_children)
				return;
			for (int c = 0; c < b.Children.Count; c++)
				BoneInflate((BoneId) b.Children[c], distance, do_children);
		}

		public void RotationSet(Vector3 rot) { mBones[mBoneIndices[BONE_ROOT]].mRotation = rot; }

		public void Render() {
			glColor3f(1, 1, 1);
			glPushMatrix();
			CgSetOffset(mPosition);
			glTranslatef(mPosition.x, mPosition.y, mPosition.z);
			CgUpdateMatrix();
			mSkinRender.Render();
			CgSetOffset(Vector3(0, 0, 0));
			glPopMatrix();
			CgUpdateMatrix();
		}

		public void RenderSkeleton() {
			int    i;
			int    parent;

			glLineWidth(12.0f);
			glPushMatrix();
			glTranslatef(mPosition.x, mPosition.y, mPosition.z);
			CgUpdateMatrix();
			glDisable(GL_DEPTH_TEST);
			glDisable(GL_TEXTURE_2D);
			glDisable(GL_LIGHTING);
			for (i = 0; i < mBones.size(); i++) {
				parent = mBoneIndices[mBones[i].IdParent];
				if (!parent)
					continue;
				glColor3fv(&mBones[i].Color.red);
				glBegin(GL_LINES);
				Vector3 p = mBones[i].mPosition;
				glVertex3fv(&mBones[i].mPosition.x);
				glVertex3fv(&mBones[parent].mPosition.x);
				glEnd();
			}
			glLineWidth(1.0f);
			glEnable(GL_LIGHTING);
			glEnable(GL_TEXTURE_2D);
			glEnable(GL_DEPTH_TEST);
			glPopMatrix();
			CgUpdateMatrix();
		}

		public void Prepare() { mSkinDeform = mSkinStatic; }

		public bool LoadX(string filename) {
			FileXLoad(filename, this);
			Prepare();
			return true;
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
				from = mSkinRender._vertex[Index] - offset;
				to = Matrix4TransformPoint (m, from);
				//movement = movement - mSkinStatic._vertex[Index]; 
				mSkinRender._vertex[Index] = Vector3Interpolate (from, to, b.VertexWeights[i].Weight) + offset;
				*/
			}
		}

		private void RotateHierarchy(int id, Vector3 offset, Matrix4 m) {
			Bone*       b;
			int    i;

			b = &mBones[mBoneIndices[id]];
			b.mPosition = Matrix4TransformPoint(m, b.mPosition - offset) + offset;
			RotatePoints(id, offset, m);
			for (i = 0; i < b._children.size(); i++) {
				if (b._children[i])
					RotateHierarchy(b._children[i], offset, m);
			}
		}

		private static void clean_chars(char* target, char* chars) {
			char* c;

			for (int i = 0; i < strlen(chars); i++) {
				while (c = strchr(target, chars[i]))
					*c = ' ';
			}
		}
		#endregion
	}
}