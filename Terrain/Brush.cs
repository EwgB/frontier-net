/*-----------------------------------------------------------------------------
  Brush.cs
-------------------------------------------------------------------------------
  This holds the brush object class.  Bushes and the like. 
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	enum BrushStage { Begin, Build, Compile, Done }

	class Brush : GridData {
		#region Constants, member variables and properties
		private const int						BRUSH_TYPES = 4;
		private const int						MAX_TUFTS = 9;
		private const int						BRUSH_SIZE = 32;

		private static UVBox[]			mBox = new UVBox[BRUSH_TYPES];
		private static UVBox[]			mBoxFlower = new UVBox[BRUSH_TYPES];
		private static bool					mPrepDone;
		private static Vector3[,]		mTuftList = new Vector3[MAX_TUFTS, 4];
		private static VBO					mVBO;

		private BrushStage					mStage;
		private int									mCurrentDistance;
		private Coord								mOrigin;
		private Coord								mGridPosition;
		private Coord								mWalk;
		private Mesh								mMesh;
		private BBox								mBBox;

		// List<GLrgba>  colors;
		// List<Vector3> vertices;
		// List<Vector3> normals;
		// List<Vector2> uvs;
		// List<UINT>      index;

		public bool Valid { get; set; }
		public bool Ready { get { return mStage == BrushStage.Done; } }
		#endregion

		#region Private methods
		private void VertexPush(Vector3 vert, Vector3 normal, Color4 color, Vector2 uv);
		private void QuadPush (int n1, int n2, int n3, int n4);

		private static void DoPrep() {
			for (int i = 0; i < BRUSH_TYPES; i++) {
				mBox[i].Set (i, 0, BRUSH_TYPES, 2);
				mBox[i].lr.y *= 0.99f;
			}
			float angleStep = 360.0f / MAX_TUFTS;
			for (int i = 0; i < MAX_TUFTS; i++) {
				mTuftList[i, 0] = new Vector3(-1, -1, 0);
				mTuftList[i, 1] = new Vector3( 1, -1, 0);
				mTuftList[i, 2] = new Vector3( 1,  1, 0);
				mTuftList[i, 3] = new Vector3(-1,  1, 0);
				Matrix4 m = Matrix4.CreateRotationZ(angleStep * i);
				for (int j = 0; j < 4; j++)
					mTuftList[i, j] = m.TransformPoint(mTuftList[i, j]);
			}
			mPrepDone = true;
		}

		private bool ZoneCheck() {
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y))
				return false;
			if (!CachePointAvailable (mOrigin.X + BRUSH_SIZE, mOrigin.Y))
				return false;
			if (!CachePointAvailable (mOrigin.X + BRUSH_SIZE,mOrigin.Y + BRUSH_SIZE))
				return false;
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y + BRUSH_SIZE))
				return false;
			return true;
		}

		private void Build(long stop) {
			int world_x = mOrigin.X + mWalk.X;
			int world_y = mOrigin.Y + mWalk.Y;

			if (CacheSurface(world_x, world_y) == SURFACE_GRASS_EDGE) {
				Region r = FWorld.RegionFromPosition(world_x, world_y);
				int index = world_x + world_y * BRUSH_SIZE;
				int thisTuftIndex	= index % MAX_TUFTS;
				Vector3 root = new Vector3(world_x, world_y, 0);
				float height = 0.25f + (r.moisture * r.temperature) * 2.0f;

				Vector2 size = new Vector2(
					1.0f + FWorld.NoiseFloat(index) * 1.0f,
					1.0f + FWorld.NoiseFloat(index) * height);
				size.Y = Math.Max (size.X, size.Y);		// Don't let bushes get wider than they are tall
				
				Color4 color = CacheSurfaceColor (world_x, world_y);
				color *= 0.75f;
				color.A = 1;

				//Now we construct our grass panels
				Vector3[]	v = new Vector3[8];
				for (int i = 0; i < 4; i++) {
					v[i] = Vector3.Multiply(mTuftList[thisTuftIndex, i], new Vector3(size.X, size.X, 0));
					v[i + 4] = Vector3.Multiply(mTuftList[thisTuftIndex, i], new Vector3(size.X, size.X, 0));
					v[i + 4].Z += size.Y;
				}

				for (int i = 0; i < 8; i++) {
					v[i] += root;
					v[i].Z += CacheElevation (v[i].X, v[i].Y);
				}

				int patch = r.flower_shape[index % FLOWERS] % BRUSH_TYPES;
				int current = mMesh.VertexCount;
				Vector3 normal = CacheNormal(world_x, world_y);

				mMesh.PushVertex (v[0], normal, color, mBox[patch].Corner (1)); 
				mMesh.PushVertex (v[1], normal, color, mBox[patch].Corner (1)); 
				mMesh.PushVertex (v[2], normal, color, mBox[patch].Corner (0)); 
				mMesh.PushVertex (v[3], normal, color, mBox[patch].Corner (0)); 
				mMesh.PushVertex (v[4], normal, color, mBox[patch].Corner (2)); 
				mMesh.PushVertex (v[5], normal, color, mBox[patch].Corner (2)); 
				mMesh.PushVertex (v[6], normal, color, mBox[patch].Corner (3)); 
				mMesh.PushVertex (v[7], normal, color, mBox[patch].Corner (3)); 
				mMesh.PushQuad (current, current + 2, current + 6, current + 4);
				mMesh.PushQuad (current + 1, current + 3, current + 7, current + 5);
			}
			if (mWalk.Walk (BRUSH_SIZE)) 
				mStage++;
		}
		#endregion

		#region Public methods
		public Brush() : base() {
			mOrigin = new Coord(0, 0);
			mGridPosition = new Coord();
			mWalk = new Coord();
			mCurrentDistance = 0;
			Valid = false;
			mBBox.Clear();
			mGridPosition.Clear();
			mWalk.Clear();
			mMesh.Clear();
			mStage = BrushStage.Begin;
  
			if (!mPrepDone)
				DoPrep ();
		}

		public void Set(int x, int y, int density) {
			if (mOrigin.X == x * BRUSH_SIZE && mOrigin.Y == y * BRUSH_SIZE)
				return;

			mGridPosition.X = x;
			mGridPosition.Y = y;

			mCurrentDistance = (int) Math.Abs(density);
			mOrigin.X = x * BRUSH_SIZE;
			mOrigin.Y = y * BRUSH_SIZE;
			mStage = BrushStage.Begin;
			mMesh.Clear();
			mBBox.Clear();
		}

		public void Update(long stop) {
			while (SdlTick() < stop && !Ready ()) {
				switch (mStage) {
					case BrushStage.Begin:
						if (!ZoneCheck())
							return;
						mStage++;
					case BrushStage.Build:
						Build(stop);
						break;
					case BrushStage.Compile:
						if (mMesh.VertexCount != 0)
							mVBO.Create(mMesh);
						else
							mVBO.Clear();
						mStage++;
						Valid = true;
						break;
				}
			}
		}

		public void Render() {
			//We need at least one successful build before we can draw.
			if (!Valid)
				return;
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			GL.Disable(EnableCap.CullFace);
			mVBO.Render();
		}
		#endregion
	}
}