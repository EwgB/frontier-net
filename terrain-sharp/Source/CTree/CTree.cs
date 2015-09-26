namespace terrain_sharp.Source.CTree {
	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.OpenGL;

	using StdAfx;

	using System;
	using System.Collections.Generic;

	class CTree {
		const int TREE_ALTS = 3;
		const float SEGMENTS_PER_METER = 0.25f;
		const int MIN_SEGMENTS = 3;
		const int TEXTURE_SIZE = 256;
		const int TEXTURE_HALF = (TEXTURE_SIZE / 2);
		const float MIN_RADIUS = 0.3f;
		static readonly Vector3 UP = new Vector3(0f, 0f, 1f);

		private enum TreeTrunkStyle {
			TREE_TRUNK_NORMAL,
			TREE_TRUNK_JAGGED,
			TREE_TRUNK_BENT,
			TREE_TRUNK_STYLES
		};

		private enum TreeFoliageStyle {
			TREE_FOLIAGE_UMBRELLA,
			TREE_FOLIAGE_BOWL,
			TREE_FOLIAGE_SHIELD,
			TREE_FOLIAGE_PANEL,
			TREE_FOLIAGE_SAG,
			TREE_FOLIAGE_STYLES
		};

		private enum TreeLiftStyle {
			TREE_LIFT_STRAIGHT,
			TREE_LIFT_IN,
			TREE_LIFT_OUT,
			TREE_LIFT_STYLES
		};

		private enum TreeLeafStyle {
			TREE_LEAF_FAN,
			TREE_LEAF_SCATTER,
			TREE_LEAF_STYLES
		};

		TreeTrunkStyle _trunk_style;
		TreeFoliageStyle _foliage_style;
		TreeLiftStyle _lift_style;
		TreeLeafStyle _leaf_style;

		int _seed;
		int _seed_current;
		bool _funnel_trunk;
		bool _evergreen;
		bool _canopy;
		public bool GrowsHigh { get; private set; }
		bool _has_vines;
		public uint Texture { get; private set; }

		int _default_branches;
		float _default_height;
		float _default_bend_frequency;
		float _default_base_radius;
		float _default_lowest_branch;

		int _current_branches;
		float _current_height;
		float _current_bend_frequency;
		float _current_angle_offset;
		float _current_base_radius;
		float _current_lowest_branch;

		float _moisture;
		float _temperature;

		float _texture_tile;

		float _branch_lift;
		float _branch_reach;
		float _foliage_size;
		float _leaf_size;
		glRgba _bark_color1;
		glRgba _bark_color2;
		glRgba _leaf_color;
		List<Leaf> _leaf_list;
		var _meshes = new GLmesh[TREE_ALTS, (int)LOD.Levels];

		public void DrawBark() {
			GL.Color3(_bark_color1);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.Begin(PrimitiveType.Quads);
			GL.TexCoord2(0f, 0f);
			GL.Vertex2(0, 0);
			GL.TexCoord2(1f, 0f);
			GL.Vertex2(TEXTURE_SIZE, 0);
			GL.TexCoord2(1f, 1f);
			GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			GL.TexCoord2(0f, 1f);
			GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();

			GLtexture t = TextureFromName("bark1.bmp");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1.0f / frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			GLuvbox uvframe;
			uvframe.Set(new Vector2(0.0f, frame * frame_size), new Vector2(1.0f, (frame + 1) * frame_size));
			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ColorMask(true, true, true, false);
			GL.Color4(_bark_color2);
			GL.Begin(PrimitiveType.Quads);
			Vector2 uv = uvframe.Corner(0);
			GL.TexCoord2(uv);
			GL.Vertex2(0, 0);
			uv = uvframe.Corner(1);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(2);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(3);
			GL.TexCoord2(uv);
			GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();
			GL.ColorMask(true, true, true, true);
		}

		public void DrawLeaves() {
			if (_leaf_style == TreeLeafStyle.TREE_LEAF_SCATTER) {
				glRgba color = _bark_color1;
				color *= 0.5f;
				GL.BindTexture(TextureTarget.Texture2D, 0);
				GL.LineWidth(3);
				GL.Color3(color);

				GL.Begin(PrimitiveType.Lines);
				foreach (var leaf in _leaf_list) {
					GL.Vertex2(_leaf_list[leaf.neighbor].position);
					GL.Vertex2(leaf.position);
				}
				GL.End();
			}

			GLtexture t = TextureFromName("foliage.png");
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1.0f / (float) frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			GLuvbox uvframe = new GLuvbox();
      uvframe.Set(new Vector2(0f, frame * frame_size), new Vector2(1f, (frame + 1) * frame_size));
			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);	
			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);	
			for (int i = 0; i < _leaf_list.Count; i++) {
				Leaf l = _leaf_list[i];
				GL.PushMatrix();
				GL.Translate(l.position.x, l.position.y, 0);
				GL.Rotate(l.angle, 0.0f, 0.0f, 1.0f);
				GL.Translate(-l.position.x, -l.position.y, 0);

				//GLrgba color = _leaf_color * l.brightness;
				GL.Color3(l.color);
				GL.Begin(PrimitiveType.Quads);
				Vector2 uv = uvframe.Corner(0);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X - l.size, l.position.Y - l.size);
				uv = uvframe.Corner(1);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X + l.size, l.position.Y - l.size);
				uv = uvframe.Corner(2);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X + l.size, l.position.Y + l.size);
				uv = uvframe.Corner(3);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X - l.size, l.position.Y + l.size);
				GL.End();
				GL.PopMatrix();
			}
		}

		public void DrawVines() {
			GL.Color3(&_bark_color1.red);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GLtexture t = TextureFromName("vines.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1.0f / (float) frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			GLuvbox uvframe = new GLuvbox();
			uvframe.Set(new Vector2(0f, frame * frame_size), new Vector2(1f, (frame + 1) * frame_size));
			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GLrgba color = _leaf_color * 0.75f;
			GL.Color3(_leaf_color);
			GL.Begin(PrimitiveType.Quads);
			Vector2 uv = uvframe.Corner(3);
			GL.TexCoord2(uv);
			GL.Vertex2(0, 0);
			uv = uvframe.Corner(0);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(1);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(2);
			GL.TexCoord2(uv);
			GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();
		}

		public void DrawFacer() {
			GLbbox box;
			Vector3 size, center;

			GL.Disable(EnableCap.Blend);
			
			//We get the bounding box for the high-res tree, but we cut off the roots.  No reason to 
			//waste texture pixels on that.
			_meshes[0][LOD.High].RecalculateBoundingBox();
			box = _meshes[0][LOD.High]._bbox;
			box.pmin.z = 0.0f;	//Cuts off roots
			center = box.Center();
			size = box.Count;
			
			//Move our viewpoint to the middle of the texture frame 
			GL.Translate(TEXTURE_HALF, TEXTURE_HALF, 0.0f);
			GL.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);

			//Scale so that the tree will exactly fill the rectangle
			GL.Scale((1.0f / size.X) * TEXTURE_SIZE, 1.0f, (1.0f / size.Z) * TEXTURE_SIZE);
			GL.Translate(-center.X, 0.0f, -center.Z);
			GL.Color3(1, 1, 1);
			Render(new Vector3(), 0, LOD.High);
		}

		void DoVines(GLmesh m, List<Vector3> points, int segments) {
			if (!_has_vines)
				return;
			int base_index = m._vertex.Count;
			for (int segment = 0; segment < segments; segment++) {
				float v = segment;
				m.PushVertex(points[segment], UP, new Vector2(0.75f, v));
				m.PushVertex(Vector3.Add(points[segment], new Vector3(0.0f, 0.0f, -3.5f)), UP, new Vector2(0.5f, v));
			}
			for (int segment = 0; segment < segments - 1; segment++) {
				m.PushTriangle(
							base_index + segment * 2,
							base_index + segment * 2 + 1,
							base_index + (segment + 1) * 2 + 1);
				m.PushTriangle(
							base_index + segment * 2,
							base_index + (segment + 1) * 2 + 1,
							base_index + (segment + 1) * 2);
			}
		}

		void DoFoliage(GLmesh m, Vector3 pos, float size, float angle);
		void DoBranch(GLmesh m, BranchAnchor anchor, float angle, LOD lod);
		void DoTrunk(GLmesh m, uint local_seed, LOD lod);
		void DoLeaves();
		void DoTexture();
		Vector3 TrunkPosition(float delta, float radius);
		void Build();

		public void Create(bool canopy, float moisture, float temperature, int seed);
		public void Render(Vector3 pos, uint alt, LOD lod);
		public void TexturePurge();
		public GLmesh Mesh(uint alt, LOD lod);
		public void Info();

		// TODO: convert comparer
		/*int sort_leaves (const void* elem1, const void* elem2)
		{
			Leaf*   e1 = (Leaf*)elem1;
			Leaf*   e2 = (Leaf*)elem2;

			if (e1.dist < e2.dist)
				return -1;
			else if (e1.dist > e2.dist)
				return 1;
			return 0;
		}*/


		/*
		GLmesh* Mesh (uint alt, LOD lod)
		{
			return &_meshes[alt % TREE_ALTS][lod];
		}

		//Given the value of 0.0 (root) to 1.0f (top), return the center of the trunk 
		//at that height.
		Vector3 TrunkPosition (float delta, float* radius_in)
		{
			Vector3    trunk;
			float       bend;
			float       delta_curve;
			float       radius;

			if (_funnel_trunk) {
				delta_curve = 1.0f - delta;
				delta_curve *= delta_curve;
				delta_curve = 1.0f - delta_curve;
			} else 
				delta_curve = delta;
			if (_canopy) //canopy trees are thick all the way up, do not taper to a point
				radius = _current_base_radius * (1.0f - delta_curve * 0.5);
			else
				radius = _current_base_radius * (1.0f - delta_curve);

			radius = Math.Max (radius, MIN_RADIUS);
			bend = delta * delta;
			switch (_trunk_style) {
			case TREE_TRUNK_BENT:
				trunk.x = bend * _current_height / 3.0f;
				trunk.y = 0.0f;
				break;
			case TREE_TRUNK_JAGGED:
				trunk.x = bend * _current_height / 2.0f;
				trunk.y = sin (delta * _current_bend_frequency) * _current_height / 3.0f;
				break;
			case TREE_TRUNK_NORMAL:
			default:
				trunk.x = 0.0f;
				trunk.y = 0.0f;
				break;
			}
			trunk.z = delta * _current_height;
			if (radius_in)
				*radius_in = radius;
			return trunk;
  
		}

		void DoFoliage (GLmesh* m, Vector3 pos, float fsize, float angle)
		{

			GLuvbox   uv;
			int       base_index;

			fsize *= _foliage_size;
			uv.Set (Vector3 (0.25f, 0.0f), Vector3 (0.5f, 1.0f));
			base_index = m._vertex.size ();

			//don't let the foliage get so big it touches the ground.
			fsize = min (pos.z - 2.0f, fsize);
			if (fsize < 0.1f)
				return;
			if (_foliage_style == TREE_FOLIAGE_PANEL) {
				m.PushVertex (Vector3 (-0.0f, -fsize, -fsize), UP, uv.Corner (0));
				m.PushVertex (Vector3 (-1.0f,  fsize, -fsize), UP, uv.Corner (1));
				m.PushVertex (Vector3 (-1.0f,  fsize,  fsize), UP, uv.Corner (2));
				m.PushVertex (Vector3 (-0.0f, -fsize,  fsize), UP, uv.Corner (3));

				m.PushVertex (Vector3 ( 0.0f, -fsize, -fsize), UP, uv.Corner (1));
				m.PushVertex (Vector3 ( 1.0f,  fsize, -fsize), UP, uv.Corner (2));
				m.PushVertex (Vector3 ( 1.0f,  fsize,  fsize), UP, uv.Corner (3));
				m.PushVertex (Vector3 ( 0.0f, -fsize,  fsize), UP, uv.Corner (0));

				m.PushQuad (base_index + 0, base_index + 1, base_index + 2, base_index + 3);
				m.PushQuad (base_index + 7, base_index + 6, base_index + 5, base_index + 4);

			} else if (_foliage_style == TREE_FOLIAGE_SHIELD) {
				m.PushVertex (Vector3 ( fsize / 2, 0.0f,  0.0f), UP, uv.Center ());
				m.PushVertex (Vector3 (0.0f, -fsize, 0.0f), UP, uv.Corner (0));
				m.PushVertex (Vector3 (0.0f,  0.0f,  fsize), UP, uv.Corner (1));
				m.PushVertex (Vector3 (0.0f,  fsize, 0.0f), UP, uv.Corner (2));
				m.PushVertex (Vector3 (0.0f,  0.0f,  -fsize), UP, uv.Corner (3));
				m.PushVertex (Vector3 (-fsize / 2, 0.0f,  0.0f), UP, uv.Center ());
				//Cap
				m.PushTriangle (base_index, base_index + 1, base_index + 2);
				m.PushTriangle (base_index, base_index + 2, base_index + 3);
				m.PushTriangle (base_index, base_index + 3, base_index + 4);
				m.PushTriangle (base_index, base_index + 4, base_index + 1);
				m.PushTriangle (base_index + 5, base_index + 2, base_index + 1);
				m.PushTriangle (base_index + 5, base_index + 3, base_index + 2);
				m.PushTriangle (base_index + 5, base_index + 4, base_index + 3);
				m.PushTriangle (base_index + 5, base_index + 1, base_index + 4);
			} else if (_foliage_style == TREE_FOLIAGE_SAG) {
				/*     /\
							/__\
						 /|  |\
						 \|__|/
							\  /
							 \/   */
				/*float level1   = fsize * -0.4f;
				float level2   = fsize * -1.2f;
				GLuvbox   uv_inner;

				uv_inner.Set (Vector3 (0.25f + 1.25f, 0.125f), Vector3 (0.5f - 0.125f, 1.0f - 0.125f));
				//Center
				m.PushVertex (Vector3 ( 0.0f, 0.0f, 0.0f), UP, uv.Center ());
				//First ring
				m.PushVertex (Vector3 (-fsize / 2, -fsize / 2, level1), UP, uv.Corner (GLUV_TOP_EDGE));//1
				m.PushVertex (Vector3 ( fsize / 2, -fsize / 2, level1), UP, uv.Corner (GLUV_RIGHT_EDGE));//2
				m.PushVertex (Vector3 ( fsize / 2,  fsize / 2, level1), UP, uv.Corner (GLUV_BOTTOM_EDGE));//3
				m.PushVertex (Vector3 (-fsize / 2,  fsize / 2, level1), UP, uv.Corner (GLUV_LEFT_EDGE));//4
				//Tips
				m.PushVertex (Vector3 (0.0f, -fsize, level2), UP, uv.Corner (1));//5
				m.PushVertex (Vector3 (fsize,  0.0f, level2), UP, uv.Corner (2));//6
				m.PushVertex (Vector3 (0.0f,  fsize, level2), UP, uv.Corner (3));//7
				m.PushVertex (Vector3 (-fsize, 0.0f, level2), UP, uv.Corner (0));//8
				//Center, but lower
				m.PushVertex (Vector3 ( 0.0f, 0.0f, level1 / 16), UP, uv.Center ());
    
				//Cap
				m.PushTriangle (base_index, base_index + 2, base_index + 1);
				m.PushTriangle (base_index, base_index + 3, base_index + 2);
				m.PushTriangle (base_index, base_index + 4, base_index + 3);
				m.PushTriangle (base_index, base_index + 1, base_index + 4);
				//Outer triangles
				m.PushTriangle (base_index + 5, base_index + 1, base_index + 2);
				m.PushTriangle (base_index + 6, base_index + 2, base_index + 3);
				m.PushTriangle (base_index + 7, base_index + 3, base_index + 4);
				m.PushTriangle (base_index + 8, base_index + 4, base_index + 1);
			} else if (_foliage_style == TREE_FOLIAGE_BOWL) {
				float  tip_height;

				tip_height = fsize / 4.0f;
				if (_foliage_style == TREE_FOLIAGE_BOWL)
					tip_height *= -1.0f;
				m.PushVertex (Vector3 (0.0f, 0.0f, tip_height), Vector3 (0.0f, 0.0f, 1.0f), uv.Center ());
				m.PushVertex (Vector3 (-fsize, -fsize, -tip_height), Vector3 (-0.5f, -0.5f, 0.0f), uv.Corner (0));
				m.PushVertex (Vector3 (fsize, -fsize, -tip_height), Vector3 ( 0.5f, -0.5f, 0.0f), uv.Corner (1));
				m.PushVertex (Vector3 (fsize, fsize, -tip_height), Vector3 ( 0.5f, 0.5f, 0.0f), uv.Corner (2));
				m.PushVertex (Vector3 (-fsize, fsize, -tip_height), Vector3 ( -0.5f, 0.5f, 0.0f), uv.Corner (3));
				m.PushVertex (Vector3 (0.0f, 0.0f, tip_height / 2), Vector3 (0.0f, 0.0f, 1.0f), uv.Center ());
				m.PushTriangle (base_index, base_index + 1, base_index + 2);
				m.PushTriangle (base_index, base_index + 2, base_index + 3);
				m.PushTriangle (base_index, base_index + 3, base_index + 4);
				m.PushTriangle (base_index, base_index + 4, base_index + 1);

				m.PushTriangle (base_index + 5, base_index + 2, base_index + 1);
				m.PushTriangle (base_index + 5, base_index + 3, base_index + 2);
				m.PushTriangle (base_index + 5, base_index + 4, base_index + 3);
				m.PushTriangle (base_index + 5, base_index + 1, base_index + 4);

				//m.PushQuad (base_index + 1, base_index + 4, base_index + 3, base_index + 2);
			} else if (_foliage_style == TREE_FOLIAGE_UMBRELLA) {
				float  tip_height;

				tip_height = fsize / 4.0f;
				m.PushVertex (Vector3 (0.0f, 0.0f, tip_height), Vector3 (0.0f, 0.0f, 1.0f), uv.Center ());
				m.PushVertex (Vector3 (-fsize, -fsize, -tip_height), Vector3 (-0.5f, -0.5f, 0.0f), uv.Corner (0));
				m.PushVertex (Vector3 (fsize, -fsize, -tip_height), Vector3 ( 0.5f, -0.5f, 0.0f), uv.Corner (1));
				m.PushVertex (Vector3 (fsize, fsize, -tip_height), Vector3 ( 0.5f, 0.5f, 0.0f), uv.Corner (2));
				m.PushVertex (Vector3 (-fsize, fsize, -tip_height), Vector3 ( -0.5f, 0.5f, 0.0f), uv.Corner (3));
				m.PushVertex (Vector3 (0.0f, 0.0f, tip_height / 2), Vector3 (0.0f, 0.0f, 1.0f), uv.Center ());
				//Top
				m.PushTriangle (base_index, base_index + 2, base_index + 1);
				m.PushTriangle (base_index, base_index + 3, base_index + 2);
				m.PushTriangle (base_index, base_index + 4, base_index + 3);
				m.PushTriangle (base_index, base_index + 1, base_index + 4);
			}   
			GLmatrix  mat;
			uint  i;
			//angle = MathAngle (pos.x, pos.y, 0.0f, 0.0f);
			//angle += 45.0f;
			mat.Identity ();
			mat.Rotate (angle, 0.0f, 0.0f, 1.0f);
			for (i = base_index; i < m._vertex.size (); i++) {
				m._vertex[i] = glMatrixTransformPoint (mat, m._vertex[i]);
				m._vertex[i] += pos;
			}

		}

		void DoBranch (GLmesh* m, BranchAnchor anchor, float branch_angle, LOD lod)
		{
  
			uint          ring, segment, segment_count;
			uint          radial_steps, radial_edge;
			float             radius;
			float             angle;
			float             horz_pos;
			float             curve;
			Vector3          core;
			Vector3          pos;
			uint          base_index;
			GLmatrix          mat;
			Vector2         uv;  
			vector<Vector3>  underside;

			if (anchor.length < 2.0f)
				return;
			if (anchor.radius < MIN_RADIUS)
				return;
			segment_count = (int)(anchor.length * SEGMENTS_PER_METER);
			segment_count = Math.Max (segment_count, MIN_SEGMENTS);
			segment_count += 3;
			base_index = m._vertex.size ();
			mat.Identity ();
			mat.Rotate (branch_angle, 0.0f, 0.0f, 1.0f);
			if (lod == LOD_LOW) {
				segment_count = 2;
				radial_steps = 2;
			} else if (lod == LOD_MED) {
				radial_steps = 2;
				segment_count = 3;
			} else {
				segment_count = 5;
				radial_steps = 6;
			}
			radial_edge = radial_steps + 1;
			core = anchor.root;
			radius = anchor.radius;
			for (segment= 0; segment <= segment_count; segment++) {
				horz_pos = (float)segment/ (float)(segment_count + 1);
				if (_lift_style == TREE_LIFT_OUT) 
					curve = horz_pos * horz_pos;
				else if (_lift_style == TREE_LIFT_IN) {
					curve = 1.0f - horz_pos;
					curve *= curve * curve;;
					curve = 1.0f - curve;
				} else //Straight
					curve = horz_pos;
				radius = Math.Max (MIN_RADIUS, anchor.radius * (1.0f - horz_pos));
				core.z = anchor.root.z + anchor.lift * curve * _branch_lift;
				uv.x = 0.0f;
				//if this is the last segment, don't make a ring of points. Make ONE, in the center.
				//This is so the branch can end at a point.
				if (segment== segment_count) {
					pos.x = 0.0f;
					pos.y = anchor.length * horz_pos;
					pos.z = 0.0f;
					pos = glMatrixTransformPoint (mat, pos);
					m.PushVertex (pos + core, Vector3 (pos.x, 0.0f, pos.z), Vector3 (0.249f, pos.y * _texture_tile));
				} else for (ring = 0; ring <= radial_steps; ring++) {
					//Make sure the final edge perfectly matches the starting one. Can't leave
					//this to floating-point math.
					if (ring == radial_steps || ring == 0)
						angle = 0.0f;
					else
						angle = (float)ring * (360.0f / (float)radial_steps);
					angle *= DEGREES_TO_RADIANS;
					pos.x = -sin (angle) * radius;
					pos.y = anchor.length * horz_pos;
					pos.z = -cos (angle) * radius;
					pos = glMatrixTransformPoint (mat, pos);
					m.PushVertex (pos + core, Vector3 (pos.x, 0.0f, pos.z), Vector3 (((float)ring / (float) radial_steps) * 0.249f, pos.y * _texture_tile));
				}
				underside.push_back (pos + core);
			}
			//Make the triangles for the branch
			for (segment = 0; segment< segment_count; segment++) {
				for (ring = 0; ring < radial_steps; ring++) {
					if (segment< segment_count - 1) {
						m.PushQuad (base_index + (ring + 0) + (segment+ 0) * (radial_edge),
							base_index + (ring + 0) + (segment+ 1) * (radial_edge),
							base_index + (ring + 1) + (segment+ 1) * (radial_edge),
							base_index + (ring + 1) + (segment+ 0) * (radial_edge));
					} else {//this is the last segment. It ends in a single point
						m.PushTriangle (
							base_index + (ring + 1) + segment* (radial_edge),
							base_index + (ring + 0) + segment* (radial_edge),
							m.Vertices () - 1);
					}
				}
			}
			//Grab the last point and use it as the origin for the foliage
			pos = m._vertex[m.Vertices () - 1];
			DoFoliage (m, pos, anchor.length * 0.56f, branch_angle);
			//We saved the points on the underside of the branch.
			//Use these to hang vines on the branch
			if (lod == LOD.High)
				DoVines (m, &underside[0], underside.size ());

		}

		void DoTrunk (GLmesh* m, uint local_seed, LOD lod)
		{

			int                   ring, segment, segment_count;
			int                   radial_steps, radial_edge;
			float                 branch_spacing;
			float                 angle;
			float                 radius;
			float                 x, y;
			float                 vertical_pos;
			float                 circumference;
			Vector3              core;
			vector<BranchAnchor>  branch_list;
			BranchAnchor          branch;
			int                   i;

			//Determine the branch locations
			branch_spacing = (0.95f - _current_lowest_branch) / (float)_current_branches;
			for (i = 0; i < _current_branches; i++) {
				vertical_pos = _current_lowest_branch + branch_spacing * (float)i;
				branch.root = TrunkPosition (vertical_pos, &branch.radius);
				branch.length = (_current_height - branch.root.z) * _branch_reach;
				branch.length = min (branch.length, _current_height / 2);
				branch.lift = (branch.length) / 2;
				branch_list.push_back (branch);
			}
			//Just make a 2-panel facer
			if (lod == LOD_LOW) {
				GLuvbox   uv;
				float     width, height;

				//Use the fourth frame of our texture
				uv.Set (Vector3 (0.75f, 0.0f), Vector3 (1.0f, 1.0f));
				height = _current_height;
				width = _current_height / 2.0f;
				//First panel
				m.PushVertex (Vector3 (-width, -width, 0.0f),   Vector3 (-width, -width, 0.0f), uv.Corner (0));
				m.PushVertex (Vector3 ( width,  width, 0.0f),   Vector3 ( width,  width, 0.0f), uv.Corner (1));
				m.PushVertex (Vector3 ( width,  width, height), Vector3 ( width,  width, height), uv.Corner (2));
				m.PushVertex (Vector3 (-width, -width, height), Vector3 (-width, -width, height), uv.Corner (3));
				//Second Panel
				m.PushVertex (Vector3 (-width,  width, 0.0f),   Vector3 (-width,  width, 0.0f), uv.Corner (0));
				m.PushVertex (Vector3 ( width, -width, 0.0f),   Vector3 ( width, -width, 0.0f), uv.Corner (1));
				m.PushVertex (Vector3 ( width, -width, height), Vector3 ( width, -width, height), uv.Corner (2));
				m.PushVertex (Vector3 (-width,  width, height), Vector3 (-width,  width, height), uv.Corner (3));
				for (i = 0; i < (int)m._normal.size (); i++) 
					m._normal[i].Normalize ();
				m.PushQuad (0, 1, 2, 3);
				m.PushQuad (4, 5, 6, 7);
				return;
			}
			//Work out the circumference of the BASE of the tree
			circumference = _current_base_radius * _current_base_radius * (float)PI;
			//The texture will repeat ONCE horizontally around the tree.  Set the vertical to repeat in the same distance.
			_texture_tile = 1;//(float)((int)circumference + 0.5f); 
			radial_steps = 3;
			if (lod == LOD.High)
				radial_steps = 7;
			radial_edge = radial_steps + 1;
			segment_count = 0;
			//Work our way up the tree, building rings of verts
			for (i = -1; i < (int)branch_list.size (); i++) {
				if (i < 0) { //-1 is the bottom rung, the root. Put it underground, widen it a bit
					core = TrunkPosition (0.0f, &radius);
					radius *= 1.5f;
					core.z -= 2.0f;
				} else {
					core = branch_list[i].root;
					radius = branch_list[i].radius;
				}
				for (ring = 0; ring <= radial_steps; ring++) {
					//Make sure the final edge perfectly matches the starting one. Can't leave
					//this to floating-point math.
					if (ring == radial_steps || ring == 0)
						angle = 0.0f;
					else
						angle = (float)ring * (360.0f / (float)radial_steps);
					angle *= DEGREES_TO_RADIANS;
					x = sin (angle);
					y = cos (angle);
					m.PushVertex (core + Vector3 (x * radius, y * radius, 0.0f),
						Vector3 (x, y, 0.0f),
						Vector3 (((float)ring / (float) radial_steps) * 0.249f, core.z * _texture_tile));

				}
				segment_count++;
			}
			//Push one more point, for the very tip of the tree
			m.PushVertex (TrunkPosition (1.0f, NULL), Vector3 (0.0f, 0.0f, 1.0f), Vector3 (0.0f, 0.0f));
			//Make the triangles for the main trunk.
			for (segment = 0; segment < segment_count - 1; segment++) {
				for (ring = 0; ring < radial_steps; ring++) {
					m.PushQuad ((ring + 0) + (segment + 0) * (radial_edge),
						(ring + 1) + (segment + 0) * (radial_edge),
						(ring + 1) + (segment + 1) * (radial_edge),
						(ring + 0) + (segment + 1) * (radial_edge));

				}
			}
  
			//Make the triangles for the tip
			for (ring = 0; ring < radial_steps; ring++) {
				m.PushTriangle ((ring + 1) + (segment_count - 1) * radial_edge, m._vertex.size () - 1,
					(ring + 0) + (segment_count - 1) * radial_edge);
			}
			DoFoliage (m, m._vertex[m._vertex.size () - 1] + Vector3 (0.0f, 0.0f, -0.0f), _current_height / 2, 0.0f);
			//if (!_canopy) {
				//DoFoliage (TrunkPosition (vertical_pos, NULL), vertical_pos * _height, 0.0f);
				if (_evergreen) { //just rings of foliage, like an evergreen
					for (i = 0; i < (int)branch_list.size (); i++) {
						angle = (float)i * ((360.0f / (float)branch_list.size ()));
						DoFoliage (m, branch_list[i].root, branch_list[i].length, angle);
					}
				} else { //has branches
					for (i = 0; i < (int)branch_list.size (); i++) {
						angle = _current_angle_offset + (float)i * ((360.0f / (float)branch_list.size ()) + 180.0f);
						DoBranch (m, branch_list[i], angle, lod);
					}
				} 
			//}

		}

		void Build ()
		{

			uint    lod;
			uint    alt;

			//_branches = 3 + WorldNoisei (_seed_current++) % 3;
			//_trunk_bend_frequency = 3.0f + WorldNoisef (_seed_current++) * 4.0f;
			_seed_current = _seed;
			for (alt = 0; alt < TREE_ALTS; alt++) {
				_current_angle_offset = WorldNoisef (_seed_current++) * 360.0f;
				_current_height = _default_height * ( 0.5f + WorldNoisef (_seed_current++));
				_current_base_radius = _default_base_radius * (0.5f + WorldNoisef (_seed_current++));
				_current_branches = _default_branches + WorldNoisei (_seed_current++) % 3;
				_current_bend_frequency = _default_bend_frequency + WorldNoisef (_seed_current++);
				_current_lowest_branch = _default_lowest_branch + WorldNoisef (_seed_current++) * 0.2f;
				for (lod = 0; lod < LOD_LEVELS; lod++) {
					_meshes[alt][lod].Clear ();
					DoTrunk (&_meshes[alt][lod], _seed_current + alt, (LOD)lod);
					//The facers use hand-made normals, so don't recalculate them.
					if (lod != LOD_LOW)
						_meshes[alt][lod].CalculateNormalsSeamless ();
				}
			}

		}

		void Create (bool is_canopy, float moisture, float temp_in, int seed_in)
		{
  
			//Prepare, clear the tables, etc.
			_leaf_list.clear ();
			_seed = seed_in;
			_seed_current = _seed;
			_moisture = moisture;
			_canopy = is_canopy;
			_temperature = temp_in;
			_seed_current = _seed;
			//We want our height to fall on a bell curve
			_default_height = 8.0f + WorldNoisef (_seed_current++) * 4.0f + WorldNoisef (_seed_current++) * 4.0f;
			_default_bend_frequency = 1.0f + WorldNoisef (_seed_current++) * 2.0f;
			_default_base_radius = 0.2f + (_default_height / 20.0f) * WorldNoisef (_seed_current++);
			_default_branches = 2 + WorldNoisei (_seed_current) % 2;
			//Keep branches away from the ground, since they don't have collision
			_default_lowest_branch = (3.0f / _default_height);
			//Funnel trunk trees taper off quickly at the base.
			_funnel_trunk = (WorldNoisei (_seed_current++) % 6) == 0;
			if (_funnel_trunk) {//Funnel trees need to be bigger and taller to look right
				_default_base_radius *= 1.2f;
				_default_height *= 1.5f;
			}
			_trunk_style = (TreeTrunkStyle)(WorldNoisei (_seed_current) % TREE_TRUNK_STYLES); 
			_foliage_style = (TreeFoliageStyle)(WorldNoisei (_seed_current++) % TREE_FOLIAGE_STYLES);
			_lift_style = (TreeLiftStyle)(WorldNoisei (_seed_current++) % TREE_LIFT_STYLES);
			_leaf_style = (TreeLeafStyle)(WorldNoisei (_seed_current++) % TREE_LEAF_STYLES);
			_evergreen = _temperature + (WorldNoisef (_seed_current++) * 0.25f) < 0.5f;
			_has_vines = _moisture > 0.6f && _temperature > 0.5f;
			//Narrow trees can gorw on top of hills. (Big ones will stick out over cliffs, so we place them low.)
			if (_default_base_radius <= 1.0f) 
				_grows_high = true;
			else 
				_grows_high = false;
			_branch_reach = 1.0f + WorldNoisef (_seed_current++) * 0.5f;
			_branch_lift = 1.0f + WorldNoisef (_seed_current++);
			_foliage_size = 1.0f;
			_leaf_size = 0.125f;
			_leaf_color = TerraformColorGenerate (SURFACE_COLOR_GRASS, moisture, _temperature, _seed_current++);
			_bark_color2 = TerraformColorGenerate (SURFACE_COLOR_DIRT, moisture, _temperature, _seed_current++);
			_bark_color1 = _bark_color2 * 0.5f;
			//1 in 8 non-tropical trees has white bark
			if (!_has_vines && !(WorldNoisei (_seed_current++) % 8))
				_bark_color2 = glRgba (1.0f);
			//These foliage styles don't look right on evergreens.
			if (_evergreen && _foliage_style == TREE_FOLIAGE_BOWL)
				_foliage_style = TREE_FOLIAGE_UMBRELLA;
			if (_evergreen && _foliage_style == TREE_FOLIAGE_SHIELD)
				_foliage_style = TREE_FOLIAGE_UMBRELLA;
			if (_evergreen && _foliage_style == TREE_FOLIAGE_PANEL)
				_foliage_style = TREE_FOLIAGE_SAG;
			if (_canopy) {
				_foliage_style = TREE_FOLIAGE_UMBRELLA;
				_default_height = Math.Max (_default_height, 16.0f);
				_default_base_radius = 1.5f;
				_foliage_size = 2.0f;
				_trunk_style = TREE_TRUNK_NORMAL;
			}
			Build ();
			DoLeaves ();
			DoTexture ();

		}

		//Render a single tree. Very slow. Used for debugging. 
		void Render (Vector3 pos, uint alt, LOD lod)
		{

			glEnable (EnableCap.Blend);
			glEnable (TextureTarget.Texture2D);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BindTexture (TextureTarget.Texture2D, _texture);
			glPushMatrix ();
			GL.Translate (pos.x, pos.y, pos.z);
			_meshes[alt][lod].Render ();
			glPopMatrix ();

		}

		void DoLeaves ()
		{


			uint          i;
			Leaf              l;

			int     total_steps;
			float   x;
			float   size;
			float   radius;
			float   current_steps, step_size;
			float   circ;
			float   rad;

			if (_leaf_style == TREE_LEAF_FAN) {
				total_steps = 5;
				current_steps = (float)total_steps;
				for (current_steps = (float)total_steps; current_steps >= 1.0f; current_steps -= 1.0f) {
					size = (TEXTURE_HALF / 2) / (1.0f + ((float)total_steps - current_steps));
					radius = (TEXTURE_HALF - size * 2.0f);
					circ = (float)PI * radius * 2;
					step_size = 360.0f / current_steps;
					for (x = 0.0f; x < 360.0f; x += step_size) {
						rad = x * DEGREES_TO_RADIANS;
						l.size = size;
						l.position.x = TEXTURE_HALF + sin (rad) * l.size;
						l.position.y = TEXTURE_HALF + cos (rad) * l.size;
						l.angle = -MathAngle (TEXTURE_HALF, TEXTURE_HALF, l.position.x, l.position.y);
						//l.brightness = 1.0f - (current_steps / (float)total_steps) * WorldNoisef (_seed_current++) * 0.5f;
						//l.brightness = 1.0f - WorldNoisef (_seed_current++) * 0.2f;
						//l.color = glRgbaInterpolate (_leaf_color, glRgba (0.0f, 0.5f, 0.0f), WorldNoisef (_seed_current++) * 0.25f);
						_leaf_list.push_back (l);
					}
				}
			} else if (_leaf_style == TREE_LEAF_SCATTER) {
				float     leaf_size;
				float     nearest;
				float     distance;
				Vector2 delta;
				uint  j;

				//Put one big leaf in the center
				leaf_size = TEXTURE_HALF / 3;
				l.size = leaf_size;
				l.position.x = TEXTURE_HALF;
				l.position.y = TEXTURE_HALF;
				l.angle = 0.0f;
				_leaf_list.push_back (l);
				//now scatter other leaves around
				for (i = 0; i < 50; i++) {
					l.size = leaf_size * 0.5f;//  * (0.5f + WorldNoisef (_seed_current++);
					l.position.x = TEXTURE_HALF + (WorldNoisef (_seed_current++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					l.position.y = TEXTURE_HALF + (WorldNoisef (_seed_current++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					delta = _leaf_list[i].position - Vector3 (TEXTURE_HALF, TEXTURE_HALF);
					l.dist = delta.Length ();
					//Leaves get smaller as we move from the center of the texture
					l.size = (0.25f + ((TEXTURE_HALF - l.dist) / TEXTURE_HALF) * 0.75f) * leaf_size; 
					l.angle = 0.0f;
					//l.brightness = 0.7f + ((float)i / 50) * 0.3f;
					//l.color = 
					_leaf_list.push_back (l);
				}
				//Sort our list of leaves, inward out
				qsort (&_leaf_list[0], _leaf_list.size (), sizeof (Leaf), sort_leaves);
				//now look at each leaf and figure out its closest neighbor
				for (i = 0; i < _leaf_list.size (); i++) {
					_leaf_list[i].neighbor = 0;
					delta = _leaf_list[i].position - _leaf_list[0].position;
					nearest = delta.Length ();
					for (j = 1; j < i; j++) {
						//Don't connect this leaf to itself!
						if (j == i)
							continue;
						delta = _leaf_list[i].position - _leaf_list[j].position;
						distance = delta.Length ();
						if (distance < nearest) {
							_leaf_list[i].neighbor = j;
							nearest = distance;
						}      
					}
				}
				//Now we have the leaves, and we know their neighbors
				//Get the angles between them
				for (i = 1; i < _leaf_list.size (); i++) {
					j = _leaf_list[i].neighbor;
					_leaf_list[i].angle = -MathAngle (_leaf_list[j].position.x, _leaf_list[j].position.y, _leaf_list[i].position.x, _leaf_list[i].position.y);
				}
			}
			for (i = 0; i < _leaf_list.size (); i++) 
				_leaf_list[i].color = glRgbaInterpolate (_leaf_color, glRgba (0.0f, 0.5f, 0.0f), WorldNoisef (_seed_current++) * 0.33f);

		}

		void DoTexture ()
		{

			uint  i;

			GL.Disable (GL_CULL_FACE);
			GL.Disable (GL_FOG);
			GL.Disable (GL_LIGHTING);
			glEnable (EnableCap.Blend);
			glEnable (TextureTarget.Texture2D);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
 			GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMFilter.Nearest);	
			GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMFilter.Nearest);	
			if (_texture)
				glDeleteTextures (1, &_texture); 
			glGenTextures (1, &_texture); 
			GL.BindTexture(TextureTarget.Texture2D, _texture);
			glTexImage2D (TextureTarget.Texture2D, 0, GL_RGBA, TEXTURE_SIZE * 4, TEXTURE_SIZE, 0, GL_RGBA, GL_uint_BYTE, NULL);
			RenderCanvasBegin (0, TEXTURE_SIZE, 0, TEXTURE_SIZE, TEXTURE_SIZE);
 			GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMFilter.Nearest);	
			GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMFilter.Nearest);	
			char* buffer = new char[TEXTURE_SIZE * TEXTURE_SIZE * 4];
			for (i = 0; i < 4; i++) {
				glClearColor (1.0f, 0.0f, 1.0f, 0.0f);
				glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
				if (i == 0)       
					DrawBark ();
				else if (i == 1)
					DrawLeaves ();
				else if (i == 2)
					DrawVines ();
				else
					DrawFacer ();    
				//CgShaderSelect (FSHADER_MASK_TRANSFER);
				GL.BindTexture(TextureTarget.Texture2D, _texture);
 				GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMFilter.Nearest);	
				GL.TexParameter (TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMFilter.Nearest);	
				//glCopyTexSubImage2D (TextureTarget.Texture2D, 0, TEXTURE_SIZE * i, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE);
				glReadPixels (0, 0, TEXTURE_SIZE, TEXTURE_SIZE, GL_RGBA, GL_uint_BYTE, buffer);
				//CgShaderSelect (FSHADER_MASK_TRANSFER);
				glTexSubImage2D (TextureTarget.Texture2D, 0, TEXTURE_SIZE * i, 0, TEXTURE_SIZE, TEXTURE_SIZE, GL_RGBA, GL_uint_BYTE, buffer);
				//CgShaderSelect (FSHADER_NONE);
			}
			delete buffer;
			RenderCanvasEnd ();
  
		}

		void Info ()
		{

			TextPrint ("TREE:\nSeed:%d Moisture: %f Temp: %f", _seed, _moisture, _temperature);

		}

		void TexturePurge ()
		{

			if (_texture)
				DoTexture ();

		}*/
	}
}