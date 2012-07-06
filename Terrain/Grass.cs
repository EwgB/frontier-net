/*-----------------------------------------------------------------------------
  Grass.cs
-------------------------------------------------------------------------------
  This holds the grass object class.  Little bits of grass all over!
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class Grass : GridData {
		enum GrassStage { Begin, Build, Compile, Done }

		#region Constants, member variables and properties
		private const int					GRASS_SIZE = 32;
		private const int					GRASS_TYPES = 8;
		private const int					MAX_TUFTS = 9;

		private static UVBox[]		mBoxGrass = new UVBox[GRASS_TYPES];
		private static UVBox[]		mBoxFlower = new UVBox[GRASS_TYPES];
		private static bool				mPrepDone;
		private static Vector3[,]	mTuftList = new Vector3[MAX_TUFTS, 4];

		private Coord							mOrigin;
		private Coord							mWalk;
		private int								mCurrentDistance;
		private List<Color4>			mColor;
		private List<Vector3>			mVertices;
		private List<Vector3>			mNormals;
		private List<Vector2>			mUVs;
		private List<int>					mIndices;
		private static VBO				mVBO;
		private GrassStage				mStage;
		private BBox							mBBox;

		private bool							Ready  { get { return mStage == GrassStage.Done;} }
		#endregion

		#region Private methods
		private static void DoPrep() {
			for (int i = 0; i < GRASS_TYPES; i++) {
				mBoxGrass[i].Set (i, 2, GRASS_TYPES, 4);
				mBoxFlower[i].Set (i, 3, GRASS_TYPES, 4);
			}

			float angleStep = 360.0f / MAX_TUFTS;
			for (int i = 0; i < MAX_TUFTS; i++) {
				mTuftList[i, 0] = new Vector3(-1, -1, 0);
				mTuftList[i, 1] = new Vector3( 1, -1, 0);
				mTuftList[i, 2] = new Vector3( 1,  1, 0);
				mTuftList[i, 3] = new Vector3(-1,  1, 0);
				Matrix4 m = Matrix4.CreateRotationZ(angleStep * (float)i);
				for (int j = 0; j < 4; j++)
					mTuftList[i, j] = m.TransformPoint(mTuftList[i, j]);
			}
			mPrepDone = true;
		}

		private void VertexPush (Vector3 vert, Vector3 normal, Color4 color, Vector2 uv) {
			mVertices.Add(vert);
			mNormals.Add(normal);
			mColor.Add(color);
			mUVs.Add(uv);
			mBBox.ContainPoint(vert);
		}

		private void QuadPush (int n1, int n2, int n3, int n4) {
			mIndices.Add (n1);
			mIndices.Add (n2);
			mIndices.Add (n3);
			mIndices.Add (n4);
		}

		private bool ZoneCheck () {
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y))
				return false;
			if (!CachePointAvailable (mOrigin.X + GRASS_SIZE, mOrigin.Y))
				return false;
			if (!CachePointAvailable (mOrigin.X + GRASS_SIZE,mOrigin.Y + GRASS_SIZE))
				return false;
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y + GRASS_SIZE))
				return false;
			return true;
		}

		private void Build (long stop) {
			int worldX = mOrigin.X + mWalk.X;
			int worldY = mOrigin.Y + mWalk.Y;
			bool doGrass = (CacheSurface (worldX, worldY) == SurfaceType.Grass);

			if (((mWalk.X % mCurrentDistance) != 0) || ((mWalk.Y % mCurrentDistance) != 0))
				doGrass = false;
			if (doGrass) {
				Region r = FWorld.RegionFromPosition (worldX, worldY);
				int index = worldX + worldY * GRASS_SIZE;
				int this_tuft_index = index % MAX_TUFTS;
				float height = 0.05f + r.Moisture * r.Temperature;
				
				Vector3 root = new Vector3(
					worldX + (FWorld.NoiseFloat(index) -0.5f),
					worldY + (FWorld.NoiseFloat(index) -0.5f),
					0);

				Vector2 size = new Vector2(
					0.4f + FWorld.NoiseFloat(index) * 0.5f,
					FWorld.NoiseFloat(index) * height + (height / 2));

				bool do_flower = r.HasFlowers;
				if (do_flower) //flowers are shorter than grass
					size.Y /= 2;
				size.Y = Math.Max(size.Y, 0.3f);

				Color4 color = CacheSurfaceColor(worldX, worldY);
				color.A = 1.0f;

				// Now we construct our grass panels
				Vector3[] v = new Vector3[8];
				for (int i = 0; i < 4; i++) {
					v[i] = Vector3.Multiply(mTuftList[this_tuft_index, i], new Vector3(size.X, size.X, 0.0f));
					v[i + 4] = Vector3.Multiply(mTuftList[this_tuft_index, i], new Vector3(size.X, size.X, 0.0f));
					v[i + 4].Z += size.Y;
				}
				for (int i = 0; i < 8; i++) {
					v[i] += root;
					v[i].Z += CacheElevation(v[i].X, v[i].Y);
				}

				int patch = r.FlowerShape[index % FLOWERS] % GRASS_TYPES;
				int current = mVertices.Count;
				Vector3 normal = CacheNormal (worldX, worldY);

				VertexPush (v[0], normal, color, mBoxGrass[patch].Corner (1));
				VertexPush (v[1], normal, color, mBoxGrass[patch].Corner (1));
				VertexPush (v[2], normal, color, mBoxGrass[patch].Corner (0));
				VertexPush (v[3], normal, color, mBoxGrass[patch].Corner (0));
				VertexPush (v[4], normal, color, mBoxGrass[patch].Corner (2));
				VertexPush (v[5], normal, color, mBoxGrass[patch].Corner (2));
				VertexPush (v[6], normal, color, mBoxGrass[patch].Corner (3));
				VertexPush (v[7], normal, color, mBoxGrass[patch].Corner (3));
				QuadPush (current, current + 2, current + 6, current + 4);
				QuadPush (current + 1, current + 3, current + 7, current + 5);

				if (do_flower) {
					current = mVertices.Count;
					color = r.ColorFlowers[index % FLOWERS];
					normal = Vector3.UnitZ;
					VertexPush (v[4], normal, color, mBoxFlower[patch].Corner (0));
					VertexPush (v[5], normal, color, mBoxFlower[patch].Corner (1));
					VertexPush (v[6], normal, color, mBoxFlower[patch].Corner (2));
					VertexPush (v[7], normal, color, mBoxFlower[patch].Corner (3));
					QuadPush (current, current + 1, current + 2, current + 3);
				}
			}
			if (mWalk.Walk (GRASS_SIZE)) 
				mStage++;
		}
		#endregion

		#region Public methods
		public Grass() : base() {
			mOrigin.X = 0;
			mOrigin.Y = 0;
			mCurrentDistance = 0;
			Valid = false;
			mBBox.Clear ();
			mGridPosition.Clear();
			mWalk.Clear ();
			mStage = GrassStage.Begin;
			if (!mPrepDone) 
				DoPrep ();
		}

		public override void Set (int x, int y, int density) {
			//density = max (density, 1); //detail 0 and 1 are the same level. (Maximum density.)
			density = 1;
			if (mOrigin.X == x * GRASS_SIZE && mOrigin.Y == y * GRASS_SIZE && density == mCurrentDistance)
				return;
			mGridPosition.X = x;
			mGridPosition.Y = y;
			mCurrentDistance = density;
			mOrigin.x = x * GRASS_SIZE;
			mOrigin.y = y * GRASS_SIZE;
			mStage = GrassStage.Begin;
			mColor.Clear();
			mVertices.Clear();
			mNormals.Clear();
			mUVs.Clear();
			mIndices.Clear();
			mBBox.Clear();
		}

		public override void Update (long stop) {
			while (SdlTick () < stop && !Ready) {
				switch (mStage) {
				case GrassStage.Begin:
					if (!ZoneCheck ())
						return;
					mStage++;
				case GrassStage.Build:
					Build (stop);
					break;
				case GrassStage.Compile:
					if (mVertices.Count != 0)
						mVBO.Create (GL_QUADS, mIndices.Count, mVertices.Count, mIndices, mVertices, mNormals, mColor, mUVs);
					else
						mVBO.Clear ();
					mStage++;
					Valid = true;
					break;
				}
			}
		}

		public override void Render () {
			// We need at least one successful build before we can draw.
			if (!Valid)
				return;
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			GL.Disable(EnableCap.CullFace);
			mVBO.Render ();
			return;

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			GL.Disable(EnableCap.Blend);
			//glEnable (GL_BLEND);
			//glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
			//glDisable (GL_LIGHTING);
			mVBO.Render ();

			GL.Disable(EnableCap.Texture2D);
			//glDisable (GL_FOG);
			GL.Disable(EnableCap.Lighting);
			GL.DepthFunc(DepthFunction.Equal);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.Zero, BlendingFactorDest.SrcColor);
			GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.SrcColor);
			mVBO.Render ();
			GL.DepthFunc(DepthFunction.Lequal);
			if (false) {
				GL.Color3(1,0,1);
				mBBox.Render ();
			}
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Lighting);
		}
		#endregion
	}
}