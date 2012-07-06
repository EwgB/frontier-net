/*-----------------------------------------------------------------------------
  Terrain.cs
-------------------------------------------------------------------------------
  This holds the terrain object class.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class Terrain : GridData {
		#region Enums and structs
		enum Neighbor { North, East, South, West }

		enum Stage {
			Begin, Clear, DoCompileGrid, Heightmap, Quadtree, Stitch, BufferLoad, Compile, VBO, Texture, TextureFinal, Done }

		struct LayerAttributes {
			public int					texture_frame;
			public float        luminance;
			public float        opacity;
			public float        size;
			public SurfaceType	surface;
			public SurfaceColor color;

			public LayerAttributes(int texture_frame, float luminance, float opacity, float size, SurfaceType surface, SurfaceColor color) {
				this.texture_frame = texture_frame;
				this.luminance = luminance;
				this.opacity = opacity;
				this.size = size;
				this.surface = surface;
				this.color = color;
			}
		}
		#endregion

		#region Member fields, constants and properties
		private const int
			TERRAIN_SIZE = 128,
			TERRAIN_HALF = (TERRAIN_SIZE / 2),
			TERRAIN_EDGE = (TERRAIN_SIZE + 1),
			COMPILE_GRID = 4,
			COMPILE_SIZE = (TERRAIN_SIZE / COMPILE_GRID),
			NEIGHBOR_COUNT = 4;
	
		//Lower values make the terrain more precise at the expense of more polygons
		private const float
			TOLERANCE = 0.08f,
			LAYERS = (18 /*sizeof (layers)*/ / 6 /*sizeof (LayerAttributes)*/);

		private Coord mWalk;

		private Vector3[,] mPos = new Vector3[TERRAIN_EDGE, TERRAIN_EDGE];
	
		private Stage mStage;

		private int[] mNeighbors = new int[NEIGHBOR_COUNT];
		private int[,] mIndexMap = new int[TERRAIN_EDGE, TERRAIN_EDGE];
		private int mFrontTexture, mBackTexture, mPatchSize, mPatchSteps, mIndexBufferSize, mCurrentDistance;
		private static int[] mBoundary = new int[TERRAIN_SIZE];
		private long mRebuild;
		
		private bool[] mSurfaceTypeUsed = new bool[SurfaceType.TYPES];
		private bool[,] mPoint = new bool[TERRAIN_EDGE, TERRAIN_EDGE];
		private static bool mBoundReady;

		private LOD mLOD;
	
		private Color4 mColor;
	
		private static VBO mVBO;

		private List<int> mIndexBuffer;
		private List<Vector3> mVertexList, mNormalList, mVert;
		private List<Vector2> mUVList;

		private Coord mOrigin;
		public Coord Origin { get; private set; }

		public int PolygonsCount { get { return mIndexBuffer.Count / 3; } }
		public int PointsCount { get {return mIndexBuffer.Count; } }
		public override bool IsReady { get { return mStage == Stage.Done; } }
		public int TerrainPatch { get { return (TERRAIN_SIZE / mPatchSteps); } }

		public int TextureSize { get; private set; }

		public int mTextureDesiredSize;
		public int TextureDesiredSize {
			get { return mTextureDesiredSize; }
			private set {
				//We can't resize in the middle of rendering the texture
				if (mStage == Stage.Texture || mStage == Stage.TextureFinal)
					return;
				if (value != TextureSize) {
					mTextureDesiredSize = value;
					if (mStage == Stage.Done)
						mStage = Stage.Texture;
				}
			}
		}

		private readonly LayerAttributes[]	layers = {
			new LayerAttributes(7, 0.7f, 0.3f, 1.3f,  SurfaceType.Sand,      SurfaceColor.Sand),
  		new LayerAttributes(7, 0.8f, 0.3f, 1.2f,  SurfaceType.Sand,      SurfaceColor.Sand),
  		new LayerAttributes(7, 1.0f, 1.0f, 1.1f,  SurfaceType.Sand,      SurfaceColor.Sand),

  		new LayerAttributes(4, 0.6f, 1.0f, 1.5f,  SurfaceType.SandDark,  SurfaceColor.Sand),
  		new LayerAttributes(4, 1.0f, 1.0f, 1.4f,  SurfaceType.Dirt,      SurfaceColor.Dirt),
  		new LayerAttributes(4, 0.6f, 1.0f, 1.6f,  SurfaceType.DirtDark,  SurfaceColor.Dirt),

  		new LayerAttributes(3, 1.0f, 1.0f, 1.6f,  SurfaceType.Forest,    SurfaceColor.Dirt),

  		new LayerAttributes(6, 0.0f, 0.3f, 2.3f,  SurfaceType.GrassEdge, SurfaceColor.Grass),
  		new LayerAttributes(6, 0.0f, 0.5f, 2.2f,  SurfaceType.GrassEdge, SurfaceColor.Grass),
  		new LayerAttributes(6, 0.0f, 0.5f, 2.1f,  SurfaceType.GrassEdge, SurfaceColor.Grass),
  		new LayerAttributes(5, 0.0f, 0.3f, 1.7f,  SurfaceType.Grass,     SurfaceColor.Grass),
  		new LayerAttributes(5, 0.0f, 0.5f, 1.5f,  SurfaceType.Grass,     SurfaceColor.Grass),
  		new LayerAttributes(5, 1.0f, 1.0f, 1.4f,  SurfaceType.Grass,     SurfaceColor.Grass),
  		new LayerAttributes(6, 1.0f, 1.0f, 2.0f,  SurfaceType.GrassEdge, SurfaceColor.Grass),
  
  		new LayerAttributes(2, 0.0f, 0.3f, 1.9f,  SurfaceType.Snow,      SurfaceColor.Snow),
  		new LayerAttributes(2, 0.6f, 0.8f, 1.6f,  SurfaceType.Snow,      SurfaceColor.Snow),
  		new LayerAttributes(2, 0.8f, 0.8f, 1.55f, SurfaceType.Snow,      SurfaceColor.Snow),
  		new LayerAttributes(2, 1.0f, 1.0f, 1.5f,  SurfaceType.Snow,      SurfaceColor.Snow)
		};
		#endregion

		#region Private methods
		// This finds the largest power-of-two denominator for the given number.
		// This is used to determine what level of the quadtree a grid position occupies.  
		private static int Boundary(int val) {
			if (!mBoundReady) {
				for (int n = 0; n < TERRAIN_SIZE; n++) {
					mBoundary[n] = -1;
					if (n == 0)
						mBoundary[n] = TERRAIN_SIZE;
					else {
						for (int level = TERRAIN_SIZE; level > 1; level /= 2) {
							if (n % level == 0) {
								mBoundary[n] =  level;
								break;
							}
						}
						if (mBoundary[n] == -1)
							mBoundary[n] = 1;
					}
				}
				mBoundReady = true;
			}
			return mBoundary[val];
		}

		private void DoPatch(int patch_z, int patch_y) {
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Fog);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);	
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);	
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);	

			Coord start, end;
			if (mPatchSteps > 1) {
				int texture_step = TERRAIN_SIZE / mPatchSteps;
				start = new Coord(mWalk.X * texture_step - 3, mWalk.Y * texture_step - 3);
				end = new Coord(start.X + texture_step + 5, start.Y + texture_step + 6);
			} else {
				start = new Coord(-2, -2);
				end = new Coord(TERRAIN_EDGE + 2, TERRAIN_EDGE + 2);
			}

			GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName("terrain_rock.png"));
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);	

			for (int y = start.Y; y < end.Y - 1; y++) {
				GL.Begin(BeginMode.QuadStrip);
				for (int x = start.X; x < end.X; x++) {
					int world_x = mOrigin.X + x;
					int world_y = mOrigin.Y + y;

					GL.TexCoord2(x / 8, y / 8);
					Color4 SurfaceTypeColor = CacheSurfaceColor (world_x, world_y);
					GL.Color4(SurfaceTypeColor);
					GL.Vertex2(x, y);

					GL.TexCoord2(x / 8, (y + 1) / 8);
					SurfaceTypeColor = CacheSurfaceColor(world_x, world_y + 1);
					GL.Color4(SurfaceTypeColor);
					GL.Vertex2(x, y + 1);
				}
				GL.End ();
			}

			for (int stage = 0; stage < LAYERS; stage++) {
				// Special layer to give the Sand & rock some more depth
				if (stage == 3) {
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
					GL.Color4(1, 1, 1, 0.5f);
					GL.Color3(1, 1, 1);

					GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.SrcColor);
					GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName("terrain_shading.png"));
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);	

					GL.Begin(BeginMode.Quads);
					GL.TexCoord2(0, 0);		GL.Vertex2(0, 0);
					GL.TexCoord2(0, 2);		GL.Vertex2(TERRAIN_SIZE, 0);
					GL.TexCoord2(2, 2);		GL.Vertex2(TERRAIN_SIZE, TERRAIN_SIZE);
					GL.TexCoord2(2, 0);		GL.Vertex2(0, TERRAIN_SIZE);
					GL.End();
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				}

				if (!mSurfaceTypeUsed[(int) layers[stage].surface])
					continue;
				GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName("terrain.png"));
				UVBox uvb;
				uvb.Set(0, layers[stage].texture_frame, 1, 8);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);	

				for (int y = start.Y; y < end.Y - 1; y++) {
					for (int x = start.X; x < end.X; x++) {
						int world_x = mOrigin.X + x;
						int world_y = mOrigin.Y + y;
						SurfaceType surface = CacheSurface (world_x, world_y);
						if (surface != layers[stage].surface)
							continue;

						Vector2 pos = new Vector2(x, y);
						float tile = 0.66f * layers[stage].size; 
						GL.PushMatrix();
						GL.Translate(pos.X - 0.5f, pos.Y - 0.5f, 0);
						int angle = (world_x + world_y * 2) * 25;
						angle %= 360;

						GL.Rotate(angle, 0.0f, 0.0f, 1.0f);
						GL.Translate(-pos.X, -pos.Y, 0);

						Color4 SurfaceTypeColor;
						if (layers[stage].color == SurfaceColor.Black)
							SurfaceTypeColor = Color4.Black;
						else
							SurfaceTypeColor = CacheSurfaceColor (world_x, world_y);

						Color4 col = SurfaceTypeColor * layers[stage].luminance;
						col.A = layers[stage].opacity;
						GL.Color4(col);
						GL.Begin(BeginMode.Quads);

						Vector2 uv;
						uv = uvb.Corner (0);	GL.TexCoord2(uv);		GL.Vertex2(pos.X - tile, pos.Y - tile);
						uv = uvb.Corner (1);	GL.TexCoord2(uv);		GL.Vertex2(pos.X + tile, pos.Y - tile);
						uv = uvb.Corner (2);	GL.TexCoord2(uv);		GL.Vertex2(pos.X + tile, pos.Y + tile);
						uv = uvb.Corner (3);	GL.TexCoord2(uv);		GL.Vertex2(pos.X - tile, pos.Y + tile);
						GL.End();
						GL.PopMatrix();
					}
				}
			}
		}

		private void DoTexture() {
			if (mBackTexture != 0) {
				GL.GenTextures (1, out mBackTexture);
				GL.BindTexture(TextureTarget.Texture2D, mBackTexture);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);	

				// We draw the terrain texture in squares called patches, but how big should they be?
				// We can't draw more than will fit in the viewport
				mPatchSize = Math.Min(RenderMaxDimension(), TextureDesiredSize);
				mPatchSteps = TextureDesiredSize / mPatchSize;
				
				// We also don't want to do much at once. Walking a 128x128 grid in a singe frame creates stuttering. 
				while (TERRAIN_SIZE / mPatchSteps > 32) {
					mPatchSize /= 2;
					mPatchSteps = TextureDesiredSize / mPatchSize;
				}

				//mPatchSteps = max (mPatchSteps, 1);//Avoid div by zero. Trust me, it's bad.
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, TextureDesiredSize, TextureDesiredSize,
					0, PixelFormat.Rgb, PixelType.Byte, null);
			}

			RenderCanvasBegin (mWalk.X * TerrainPatch, mWalk.X * TerrainPatch + TerrainPatch, mWalk.Y * TerrainPatch, mWalk.Y * TerrainPatch + TerrainPatch, mPatchSize);
			DoPatch (mWalk.X, mWalk.Y);
			GL.BindTexture(TextureTarget.Texture2D, mBackTexture);
			GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, mWalk.X * mPatchSize, mWalk.Y * mPatchSize, 0, 0, mPatchSize, mPatchSize);
			RenderCanvasEnd();
			if (mWalk.Walk (mPatchSteps))
				mStage++;
		}

		// In order to avoid having gaps between adjacent terrains, we have to "stitch" them together.
		// We analyze the points used along the shared Edge and activate any points used by our neighbor.
		private void DoStitch() {
			Terrain w = SceneTerrainGet(mGridPosition.X - 1, mGridPosition.Y);
			Terrain e = SceneTerrainGet(mGridPosition.X + 1, mGridPosition.Y);
			Terrain s = SceneTerrainGet(mGridPosition.X, mGridPosition.Y + 1);
			Terrain n = SceneTerrainGet(mGridPosition.X, mGridPosition.Y - 1);

			for (int i = 0; i < TERRAIN_EDGE; i++) {
				int b = Boundary (i);
				if ((w != null) && w.GetPoint(TERRAIN_SIZE, i)) {
					PointActivate(0, i);
					PointActivate(b, i);
				}
				if ((e != null) && e.GetPoint(0, i)) {
					PointActivate(TERRAIN_SIZE - b, i);
					PointActivate(TERRAIN_SIZE, i);
				}
				if ((s != null) && s.GetPoint(i, 0)) {
					PointActivate(i, TERRAIN_SIZE);
					PointActivate(i, TERRAIN_SIZE - b);
				}
				if ((n != null) && n.GetPoint (i, TERRAIN_SIZE)) {
					PointActivate(i, b);
					PointActivate(i, 0);
				}
			}

			// Now save a snapshot of how many points our neighbors are using. 
			// If these change, they have added detail and we'll need to re-stitch.
			for (int i = 0; i < NEIGHBOR_COUNT; i++)
				mNeighbors[i] = 0;
			if (w != null) mNeighbors[(int) Neighbor.West] = w.PointsCount;
			if (e != null) mNeighbors[(int) Neighbor.East] = e.PointsCount;
			if (n != null) mNeighbors[(int) Neighbor.North] = n.PointsCount;
			if (s != null) mNeighbors[(int) Neighbor.South] = s.PointsCount;
		}

		// Look at our neighbors and see if they have added detail since our last rebuild.
		private bool DoCheckNeighbors() {
			Terrain w = SceneTerrainGet (mGridPosition.X - 1, mGridPosition.Y);
			Terrain e = SceneTerrainGet (mGridPosition.X + 1, mGridPosition.Y);
			Terrain s = SceneTerrainGet (mGridPosition.X, mGridPosition.Y + 1);
			Terrain n = SceneTerrainGet (mGridPosition.X, mGridPosition.Y - 1);

			if ((w != null) && w.PointsCount != mNeighbors[(int) Neighbor.West])		return true;
			if ((s != null) && s.PointsCount != mNeighbors[(int) Neighbor.South])	return true;
			if ((e != null) && e.PointsCount != mNeighbors[(int) Neighbor.East])		return true;
			if ((n != null) && n.PointsCount != mNeighbors[(int) Neighbor.North])	return true;
			return false;
		}

		private void DoHeightmap() {
			Coord world = new Coord(mOrigin.X + mWalk.X, mOrigin.Y + mWalk.Y);
			mSurfaceTypeUsed[CacheSurface (world.X, world.Y)] = true;
			Vector3 pos = new Vector3(world.X, world.Y, CacheElevation (world.X, world.Y));
			mPos[mWalk.X, mWalk.Y] = pos;
			if (mWalk.Walk (TERRAIN_EDGE))
				mStage++;
		}

		/* This is tricky stuff.  When this is called, it means the given point is needed for the terrain we are working on.
		 * Each point, when activated, will recusivly require two other points at the next lowest level of detail.
		 * This is what causes the "shattering" effect that breaks the terrain into triangles. If you want to know more,
		 * Google for Peter Lindstrom, the inventor of this very clever system. */
		private void PointActivate(int x, int y) {
			if (x < 0 || x > TERRAIN_SIZE || y < 0 || y > TERRAIN_SIZE)
				return;
			if (GetPoint(x,y))
				return;

			mPoint[x, y] = true;
			int xl = Boundary(x);
			int yl = Boundary(y);
			int level = Math.Min(xl, yl);

			if (xl > yl) {
				PointActivate (x - level, y);
				PointActivate (x + level, y);
			} else if (xl < yl) {
				PointActivate (x, y + level);
				PointActivate (x, y - level);
			} else {
				int x2 = x & (level * 2);
				int y2 = y & (level * 2);

				if (x2 == y2) {
					PointActivate (x - level, y + level);
					PointActivate (x + level, y - level);
				} else {
					PointActivate (x + level, y + level);
					PointActivate (x - level, y - level);
				}
			}
		}

		/*
		 *       upper
		 *    ul-------ur
		 *     |\      |
		 *    l| \     |r
		 *    e|  \    |i
		 *    f|   c   |g
		 *    t|    \  |h
		 *     |     \ |t
		 *     |      \|
		 *    ll-------lr
		 *       lower
		 *
		 * This considers a quad for splitting. This is done by looking to see how 
		 * coplanar the quad is.  The elevation of the corners are averaged, and compared 
		 * to the elevation of the center.  The geater the difference between these two 
		 * values, the more non-coplanar this quad is. */
		private void DoQuad (int x1, int y1, int size) {
			int half = size / 2;
			int xc = x1 + half;
			int x2 = x1 + size;
			int yc = y1 + half;
			int y2 = y1 + size;

			if (x2 > TERRAIN_SIZE || y2 > TERRAIN_SIZE || x1 < 0 || y1 < 0)
				return;
			float ul = mPos[x1, y1].Z;
			float ur = mPos[x2, y1].Z;
			float ll = mPos[x1, y2].Z;
			float lr = mPos[x2, y2].Z;
			float center = mPos[xc, yc].Z;
			float average = (ul + lr + ll + ur) / 4.0f;

			// Look for a delta between the center point and the average elevation
			float delta = Math.Abs(average - center);

			// Scale the delta based on the size of the quad we are dealing with delta /= (float)size;
			if (delta > TOLERANCE)
				PointActivate (xc, yc);
		}

		private void TrianglePush (int i1, int i2, int i3) {
			mIndexBuffer.Add(i1);
			mIndexBuffer.Add(i2);
			mIndexBuffer.Add(i3);
		}

		/*
		 *                        North                 N
		 *  *-------*           *---+---*           *---*---*     *---+---*
		 *  |\      |           |\     /|           |\Nl|Nr/|     |   |   |
		 *  | \ Sup |           | \   / |           | \ | / |     | A | B |
		 *  |  \    |           |  \ /  |           |Wr\|/El|     |   |   |
		 *  |   \   |       West+   *   +East      W*---*---*E    *---+---*
		 *  |    \  |           |  / \  |           |Wl/|\Er|     |   |   |
		 *  | Inf \ |           | /   \ |           | / | \ |     | C | D |
		 *  |      \|           |/     \|           |/Sr|Sl\|     |   |   |
		 *  *-------*           *---+---*           *---*---*     *---*---*
		 *                        South                 S
		 *
		 *  Figure a            Figure b            Figure c      Figure d
		 *
		 * This takes a single quadtree block and decides how to divide it for rendering. If the center point is not included
		 * in the mesh (or if there IS no center because we are at the lowest level of the tree), then the block is simply
		 * cut into two triangles. (Figure a)
		 *
		 * If the center point is active, but none of the Edges, the block is cut into four triangles. (Fig. b)
		 * If the Edges are active, then the block is cut into a combination of smaller triangles (Fig. c) and sub-blocks (Fig. d). */
		private void CompileBlock(int x, int y, int size) {
			// Define the shape of this block. x and y are the upper-left (Northwest), origin, xc and yc define the center,
			// and x2, y2 mark the lower-right (Southeast) corner, and next_size is half the size of this block.
			int next_size = size / 2;
			int x2 = x + size;
			int y2 = y + size;
			int xc = x + next_size;
			int yc = y + next_size;

			/*    n0--n1--n2
						|        |
						n3  n4  n5
						|        |
						n6--n7--n8    */
			int n0 = mIndexMap[x, y];
			int n1 = mIndexMap[xc, y];
			int n2 = mIndexMap[x2, y];
			int n3 = mIndexMap[x, yc];
			int n4 = mIndexMap[xc, yc];
			int n5 = mIndexMap[x2, yc];
			int n6 = mIndexMap[x, y2];
			int n7 = mIndexMap[xc, y2];
			int n8 = mIndexMap[x2, y2];

			// If this is the smallest block, or the center is inactive, then just cut into two triangles as shown in Figure a
			if (size == 1 || !GetPoint(xc, yc)) {
				if ((x / size + y / size) % 2 != 0) {
					TrianglePush (n0, n8, n2);
					TrianglePush (n0, n6, n8);
				} else {
					TrianglePush (n0, n6, n2);
					TrianglePush (n2, n6, n8);
				}
				return;
			} 

			// If the Edges are inactive, we need 4 triangles (fig b)
			if (!GetPoint(xc, y) && !GetPoint(xc, y2) && !GetPoint(x, yc) && !GetPoint(x2, yc)) {
					TrianglePush (n0, n4, n2); //North
					TrianglePush (n2, n4, n8); //East
					TrianglePush (n8, n4, n6); //South
					TrianglePush (n6, n4, n0); //West
					return;
			}

			// If the top & bottom Edges are inactive, it is impossible to have sub-blocks.
			if (!GetPoint(xc, y) && !GetPoint(xc, y2)) {
				TrianglePush (n0, n4, n2); //North
				TrianglePush (n8, n4, n6); //South
				if (GetPoint (x, yc)) {
					TrianglePush (n3, n4, n0); //Wr
					TrianglePush (n6, n4, n3); //Wl
				} else 
					TrianglePush (n6, n4, n0); //West
				if (GetPoint (x2, yc)) {
					TrianglePush (n2, n4, n5); //El
					TrianglePush (n5, n4, n8); //Er
				} else 
					TrianglePush (n2, n4, n8); //East
				return;
			}
  
			// If the left & right Edges are inactive, it is impossible to have sub-blocks.
			if (!GetPoint(x, yc) && !GetPoint(x2, yc)) {
				TrianglePush (n2, n4, n8); //East
				TrianglePush (n6, n4, n0); //West
				if (GetPoint(xc, y)) {
					TrianglePush (n0, n4, n1); //Nl
					TrianglePush (n1, n4, n2); //Nr
				} else
					TrianglePush (n0, n4, n2); //North
				if (GetPoint(xc, y2)) {
					TrianglePush (n7, n4, n6); //Sr
					TrianglePush (n8, n4, n7); //Sl
				} else
				TrianglePush (n8, n4, n6); //South
				return;
			}

			// None of the other tests worked, which means this block is a combination of triangles and sub-blocks.
			// Brace yourself, this is not for the timid. The first step is to find out which triangles we need
			if (!GetPoint(xc, y)) {  // Is the top Edge inactive?
				TrianglePush (n0, n4, n2); //North
				if (GetPoint(x, yc))
					TrianglePush (n3, n4, n0); //Wr
				if (GetPoint(x2, yc))
					TrianglePush (n2, n4, n5); //El
			}
			if (!GetPoint(xc, y2)) { // Is the bottom Edge inactive?
				TrianglePush (n8, n4, n6); //South
				if (GetPoint (x, yc))
					TrianglePush (n6, n4, n3); //Wl
				if (GetPoint (x2, yc)) 
					TrianglePush (n5, n4, n8); //Er
			}
			if (!GetPoint (x, yc)) { // Is the left Edge inactive?
				TrianglePush (n6, n4, n0); //West
				if (GetPoint (xc, y))
					TrianglePush (n0, n4, n1); //Nl
				if (GetPoint (xc, y2)) 
					TrianglePush (n7, n4, n6); //Sr
			}
			if (!GetPoint (x2, yc)) { // Is the right Edge inactive?
				TrianglePush (n2, n4, n8); //East
				if (GetPoint (xc, y))
					TrianglePush (n1, n4, n2); //Nr
				if (GetPoint (xc, y2)) 
					TrianglePush (n8, n4, n7); //Sl
			}

			// Now that the various triangles have been added, we add the various sub-blocks. This is recursive.
			if (GetPoint (xc, y) && GetPoint (x, yc)) 
				CompileBlock (x, y, next_size); //Sub-block A
			if (GetPoint (xc, y) && GetPoint (x2, yc)) 
				CompileBlock (x + next_size, y, next_size); //Sub-block B
			if (GetPoint (x, yc) && GetPoint (xc, y2)) 
				CompileBlock (x, y + next_size, next_size); //Sub-block C
			if (GetPoint (x2, yc) && GetPoint (xc, y2)) 
				CompileBlock (x + next_size, y + next_size, next_size); //Sub-block D
		}

		/* This checks the four corners of zone data that will be used by this terrain.
		 * Returns true if the data is ready and terrain building can proceed. This
		 * will also "touch" the zone, letting the zone know it's still in use. */
		private bool ZoneCheck(long stop) {
			//If we're waiting on a zone, give it our update allotment
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y)) {
				CacheUpdatePage (mOrigin.X, mOrigin.Y, stop);
				return false;
			}
			if (!CachePointAvailable (mOrigin.X + TERRAIN_EDGE, mOrigin.Y + TERRAIN_EDGE)) {
				CacheUpdatePage (mOrigin.X + TERRAIN_EDGE, mOrigin.Y + TERRAIN_EDGE, stop);
				return false;
			}
			if (!CachePointAvailable (mOrigin.X + TERRAIN_EDGE, mOrigin.Y)) {
				CacheUpdatePage (mOrigin.X + TERRAIN_EDGE, mOrigin.Y, stop);
				return false;
			}
			if (!CachePointAvailable (mOrigin.X, mOrigin.Y + TERRAIN_EDGE)) {
				CacheUpdatePage (mOrigin.X, mOrigin.Y + TERRAIN_EDGE, stop);
				return false;
			}
			return true;
		}
		#endregion

		#region Public methods
		//public Terrain() : base() {}
		//public int Sizeof () { return sizeof (CTerrain); }

		public bool GetPoint(int x, int y) { return mPoint[x, y]; }

		public override void Update(long stop) {
			while (SdlTick () < stop) {
				switch (mStage) {
				case Stage.Begin: 
					if (!ZoneCheck (stop)) 
						break;
					for (int i =0; i < 12; i++) // 12 = number of surface types
						mSurfaceTypeUsed[i] = false;
					for (int i =0; i < NEIGHBOR_COUNT; i++)
						mNeighbors[i] = 0;
					mWalk.Clear ();
					mRebuild = SdlTick ();
					mStage++;
					break;
				case Stage.Clear: 
					mPoint[mWalk.X, mWalk.Y] = false;
					mIndexMap[mWalk.X, mWalk.Y] = -1;
					if (mWalk.Walk (TERRAIN_EDGE))
						mStage++;
					break;
				case Stage.DoCompileGrid: 
					PointActivate (mWalk.X * COMPILE_SIZE, mWalk.Y * COMPILE_SIZE);
					if (mWalk.Walk (COMPILE_GRID + 1))
						mStage++;
					break;
				case Stage.Heightmap: 
					DoHeightmap ();
					break;
				case Stage.Quadtree:
					if (!GetPoint (mWalk.X, mWalk.Y)) {
						int xx = Boundary (mWalk.X);
						int yy = Boundary (mWalk.Y);
						int level = Math.Min(xx, yy);
						DoQuad (mWalk.X - level, mWalk.Y - level, level * 2);
					}
					if (mWalk.Walk (TERRAIN_SIZE))
						mStage++;
					break;  
				case Stage.Stitch:
					DoStitch ();
					mVertexList.Clear();
					mNormalList.Clear();
					mUVList.Clear();
					mIndexBuffer.Clear();
					mStage++;
					break;  
				case Stage.BufferLoad: 
					if (GetPoint(mWalk.X, mWalk.Y)) {
						Coord world;
        
						world.X = mOrigin.X + mWalk.X;
						world.Y = mOrigin.Y + mWalk.Y;
						mIndexMap[mWalk.X, mWalk.Y] = mVertexList.Count;
						mVertexList.Add (mPos[mWalk.X, mWalk.Y]);
						mNormalList.Add (CacheNormal (world.X, world.Y));
						mUVList.Add(new Vector2 ((float) mWalk.X / TERRAIN_SIZE, (float) mWalk.Y / TERRAIN_SIZE));
						//_list_pos++;
					}
					if (mWalk.Walk (TERRAIN_EDGE))
						mStage++;
					break; 
				case Stage.Compile:
					CompileBlock (mWalk.X * COMPILE_SIZE, mWalk.Y * COMPILE_SIZE, COMPILE_SIZE);
					if (mWalk.Walk (COMPILE_GRID))
						mStage++;
					break;
				case Stage.VBO:
					if (mVBO.Ready())
						mVBO.Clear();
					mVBO.Create(BeginMode.Triangles, mIndexBuffer.Count, mVertexList.Count, mIndexBuffer, mVertexList, mNormalList, null, mUVList);
					mStage++;
					break;
				case Stage.Texture: 
					if (TextureSize == TextureDesiredSize) {
						mStage = Stage.Done;
						break;
					}
					DoTexture ();
					break;
				case Stage.TextureFinal: 
					if (mFrontTexture != 0) 
						GL.DeleteTextures (1, ref mFrontTexture); 
					mFrontTexture = mBackTexture;
					mBackTexture = 0;
					TextureSize = TextureDesiredSize;
					mStage++;
					break;
				case Stage.Done:
					Valid = true;
					if (SdlTick () < mRebuild) 
						return;
					ZoneCheck (stop); // Touch the zones to keep them in memory
					mRebuild = SdlTick () + 1000;
					if (mLOD == LOD.High && DoCheckNeighbors ())
						mStage = Stage.Quadtree;
					return;
				default: //any stages not used end up here, skip it
					mStage++;
					break;
				}
			}
		}

		public void Clear() {
			if (mFrontTexture != 0)	GL.DeleteTextures (1, ref mFrontTexture); 
			if (mBackTexture != 0)	GL.DeleteTextures (1, ref mBackTexture); 
			mFrontTexture = 0;
			mBackTexture = 0;
			mStage = Stage.Begin;
			TextureSize = 0;
			mVertexList.Clear();
			mNormalList.Clear();
			mUVList.Clear();
			mIndexBuffer.Clear();
			mWalk.Clear ();
		}

		public void TexturePurge() {
			if (mFrontTexture != 0)	GL.DeleteTextures(1, ref mFrontTexture); 
			if (mBackTexture != 0)	GL.DeleteTextures(1, ref mBackTexture); 
			mFrontTexture = 0;
			mBackTexture = 0;
			TextureSize = 0;
			TextureDesiredSize = 64;
			if (mStage >= Stage.Texture) {
				mStage = Stage.Texture;
				mWalk.Clear();
			}
		}

		public override void Set(int grid_x, int grid_y, int distance) {
			LOD new_lod;

			if (mStage == Stage.Texture)
				return;
		
			if (distance < 2)		new_lod = LOD.High;
			else								new_lod = LOD.Low;

			if (grid_x == mGridPosition.X && grid_y == mGridPosition.Y && mLOD == new_lod)
				return;

			// If this terrain is now in a new location, we have to kill it entirely
			if (grid_x != mGridPosition.X || grid_y != mGridPosition.Y) 
				Clear ();
			else // Just changed LOD, rebuild the texture
				mStage = Stage.Texture;
			mLOD = new_lod;
			mGridPosition.X = grid_x;
			mGridPosition.Y = grid_y;
			mCurrentDistance = distance;
			mOrigin.X = grid_x * TERRAIN_SIZE;
			mOrigin.Y = grid_y * TERRAIN_SIZE;
			mColor = Color4Unique (mGridPosition.X + mGridPosition.Y * 16);
			if (mLOD == LOD.High) {
				mColor = Color4.Magenta;
				TextureDesiredSize = 2048;
			} else {
				mColor = Color4.Cyan;
				TextureDesiredSize = 128;
			}
			//ConsoleLog ("Texture: %d, %d = %d", grid_x, grid_y, TextureDesiredSize);
			mWalk.Clear ();
		}

		public override void Render() {
			if ((mFrontTexture != 0) && Valid) {
				//GL.Color3fv (&mColor.red);
				GL.BindTexture(TextureTarget.Texture2D, mFrontTexture);
				mVBO.Render ();
			}
		}
		#endregion
	}
}