/*-----------------------------------------------------------------------------
  Brush.cs
-------------------------------------------------------------------------------
  This holds the brush object class.  Bushes and the like. 
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

namespace Frontier {
	struct tuft {
		public Vector3[] v = new Vector3[4];};

	enum BrushStage {
		BRUSH_STAGE_BEGIN,
		BRUSH_STAGE_BUILD,
		BRUSH_STAGE_COMPILE,
		BRUSH_STAGE_DONE};

	class Brush : GridData {
		private const int
			BRUSH_TYPES = 4,
			MAX_TUFTS   = 9;
		private const int BRUSH_SIZE = 32;

		private static GLuvbox[]
			box = new GLuvbox[BRUSH_TYPES],
			boxFlower = new GLuvbox[BRUSH_TYPES];
		private static bool prep_done;
		private static tuft[] tuftList = new tuft[MAX_TUFTS];

		private BrushStage stage;
		private int currentDistance;
		private Coord origin, gridPosition, walk;
		
		// List<GLrgba>  colors;
		// List<Vector3> vertices;
		// List<Vector3> normals;
		// List<Vector2> uvs;
		// List<UINT>      index;
		private Mesh        mesh;
		private static VBO  vbo;
		private GLbbox      bbox;

		public bool Valid { get; set; }
		public bool Ready { get { return stage == BrushStage.BRUSH_STAGE_DONE; } }

		private void VertexPush (Vector3 vert, Vector3 normal, Color4 color, Vector2 uv);
		private void QuadPush (int n1, int n2, int n3, int n4);

		public Brush() : base() {
			origin = new Coord(0, 0);
			gridPosition = new Coord();
			walk = new Coord();
			currentDistance = 0;
			Valid = false;
			bbox.Clear();
			gridPosition.Clear();
			walk.Clear();
			mesh.Clear();
			stage = BrushStage.BRUSH_STAGE_BEGIN;
  
			if (!prep_done)
				do_prep ();
		}

		public void Set(int x, int y, int density) {
			if (origin.X == x * BRUSH_SIZE && origin.Y == y * BRUSH_SIZE)
				return;

			gridPosition.X = x;
			gridPosition.Y = y;

			currentDistance = (uint) Math.Abs(density);
			origin.X = x * BRUSH_SIZE;
			origin.Y = y * BRUSH_SIZE;
			stage = BrushStage.BRUSH_STAGE_BEGIN;
			mesh.Clear ();
			bbox.Clear ();
		}

		private static void do_prep() {
			int           i, j;
			GLmatrix      m;
			float         angle_step;

			for (i = 0; i < BRUSH_TYPES; i++) {
				box[i].Set (i, 0, BRUSH_TYPES, 2);
				box[i].lr.y *= 0.99f;
			}
			angle_step = 360.0f / MAX_TUFTS;
			for (i = 0; i < MAX_TUFTS; i++) {
				tuftList[i].v[0] = new Vector3(-1, -1, 0);
				tuftList[i].v[1] = new Vector3(1, -1, 0);
				tuftList[i].v[2] = new Vector3(1, 1, 0);
				tuftList[i].v[3] = new Vector3(-1, 1, 0);
				m.Identity ();
				m.Rotate (angle_step * (float)i, 0.0f, 0.0f, 1.0f);
				for (j = 0; j < 4; j++) 
					tuftList[i].v[j] = m.TransformPoint (tuftList[i].v[j]);
			}
			prep_done = true;
		}

		private bool ZoneCheck() {
			if (!CachePointAvailable (origin.X, origin.Y))
				return false;
			if (!CachePointAvailable (origin.X + BRUSH_SIZE, origin.Y))
				return false;
			if (!CachePointAvailable (origin.X + BRUSH_SIZE,origin.Y + BRUSH_SIZE))
				return false;
			if (!CachePointAvailable (origin.X, origin.Y + BRUSH_SIZE))
				return false;
			return true;
		}

		private void Build(long stop) {
			int world_x = origin.X + walk.X;
			int world_y = origin.Y + walk.Y;

			if (CacheSurface (world_x, world_y) == SURFACE_GRASS_EDGE) {
				Region r					= WorldRegionFromPosition (world_x, world_y);
				int index					= world_x + world_y * BRUSH_SIZE;
				tuft this_tuft		= tuftList[index % MAX_TUFTS];
				Vector3 root			= new Vector3(world_x, world_y, 0.0f);
				float height			= 0.25f + (r.moisture * r.temperature) * 2.0f;

				Vector2 size			= new Vector2(1.0f + WorldNoisef (index) * 1.0f, 1.0f + WorldNoisef (index) * height);
				size.Y						= Math.Max (size.X, size.Y);		// Don't let bushes get wider than they are tall
				
				GLrgba color;
				color = CacheSurfaceColor (world_x, world_y);
				color *= 0.75f;
				color.alpha = 1.0f;

				//Now we construct our grass panels
				Vector3[]	v = new Vector3[8];
				for (int i = 0; i < 4; i++) { 
					v[i]				= Vector3.Multiply(this_tuft.v[i], new Vector3(size.X, size.X, 0.0f));
					v[i + 4]		= Vector3.Multiply(this_tuft.v[i], new Vector3(size.X, size.X, 0.0f));
					v[i + 4].Z += size.Y;
				}

				for (int i = 0; i < 8; i++) {
					v[i] += root;
					v[i].Z += CacheElevation (v[i].X, v[i].Y);
				}

				int patch = r.flower_shape[index % FLOWERS] % BRUSH_TYPES;
				int current = mesh.Vertices ();
				Vector3 normal = CacheNormal(world_x, world_y);

				mesh.PushVertex (v[0], normal, color, box[patch].Corner (1)); 
				mesh.PushVertex (v[1], normal, color, box[patch].Corner (1)); 
				mesh.PushVertex (v[2], normal, color, box[patch].Corner (0)); 
				mesh.PushVertex (v[3], normal, color, box[patch].Corner (0)); 
				mesh.PushVertex (v[4], normal, color, box[patch].Corner (2)); 
				mesh.PushVertex (v[5], normal, color, box[patch].Corner (2)); 
				mesh.PushVertex (v[6], normal, color, box[patch].Corner (3)); 
				mesh.PushVertex (v[7], normal, color, box[patch].Corner (3)); 
				mesh.PushQuad (current, current + 2, current + 6, current + 4);
				mesh.PushQuad (current + 1, current + 3, current + 7, current + 5);
			}
			if (walk.Walk (BRUSH_SIZE)) 
				stage++;
		}

		public void Update(long stop) {
			while (SdlTick() < stop && !Ready ()) {
				switch (stage) {
				case BRUSH_STAGE_BEGIN:
					if (!ZoneCheck ())
						return;
					stage++;
				case BRUSH_STAGE_BUILD:
					Build (stop);
					break;
				case BRUSH_STAGE_COMPILE:
					if (mesh.Vertices ())
						vbo.Create (&mesh);
					else
						vbo.Clear ();
					stage++;
					_valid = true;
					break;
				}
			}
		}

		public void Render() {
			//We need at least one successful build before we can draw.
			if (!_valid)
				return;
			glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
			glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
			glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
			glDisable (GL_CULL_FACE);
			vbo.Render ();
		}
	}
}