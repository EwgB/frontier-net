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
	class Grass {
		enum GrassStage { Begin, Build, Compile, Done };

		struct tuft { public Vector3[] v = new Vector3[4]; };

		private const int
			GRASS_SIZE = 32,
			GRASS_TYPES = 8,
			MAX_TUFTS = 9;

		private Coord           _grid_position;
		private Coord           _origin;
		private Coord           _walk;
		private int          _current_distance;
		private List<Color4>    _color;
		private List<Vector3>  _vertex;
		private List<Vector3>  _normal;
		private List<Vector2> _uv;
		private List<int>      _index;
		private static VBO         _vbo;
		private GrassStage        _stage;
		private BBox            _bbox;

		private static UVBox[]        box_grass = new UVBox[GRASS_TYPES];
		private static UVBox[]        box_flower = new UVBox[GRASS_TYPES];
		private static bool           prep_done;
		private static tuft[]         tuft_list = new tuft[MAX_TUFTS];


		//unsigned          Sizeof () { return sizeof (Grass); }; 
		private bool            Ready  { get { return _stage == GrassStage.Done;} }
		private bool            Valid { get; private set; }

		#region Private methods
		private static void do_prep() {
			float         angle_step;

			for (int i = 0; i < GRASS_TYPES; i++) {
				box_grass[i].Set (i, 2, GRASS_TYPES, 4);
				box_flower[i].Set (i, 3, GRASS_TYPES, 4);
			}
			angle_step = 360.0f / MAX_TUFTS;
			for (int i = 0; i < MAX_TUFTS; i++) {
				tuft_list[i].v[0] = new Vector3 (-1, -1, 0);
				tuft_list[i].v[1] = new Vector3 ( 1, -1, 0);
				tuft_list[i].v[2] = new Vector3 ( 1,  1, 0);
				tuft_list[i].v[3] = new Vector3 (-1,  1, 0);
				Matrix4 m = Matrix4.CreateRotationZ(angle_step * (float)i);
				for (int j = 0; j < 4; j++)
					tuft_list[i].v[j] = m.TransformPoint (tuft_list[i].v[j]);
			}
			prep_done = true;
		}

		private void VertexPush (Vector3 vert, Vector3 normal, Color4 color, Vector2 uv) {
			_vertex.Add(vert);
			_normal.Add(normal);
			_color.Add(color);
			_uv.Add(uv);
			_bbox.ContainPoint(vert);
		}

		private void QuadPush (int n1, int n2, int n3, int n4) {
			_index.Add (n1);
			_index.Add (n2);
			_index.Add (n3);
			_index.Add (n4);
		}

		private bool ZoneCheck () {
			if (!CachePointAvailable (_origin.X, _origin.Y))
				return false;
			if (!CachePointAvailable (_origin.X + GRASS_SIZE, _origin.Y))
				return false;
			if (!CachePointAvailable (_origin.X + GRASS_SIZE,_origin.Y + GRASS_SIZE))
				return false;
			if (!CachePointAvailable (_origin.X, _origin.Y + GRASS_SIZE))
				return false;
			return true;
		}

		private void Build (long stop) {
			int       world_x, world_y;
			bool      do_grass;

			world_x = _origin.X + _walk.X;
			world_y = _origin.Y + _walk.Y;
			do_grass = CacheSurface (world_x, world_y) == SURFACE_GRASS;
			if (_walk.X % _current_distance || _walk.Y  % _current_distance)
				do_grass = false;
			if (do_grass) {
				Vector3[] v = new Vector3[8];
				Vector3    normal;
				Color4      color;
				int         current;
				Vector3    root;
				Vector2   size;
				Region      r;
				float       height;
				int         index;
				bool        do_flower;
				int         patch;
				tuft       this_tuft;
				//Matrix4    mat;

				r = WorldRegionFromPosition (world_x, world_y);
				index = world_x + world_y * GRASS_SIZE;
				this_tuft = tuft_list[index % MAX_TUFTS];
				root.X = (float) world_x + (WorldNoisef (index) -0.5f);
				root.Y = (float) world_y + (WorldNoisef (index) -0.5f);
				root.Z = 0.0f;
				height = 0.05f + r.moisture * r.temperature;
				size.X = 0.4f + WorldNoisef (index) * 0.5f;
				size.Y = WorldNoisef (index) * height + (height / 2);
				do_flower = r.has_flowers;
				if (do_flower) //flowers are shorter than grass
					size.Y /= 2;
				size.Y = Math.Max(size.y, 0.3f);
				color = CacheSurfaceColor (world_x, world_y);
				color.A = 1.0f;
				//Now we construct our grass panels
				for (int i = 0; i < 4; i++) { 
					v[i] = Vector3.Multiply(this_tuft.v[i], new Vector3 (size.X, size.X, 0.0f));
					v[i + 4] = Vector3.Multiply(this_tuft.v[i], new Vector3 (size.X, size.X, 0.0f));
					v[i + 4].Z += size.Y;
				}
				for (int i = 0; i < 8; i++) {
					v[i] += root;
					v[i].Z += CacheElevation (v[i].X, v[i].Y);
				}
				patch = r.flower_shape[index % FLOWERS] % GRASS_TYPES;
				current = _vertex.Count;
				normal = CacheNormal (world_x, world_y);
				VertexPush (v[0], normal, color, box_grass[patch].Corner (1));
				VertexPush (v[1], normal, color, box_grass[patch].Corner (1));
				VertexPush (v[2], normal, color, box_grass[patch].Corner (0));
				VertexPush (v[3], normal, color, box_grass[patch].Corner (0));
				VertexPush (v[4], normal, color, box_grass[patch].Corner (2));
				VertexPush (v[5], normal, color, box_grass[patch].Corner (2));
				VertexPush (v[6], normal, color, box_grass[patch].Corner (3));
				VertexPush (v[7], normal, color, box_grass[patch].Corner (3));
				QuadPush (current, current + 2, current + 6, current + 4);
				QuadPush (current + 1, current + 3, current + 7, current + 5);
				if (do_flower) {
					current = _vertex.Count;
					color = r.color_flowers[index % FLOWERS];
					normal = Vector3.UnitZ;
					VertexPush (v[4], normal, color, box_flower[patch].Corner (0));
					VertexPush (v[5], normal, color, box_flower[patch].Corner (1));
					VertexPush (v[6], normal, color, box_flower[patch].Corner (2));
					VertexPush (v[7], normal, color, box_flower[patch].Corner (3));
					QuadPush (current, current + 1, current + 2, current + 3);
				}
			}
			if (_walk.Walk (GRASS_SIZE)) 
				_stage++;
		}
		#endregion

		#region Public methods
		public Grass () {
			GridData ();
			_origin.X = 0;
			_origin.Y = 0;
			_current_distance = 0;
			Valid = false;
			_bbox.Clear ();
			_grid_position.Clear ();
			_walk.Clear ();
			_stage = GrassStage.Begin;
			if (!prep_done) 
				do_prep ();
		}

		public void Set (int x, int y, int density) {
			//density = max (density, 1); //detail 0 and 1 are the same level. (Maximum density.)
			density = 1;
			if (_origin.X == x * GRASS_SIZE && _origin.Y == y * GRASS_SIZE && density == _current_distance)
				return;
			_grid_position.X = x;
			_grid_position.Y = y;
			_current_distance = density;
			_origin.x = x * GRASS_SIZE;
			_origin.y = y * GRASS_SIZE;
			_stage = GrassStage.Begin;
			_color.Clear();
			_vertex.Clear();
			_normal.Clear();
			_uv.Clear();
			_index.Clear();
			_bbox.Clear();
		}

		public void Update (long stop) {
			while (SdlTick () < stop && !Ready) {
				switch (_stage) {
				case GrassStage.Begin:
					if (!ZoneCheck ())
						return;
					_stage++;
				case GrassStage.Build:
					Build (stop);
					break;
				case GrassStage.Compile:
					if (_vertex.Count != 0)
						_vbo.Create (GL_QUADS, _index.Count, _vertex.Count, _index, _vertex, _normal, _color, _uv);
					else
						_vbo.Clear ();
					_stage++;
					Valid = true;
					break;
				}
			}
		}

		public void Render () {
			// We need at least one successful build before we can draw.
			if (!Valid)
				return;
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			GL.Disable(EnableCap.CullFace);
			_vbo.Render ();
			return;

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			GL.Disable(EnableCap.Blend);
			//glEnable (GL_BLEND);
			//glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
			//glDisable (GL_LIGHTING);
			_vbo.Render ();

			GL.Disable(EnableCap.Texture2D);
			//glDisable (GL_FOG);
			GL.Disable(EnableCap.Lighting);
			GL.DepthFunc(DepthFunction.Equal);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.Zero, BlendingFactorDest.SrcColor);
			GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.SrcColor);
			_vbo.Render ();
			GL.DepthFunc(DepthFunction.Lequal);
			if (false) {
				GL.Color3(1,0,1);
				_bbox.Render ();
			}
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Lighting);
		}
		#endregion
	}
}