/*-----------------------------------------------------------------------------
  Forest.cpp
-------------------------------------------------------------------------------
  This class will generate a group of trees for the given area.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class Forest : GridData {
		#region Enums and structs
		enum ForestStage { Begin, Build, Compile, Done }

		struct TreeMesh {
			public int  textureID;
			public Mesh mesh;

			public TreeMesh(int textureID) {
				this.textureID = textureID;
				mesh = new Mesh();
			}
		}

		struct TreeVBO {
			public int texture_id;
			public VBO vbo;
			public BBox bbox;
		}
		#endregion

		#region Constants, member variables and properties
		private const int FOREST_SIZE = 128;

		private LOD mLOD;
		private int mCurrentDistance;
		private ForestStage mStage;
		private bool mSwap, mValid;
		private Coord mOrigin, mWalk;
		private List<TreeMesh> mMeshList;
		private List<TreeVBO> mVBOList;

		public bool Ready { get { return mStage == ForestStage.Done; } }
		public void Invalidate() { mValid = false; }
		#endregion

// public int Sizeof () { return sizeof (Forest); }

		#region Private methods
		private int MeshFromTexture(int texture_id) {

			for (int i = 0; i < mMeshList.Count; i++)
				if (mMeshList[i].textureID == texture_id)
					return i;

			TreeMesh tm = new TreeMesh(texture_id);
			mMeshList.Add(tm);
			return mMeshList.Count - 1;
		}

		private bool ZoneCheck() {
			if (!CachePointAvailable(mOrigin.X, mOrigin.Y))
				return false;
			if (!CachePointAvailable(mOrigin.X + FOREST_SIZE, mOrigin.Y))
				return false;
			if (!CachePointAvailable(mOrigin.X + FOREST_SIZE, mOrigin.Y + FOREST_SIZE))
				return false;
			if (!CachePointAvailable(mOrigin.X, mOrigin.Y + FOREST_SIZE))
				return false;
			return true;
		}

		private void Build(long stop) {
			Vector3      newpos;
			Vector3      newnorm;

			int world_x = mOrigin.X + mWalk.X;
			int world_y = mOrigin.Y + mWalk.Y;
			int tree_id = CacheTree(world_x, world_y);
			
			if (tree_id != 0) {
				int alt = mWalk.X + mWalk.Y * FOREST_SIZE;
				Matrix4 mat = Matrix4.CreateRotationZ(WorldNoisef(alt) * 360.0f);
				Vector3 origin = CachePosition(world_x, world_y);
				Tree tree = WorldTree(tree_id);
				Mesh tm = tree.Mesh(alt, mLOD);
				//tm = tree.Mesh (alt, LOD_LOW);///////////////
				int texture_id = tree.Texture;
				int mesh_index = MeshFromTexture(texture_id);
				int base_index = mMeshList[mesh_index].mesh.VertexCount;

				for (int i = 0; i < tm.VertexCount; i++) {
					newpos = Matrix4TransformPoint(mat, tm.vertices[i]);
					//newpos.Z *= 0.5f + FWorld.NoiseFloat (2 + mWalk.X + mWalk.Y * FOREST_SIZE) * 1.0f;
					newnorm = Matrix4TransformPoint(mat, tm.normals[i]);
					mMeshList[mesh_index].mesh.PushVertex(newpos + origin, newnorm, tm.uvs[i]);
				}

				for (int i = 0; i < tm.TriangleCount; i++) {
					int i1, i2, i3;
					i1 = base_index + tm.indices[i * 3];
					i2 = base_index + tm.indices[i * 3 + 1];
					i3 = base_index + tm.indices[i * 3 + 2];
					mMeshList[mesh_index].mesh.PushTriangle(i1, i2, i3);
				}
			}
			if (mWalk.Walk(FOREST_SIZE))
				mStage++;
		}

		private void Compile() {
			// First, purge the existing VBO
			for (int i = 0; i < mVBOList.Count; i++)
				mVBOList[i].vbo.Clear();
			mVBOList.Clear();

			// Now compile the new list
			mVBOList.Capacity = mMeshList.Count;
			for (int i = 0; i < mMeshList.Count; i++) {
				mVBOList[i].vbo.Clear();
				mVBOList[i].bbox = mMeshList[i].mesh.bbox;
				mVBOList[i].texture_id = mMeshList[i].textureID;
				if (mMeshList[i].mesh.vertices.Count > 0)
					mVBOList[i].vbo.Create(GL_TRIANGLES,
					mMeshList[i].mesh.TriangleCount * 3,
					mMeshList[i].mesh.VertexCount,
					mMeshList[i].mesh.indices[0],
					mMeshList[i].mesh.vertices[0],
					mMeshList[i].mesh.normals[0], null,
					mMeshList[i].mesh.uvs[0]);
			}

			// Now purge the mesh list, so it can begin building again in the background when the time comes.
			for (int i = 0; i < mMeshList.Count; i++)
				mMeshList[i].mesh.Clear();
			mMeshList.Clear();
			mValid = true;
			mStage++;
		}
		#endregion

		#region Public methods
		public Forest() : base() {
			mStage = ForestStage.Begin;
			mCurrentDistance = 0;
			mValid = false;
			mWalk.Clear();
			mMeshList.Clear();
			mVBOList.Clear();
		}

		public void Set(int x, int y, int distance) {
			if (mGridPosition.X == x && mGridPosition.Y == y && mCurrentDistance == distance)
				return;
			if (mStage == ForestStage.Build)
				return;

			mCurrentDistance = distance;
			mLOD = LOD.High;
			if (distance > 3)
				mLOD = LOD.Low;
			else if (distance > 1)
				mLOD = LOD.Med;

			mGridPosition.X = x;
			mGridPosition.Y = y;
			mOrigin.X = x * FOREST_SIZE;
			mOrigin.Y = y * FOREST_SIZE;
			mStage = ForestStage.Begin;
			for (int i = 0; i < mMeshList.Count; i++)
				mMeshList[i].mesh.Clear();
			mMeshList.Clear();
		}

		public void Update(long stop) {
			while (SdlTick() < stop && !Ready()) {
				switch (mStage) {
					case ForestStage.Begin:
						if (!ZoneCheck())
							return;
						mStage++;
					//Fall through
					case ForestStage.Build:
						Build(stop);
						break;
					case ForestStage.Compile:
						Compile();
						break;
				}
			}
		}

		public void Render() {
			// We need at least one successful build before we can draw.
			if (!mValid)
				return;
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			for (int i = 0; i < mVBOList.Count; i++) {
				GL.BindTexture(TextureTarget.Texture2D, mVBOList[i].texture_id);
				mVBOList[i].vbo.Render();
				//glBindTexture (GL_TEXTURE_2D, 0);
				//mVBOList[i].mBBox.Render ();
			}
		}
		#endregion
	}
}
