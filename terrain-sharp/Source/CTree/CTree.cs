namespace terrain_sharp.Source.CTree {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.OpenGL;

	using Extensions;
	using StdAfx;
	using Utils;
	using GLTypes;

	class CTree {
		const int TREE_ALTS = 3;
		const float SEGMENTS_PER_METER = 0.25f;
		const int MIN_SEGMENTS = 3;
		const int TEXTURE_SIZE = 256;
		const int TEXTURE_HALF = (TEXTURE_SIZE / 2);
		const float MIN_RADIUS = 0.3f;
		static readonly Vector3 UP = new Vector3(0f, 0f, 1f);

		private enum TreeTrunkStyle {
			Normal, Jagged, Bent
		}

		private enum TreeFoliageStyle {
			Umbrella, Bowl, Shield, Panel, Sag
		}

		private enum TreeLiftStyle {
			Straight, In, Out
		}

		private enum TreeLeafStyle {
			Fan, Scatter
		}

		TreeTrunkStyle _trunk_style;
		TreeFoliageStyle _foliage_style;
		TreeLiftStyle _lift_style;
		TreeLeafStyle _leaf_style;

		int _seed;
		int _seed_current;
		bool _funnel_trunk;
		bool _evergreen;
		bool _canopy;
		bool _has_vines;
		public bool GrowsHigh { get; private set; }
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
		Color4 _bark_color1;
		Color4 _bark_color2;
		Color4 _leaf_color;
		List<Leaf> _leaf_list;
		Mesh[,] _meshes = new Mesh[TREE_ALTS, Enum.GetValues(typeof(LOD)).Length];

		private void DrawBark() {
			GL.Color4(_bark_color1);
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
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1f / frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			var uvframe = new UvBox();
			uvframe.Set(new Vector2(0, frame * frame_size), new Vector2(1, (frame + 1) * frame_size));
			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ColorMask(true, true, true, false);
			GL.Color4(_bark_color2);
			GL.Begin(PrimitiveType.Quads);
			Vector2 uv = uvframe.Corner(UvBoxPosition.TopLeft);
			GL.TexCoord2(uv);
			GL.Vertex2(0, 0);
			uv = uvframe.Corner(UvBoxPosition.TopRight);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(UvBoxPosition.BottomRight);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(UvBoxPosition.BottomLeft);
			GL.TexCoord2(uv);
			GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();
			GL.ColorMask(true, true, true, true);
		}

		private void DrawLeaves() {
			if (_leaf_style == TreeLeafStyle.Scatter) {
				Color4 color = _bark_color1.Scale(0.5f);
				GL.BindTexture(TextureTarget.Texture2D, 0);
				GL.LineWidth(3);
				GL.Color4(color);

				GL.Begin(PrimitiveType.Lines);
				foreach (var leaf in _leaf_list) {
					GL.Vertex2(_leaf_list[leaf.neighbor].position);
					GL.Vertex2(leaf.position);
				}
				GL.End();
			}

			GLtexture t = TextureFromName("foliage.png");
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1 / (float) frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			var uvframe = new UvBox();
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
				GL.Translate(l.position.X, l.position.Y, 0);
				GL.Rotate(l.angle, 0, 0, 1);
				GL.Translate(-l.position.X, -l.position.Y, 0);

				//Color4 color = _leaf_color * l.brightness;
				GL.Color4(l.color);
				GL.Begin(PrimitiveType.Quads);
				Vector2 uv = uvframe.Corner(UvBoxPosition.TopLeft);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X - l.size, l.position.Y - l.size);
				uv = uvframe.Corner(UvBoxPosition.TopRight);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X + l.size, l.position.Y - l.size);
				uv = uvframe.Corner(UvBoxPosition.BottomRight);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X + l.size, l.position.Y + l.size);
				uv = uvframe.Corner(UvBoxPosition.BottomLeft);
				GL.TexCoord2(uv);
				GL.Vertex2(l.position.X - l.size, l.position.Y + l.size);
				GL.End();
				GL.PopMatrix();
			}
		}

		private void DrawVines() {
			GL.Color4(_bark_color1);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GLtexture t = TextureFromName("vines.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1 / (float) frames;
			int frame = WorldNoisei(_seed_current++) % frames;
			var uvframe = new UvBox();
			uvframe.Set(new Vector2(0f, frame * frame_size), new Vector2(1f, (frame + 1) * frame_size));
			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			Color4 color = _leaf_color.Scale(0.75f);
			GL.Color4(_leaf_color);
			GL.Begin(PrimitiveType.Quads);
			Vector2 uv = uvframe.Corner(UvBoxPosition.BottomLeft);
			GL.TexCoord2(uv);
			GL.Vertex2(0, 0);
			uv = uvframe.Corner(UvBoxPosition.TopLeft);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(UvBoxPosition.TopRight);
			GL.TexCoord2(uv);
			GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(UvBoxPosition.BottomRight);
			GL.TexCoord2(uv);
			GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();
		}

		private void DrawFacer() {
			GLbbox box;
			Vector3 size, center;

			GL.Disable(EnableCap.Blend);

			//We get the bounding box for the high-res tree, but we cut off the roots.  No reason to 
			//waste texture pixels on that.
			_meshes[0, (int) LOD.High].RecalculateBoundingBox();
			box = _meshes[0, (int) LOD.High]._bbox;
			box.pmin.Z = 0;  //Cuts off roots
			center = box.Center();
			size = box.Count;

			//Move our viewpoint to the middle of the texture frame 
			GL.Translate(TEXTURE_HALF, TEXTURE_HALF, 0);
			GL.Rotate(-90, 1, 0, 0);

			//Scale so that the tree will exactly fill the rectangle
			GL.Scale((1 / size.X) * TEXTURE_SIZE, 1, (1 / size.Z) * TEXTURE_SIZE);
			GL.Translate(-center.X, 0, -center.Z);
			GL.Color3(1, 1, 1);
			Render(new Vector3(), 0, LOD.High);
		}

		private void DoVines(Mesh m, List<Vector3> points) {
			if (!_has_vines)
				return;
			int base_index = m._vertex.Count;
			for (int segment = 0; segment < points.Count; segment++) {
				float v = segment;
				m.PushVertex(points[segment], UP, new Vector2(0.75f, v));
				m.PushVertex(Vector3.Add(points[segment], new Vector3(0, 0, -3.5f)), UP, new Vector2(0.5f, v));
			}
			for (int segment = 0; segment < points.Count - 1; segment++) {
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

		private void DoFoliage(Mesh m, Vector3 pos, float fsize, float angle) {
			fsize *= _foliage_size;
			var uv = new UvBox();
			uv.Set(new Vector2(0.25f, 0), new Vector2(0.5f, 1));
			int base_index = m._vertex.Count;

			//don't let the foliage get so big it touches the ground.
			fsize = Math.Min(pos.Z - 2.0f, fsize);
			if (fsize < 0.1f)
				return;
			if (_foliage_style == TreeFoliageStyle.Panel) {
				m.PushVertex(new Vector3(-0, -fsize, -fsize), UP, uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(-1, fsize, -fsize), UP, uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(-1, fsize, fsize), UP, uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(-0, -fsize, fsize), UP, uv.Corner(UvBoxPosition.BottomLeft));

				m.PushVertex(new Vector3(0, -fsize, -fsize), UP, uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(1, fsize, -fsize), UP, uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(1, fsize, fsize), UP, uv.Corner(UvBoxPosition.BottomLeft));
				m.PushVertex(new Vector3(0, -fsize, fsize), UP, uv.Corner(UvBoxPosition.TopLeft));

				m.PushQuad(base_index + 0, base_index + 1, base_index + 2, base_index + 3);
				m.PushQuad(base_index + 7, base_index + 6, base_index + 5, base_index + 4);

			} else if (_foliage_style == TreeFoliageStyle.Shield) {
				m.PushVertex(new Vector3(fsize / 2, 0, 0), UP, uv.Center);
				m.PushVertex(new Vector3(0, -fsize, 0), UP, uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(0, 0, fsize), UP, uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(0, fsize, 0), UP, uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(0, 0, -fsize), UP, uv.Corner(UvBoxPosition.BottomLeft));
				m.PushVertex(new Vector3(-fsize / 2, 0, 0), UP, uv.Center);
				//Cap
				m.PushTriangle(base_index, base_index + 1, base_index + 2);
				m.PushTriangle(base_index, base_index + 2, base_index + 3);
				m.PushTriangle(base_index, base_index + 3, base_index + 4);
				m.PushTriangle(base_index, base_index + 4, base_index + 1);
				m.PushTriangle(base_index + 5, base_index + 2, base_index + 1);
				m.PushTriangle(base_index + 5, base_index + 3, base_index + 2);
				m.PushTriangle(base_index + 5, base_index + 4, base_index + 3);
				m.PushTriangle(base_index + 5, base_index + 1, base_index + 4);
			} else if (_foliage_style == TreeFoliageStyle.Sag) {
				/*     /\
							/__\
						 /|  |\
						 \|__|/
							\  /
							 \/   */
				float level1 = fsize * -0.4f;
				float level2 = fsize * -1.2f;

				var uv_inner = new UvBox();
				uv_inner.Set(new Vector2(0.25f + 1.25f, 0.125f), new Vector2(0.5f - 0.125f, 1 - 0.125f));

				//Center
				m.PushVertex(new Vector3(), UP, uv.Center);

				//First ring
				m.PushVertex(new Vector3(-fsize / 2, -fsize / 2, level1), UP, uv.Corner(UvBoxPosition.TopEdge));//1
				m.PushVertex(new Vector3(fsize / 2, -fsize / 2, level1), UP, uv.Corner(UvBoxPosition.RightEdge));//2
				m.PushVertex(new Vector3(fsize / 2, fsize / 2, level1), UP, uv.Corner(UvBoxPosition.BottomEdge));//3
				m.PushVertex(new Vector3(-fsize / 2, fsize / 2, level1), UP, uv.Corner(UvBoxPosition.LeftEdge));//4

				//Tips
				m.PushVertex(new Vector3(0, -fsize, level2), UP, uv.Corner(UvBoxPosition.TopRight));//5
				m.PushVertex(new Vector3(fsize, 0, level2), UP, uv.Corner(UvBoxPosition.BottomRight));//6
				m.PushVertex(new Vector3(0, fsize, level2), UP, uv.Corner(UvBoxPosition.BottomLeft));//7
				m.PushVertex(new Vector3(-fsize, 0, level2), UP, uv.Corner(UvBoxPosition.TopLeft));//8
																																					//Center, but lower
				m.PushVertex(new Vector3(0, 0, level1 / 16), UP, uv.Center);

				//Cap
				m.PushTriangle(base_index, base_index + 2, base_index + 1);
				m.PushTriangle(base_index, base_index + 3, base_index + 2);
				m.PushTriangle(base_index, base_index + 4, base_index + 3);
				m.PushTriangle(base_index, base_index + 1, base_index + 4);
				//Outer triangles
				m.PushTriangle(base_index + 5, base_index + 1, base_index + 2);
				m.PushTriangle(base_index + 6, base_index + 2, base_index + 3);
				m.PushTriangle(base_index + 7, base_index + 3, base_index + 4);
				m.PushTriangle(base_index + 8, base_index + 4, base_index + 1);
			} else if (_foliage_style == TreeFoliageStyle.Bowl) {
				float tip_height;

				tip_height = fsize / 4.0f;
				if (_foliage_style == TreeFoliageStyle.Bowl)
					tip_height *= -1;
				m.PushVertex(new Vector3(0, 0, tip_height), new Vector3(0, 0, 1), uv.Center);
				m.PushVertex(new Vector3(-fsize, -fsize, -tip_height), new Vector3(-0.5f, -0.5f, 0), uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(fsize, -fsize, -tip_height), new Vector3(0.5f, -0.5f, 0), uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(fsize, fsize, -tip_height), new Vector3(0.5f, 0.5f, 0), uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(-fsize, fsize, -tip_height), new Vector3(-0.5f, 0.5f, 0), uv.Corner(UvBoxPosition.BottomLeft));
				m.PushVertex(new Vector3(0, 0, tip_height / 2), new Vector3(0, 0, 1), uv.Center);
				m.PushTriangle(base_index, base_index + 1, base_index + 2);
				m.PushTriangle(base_index, base_index + 2, base_index + 3);
				m.PushTriangle(base_index, base_index + 3, base_index + 4);
				m.PushTriangle(base_index, base_index + 4, base_index + 1);

				m.PushTriangle(base_index + 5, base_index + 2, base_index + 1);
				m.PushTriangle(base_index + 5, base_index + 3, base_index + 2);
				m.PushTriangle(base_index + 5, base_index + 4, base_index + 3);
				m.PushTriangle(base_index + 5, base_index + 1, base_index + 4);

				//m.PushQuad (base_index + 1, base_index + 4, base_index + 3, base_index + 2);
			} else if (_foliage_style == TreeFoliageStyle.Umbrella) {
				float tip_height;

				tip_height = fsize / 4.0f;
				m.PushVertex(new Vector3(0, 0, tip_height), new Vector3(0, 0, 1), uv.Center);
				m.PushVertex(new Vector3(-fsize, -fsize, -tip_height), new Vector3(-0.5f, -0.5f, 0), uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(fsize, -fsize, -tip_height), new Vector3(0.5f, -0.5f, 0), uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(fsize, fsize, -tip_height), new Vector3(0.5f, 0.5f, 0), uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(-fsize, fsize, -tip_height), new Vector3(-0.5f, 0.5f, 0), uv.Corner(UvBoxPosition.BottomLeft));
				m.PushVertex(new Vector3(0, 0, tip_height / 2), new Vector3(0, 0, 1), uv.Center);
				//Top
				m.PushTriangle(base_index, base_index + 2, base_index + 1);
				m.PushTriangle(base_index, base_index + 3, base_index + 2);
				m.PushTriangle(base_index, base_index + 4, base_index + 3);
				m.PushTriangle(base_index, base_index + 1, base_index + 4);
			}
			GLmatrix mat;
			//angle = MathUtils.MathAngle(pos.X, pos.Y, 0, 0);
			//angle += 45.0f;
			mat.Identity();
			mat.Rotate(angle, 0, 0, 1);
			for (int i = base_index; i < m._vertex.Count; i++) {
				m._vertex[i] = glMatrixTransformPoint(mat, m._vertex[i]);
				m._vertex[i] += pos;
			}
		}

		private void DoBranch(Mesh m, BranchAnchor anchor, float branch_angle, LOD lod) {
			if (anchor.length < 2.0f)
				return;
			if (anchor.radius < MIN_RADIUS)
				return;
			int segment_count = (int) (anchor.length * SEGMENTS_PER_METER);
			segment_count = Math.Max(segment_count, MIN_SEGMENTS);
			segment_count += 3;
			int base_index = m._vertex.Count;
			GLmatrix mat = new GLmatrix();
			mat.Identity();
			mat.Rotate(branch_angle, 0, 0, 1);
			int radial_steps;
			switch (lod) {
				case LOD.Low:
					segment_count = 2;
					radial_steps = 2;
					break;
				case LOD.Med:
					radial_steps = 2;
					segment_count = 3;
					break;
				default:
					segment_count = 5;
					radial_steps = 6;
					break;
			}
			int radial_edge = radial_steps + 1;
			Vector3 core = anchor.root;
			float radius = anchor.radius;
			var underside = new List<Vector3>();
			var pos = new Vector3();
			for (int segment = 0; segment <= segment_count; segment++) {
				float horz_pos = segment / (float) (segment_count + 1);
				float curve;
				if (_lift_style == TreeLiftStyle.Out)
					curve = horz_pos * horz_pos;
				else if (_lift_style == TreeLiftStyle.In) {
					curve = 1 - horz_pos;
					curve *= curve * curve;
					curve = 1 - curve;
				} else //Straight
					curve = horz_pos;
				radius = Math.Max(MIN_RADIUS, anchor.radius * (1 - horz_pos));
				core.Z = anchor.root.Z + anchor.lift * curve * _branch_lift;
				//if this is the last segment, don't make a ring of points. Make ONE, in the center.
				//This is so the branch can end at a point.
				if (segment == segment_count) {
					pos = new Vector3(0, anchor.length * horz_pos, 0);
					pos = glMatrixTransformPoint(mat, pos);
					m.PushVertex(pos + core, new Vector3(pos.X, 0, pos.Z), new Vector2(0.249f, pos.Y * _texture_tile));
				} else {
					for (int ring = 0; ring <= radial_steps; ring++) {
						//Make sure the final edge perfectly matches the starting one. Can't leave
						//this to floating-point math.
						float angle;
						if (ring == radial_steps || ring == 0)
							angle = 0;
						else
							angle = (float) MathUtils.DegreeToRadian(ring * (360f / radial_steps));
						pos = new Vector3(
							(float) (-Math.Sin(angle) * radius),
							anchor.length * horz_pos,
							(float) (-Math.Cos(angle) * radius));
						pos = glMatrixTransformPoint(mat, pos);
						m.PushVertex(
							pos + core,
							new Vector3(pos.X, 0, pos.Z),
							new Vector2(((float) ring / radial_steps) * 0.249f, pos.Y * _texture_tile));
					}
				}
				underside.Add(pos + core);
			}
			//Make the triangles for the branch
			for (int segment = 0; segment < segment_count; segment++) {
				for (int ring = 0; ring < radial_steps; ring++) {
					if (segment < segment_count - 1) {
						m.PushQuad(base_index + (ring + 0) + (segment + 0) * (radial_edge),
							base_index + (ring + 0) + (segment + 1) * (radial_edge),
							base_index + (ring + 1) + (segment + 1) * (radial_edge),
							base_index + (ring + 1) + (segment + 0) * (radial_edge));
					} else {//this is the last segment. It ends in a single point
						m.PushTriangle(
							base_index + (ring + 1) + segment * (radial_edge),
							base_index + (ring + 0) + segment * (radial_edge),
							m._vertex.Count - 1);
					}
				}
			}
			//Grab the last point and use it as the origin for the foliage
			pos = m._vertex[m._vertex.Count - 1];
			DoFoliage(m, pos, anchor.length * 0.56f, branch_angle);
			//We saved the points on the underside of the branch.
			//Use these to hang vines on the branch
			if (lod == LOD.High)
				DoVines(m, underside);
		}

		private void DoTrunk(Mesh m, LOD lod) {
			//Determine the branch locations
			float branch_spacing = (0.95f - _current_lowest_branch) / _current_branches;
			var branch_list = new List<BranchAnchor>();
			for (int i = 0; i < _current_branches; i++) {
				float vertical_pos = _current_lowest_branch + branch_spacing * i;
				BranchAnchor branch = new BranchAnchor();
				branch.root = TrunkPosition(vertical_pos, out branch.radius);
				branch.length = (_current_height - branch.root.Z) * _branch_reach;
				branch.length = Math.Min(branch.length, _current_height / 2);
				branch.lift = (branch.length) / 2;
				branch_list.Add(branch);
			}

			//Just make a 2-panel facer
			if (lod == LOD.Low) {
				//Use the fourth frame of our texture
				var uv = new UvBox();
				uv.Set(new Vector2(0.75f, 0), new Vector2(1, 1));
				float height = _current_height;
				float width = _current_height / 2.0f;

				//First panel
				m.PushVertex(new Vector3(-width, -width, 0), new Vector3(-width, -width, 0), uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(width, width, 0), new Vector3(width, width, 0), uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(width, width, height), new Vector3(width, width, height), uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(-width, -width, height), new Vector3(-width, -width, height), uv.Corner(UvBoxPosition.BottomLeft));

				//Second Panel
				m.PushVertex(new Vector3(-width, width, 0), new Vector3(-width, width, 0), uv.Corner(UvBoxPosition.TopLeft));
				m.PushVertex(new Vector3(width, -width, 0), new Vector3(width, -width, 0), uv.Corner(UvBoxPosition.TopRight));
				m.PushVertex(new Vector3(width, -width, height), new Vector3(width, -width, height), uv.Corner(UvBoxPosition.BottomRight));
				m.PushVertex(new Vector3(-width, width, height), new Vector3(-width, width, height), uv.Corner(UvBoxPosition.BottomLeft));

				for (int i = 0; i < (int) m._normal.Count; i++)
					m._normal[i].Normalize();
				m.PushQuad(0, 1, 2, 3);
				m.PushQuad(4, 5, 6, 7);
				return;
			}

			//Work out the circumference of the BASE of the tree
			float circumference = _current_base_radius * _current_base_radius * (float) Math.PI;
			//The texture will repeat ONCE horizontally around the tree.  Set the vertical to repeat in the same distance.
			_texture_tile = 1;//(float)((int)circumference + 0.5f); 
			int radial_steps = 3;
			if (lod == LOD.High)
				radial_steps = 7;
			int radial_edge = radial_steps + 1;
			int segment_count = 0;

			//Work our way up the tree, building rings of verts
			float radius = 0;
			Vector3 core;
			float angle;
			for (int i = -1; i < branch_list.Count; i++) {
				if (i < 0) { //-1 is the bottom rung, the root. Put it underground, widen it a bit
					core = TrunkPosition(0, out radius);
					radius *= 1.5f;
					core.Z -= 2.0f;
				} else {
					core = branch_list[i].root;
					radius = branch_list[i].radius;
				}
				for (int ring = 0; ring <= radial_steps; ring++) {
					//Make sure the final edge perfectly matches the starting one. Can't leave
					//this to floating-point math.
					if (ring == radial_steps || ring == 0)
						angle = 0;
					else
						angle = (float) MathUtils.DegreeToRadian(ring * (360f / radial_steps));
					float x = (float) Math.Sin(angle);
					float y = (float) Math.Cos(angle);
					m.PushVertex(core + new Vector3(x * radius, y * radius, 0),
						new Vector3(x, y, 0),
						new Vector2(((float) ring / radial_steps) * 0.249f, core.Z * _texture_tile));
				}
				segment_count++;
			}

			//Push one more point, for the very tip of the tree
			float dummyRadius;
			m.PushVertex(TrunkPosition(1, out dummyRadius), new Vector3(0, 0, 1), new Vector2(0, 0));
			//Make the triangles for the main trunk.
			for (int segment = 0; segment < segment_count - 1; segment++) {
				for (int ring = 0; ring < radial_steps; ring++) {
					m.PushQuad((ring + 0) + (segment + 0) * (radial_edge),
						(ring + 1) + (segment + 0) * (radial_edge),
						(ring + 1) + (segment + 1) * (radial_edge),
						(ring + 0) + (segment + 1) * (radial_edge));
				}
			}

			//Make the triangles for the tip
			for (int ring = 0; ring < radial_steps; ring++) {
				m.PushTriangle((ring + 1) + (segment_count - 1) * radial_edge, m._vertex.Count - 1,
					(ring + 0) + (segment_count - 1) * radial_edge);
			}
			DoFoliage(m, m._vertex[m._vertex.Count - 1] + new Vector3(0, 0, -0), _current_height / 2, 0);
			//if (!_canopy) {
			//DoFoliage (TrunkPosition (vertical_pos, NULL), vertical_pos * _height, 0);
			if (_evergreen) { //just rings of foliage, like an evergreen
				for (int i = 0; i < branch_list.Count; i++) {
					angle = i * ((360f / branch_list.Count));
					DoFoliage(m, branch_list[i].root, branch_list[i].length, angle);
				}
			} else { //has branches
				for (int i = 0; i < branch_list.Count; i++) {
					angle = _current_angle_offset + i * ((360f / branch_list.Count) + 180);
					DoBranch(m, branch_list[i], angle, lod);
				}
			}
			//}
		}

		private void DoLeaves() {
			if (_leaf_style == TreeLeafStyle.Fan) {
				int total_steps = 5;
				float current_steps = total_steps;
				for (current_steps = total_steps; current_steps >= 1; current_steps -= 1) {
					float size = (TEXTURE_HALF / 2) / (1 + ((float) total_steps - current_steps));
					float radius = (TEXTURE_HALF - size * 2.0f);
					float circ = (float) Math.PI * radius * 2;
					float step_size = 360f / current_steps;
					for (float x = 0; x < 360f; x += step_size) {
						float rad = (float) MathUtils.DegreeToRadian(x);
						var l = new Leaf();
						l.size = size;
						l.position.X = (float) (TEXTURE_HALF + Math.Sin(rad) * l.size);
						l.position.Y = (float) (TEXTURE_HALF + Math.Cos(rad) * l.size);
						l.angle = -MathUtils.MathAngle(TEXTURE_HALF, TEXTURE_HALF, l.position.X, l.position.Y);
						//l.brightness = 1 - (current_steps / (float)total_steps) * WorldNoisef (_seed_current++) * 0.5f;
						//l.brightness = 1 - WorldNoisef (_seed_current++) * 0.2f;
						//l.color = Color4Utils.Interpolate(_leaf_color, new Color4(0, 0.5f, 0), WorldNoisef(_seed_current++) * 0.25f);
						_leaf_list.Add(l);
					}
				}
			} else if (_leaf_style == TreeLeafStyle.Scatter) {
				//Put one big leaf in the center
				float leaf_size = TEXTURE_HALF / 3;
				var l = new Leaf();
				l.size = leaf_size;
				l.position.X = TEXTURE_HALF;
				l.position.Y = TEXTURE_HALF;
				l.angle = 0;
				_leaf_list.Add(l);
				//now scatter other leaves around
				for (int i = 0; i < 50; i++) {
					l.size = leaf_size * 0.5f;//  * (0.5f + WorldNoisef (_seed_current++);
					l.position.X = TEXTURE_HALF + (WorldNoisef(_seed_current++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					l.position.Y = TEXTURE_HALF + (WorldNoisef(_seed_current++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					Vector2 delta = _leaf_list[i].position - new Vector2(TEXTURE_HALF, TEXTURE_HALF);
					l.dist = delta.Length;
					//Leaves get smaller as we move from the center of the texture
					l.size = (0.25f + ((TEXTURE_HALF - l.dist) / TEXTURE_HALF) * 0.75f) * leaf_size;
					l.angle = 0;
					//l.brightness = 0.7f + ((float)i / 50) * 0.3f;
					//l.color = 
					_leaf_list.Add(l);
				}

				//Sort our list of leaves, inward out
				_leaf_list.OrderBy(leaf => leaf.dist);

				//now look at each leaf and figure out its closest neighbor
				for (int i = 0; i < _leaf_list.Count; i++) {
					_leaf_list[i].neighbor = 0;
					Vector2 delta = _leaf_list[i].position - _leaf_list[0].position;
					float nearest = delta.Length;
					for (int j = 1; j < i; j++) {
						//Don't connect this leaf to itself!
						if (j == i)
							continue;
						delta = _leaf_list[i].position - _leaf_list[j].position;
						float distance = delta.Length;
						if (distance < nearest) {
							_leaf_list[i].neighbor = j;
							nearest = distance;
						}
					}
				}

				//Now we have the leaves, and we know their neighbors
				//Get the angles between them
				for (int i = 1; i < _leaf_list.Count; i++) {
					int j = _leaf_list[i].neighbor;
					_leaf_list[i].angle = -MathUtils.MathAngle(_leaf_list[j].position.X, _leaf_list[j].position.Y, _leaf_list[i].position.X, _leaf_list[i].position.Y);
				}
			}
			for (int i = 0; i < _leaf_list.Count; i++)
				_leaf_list[i].color = Color4Utils.Interpolate(_leaf_color, new Color4(0, 0.5f, 0, 1), WorldNoisef(_seed_current++) * 0.33f);
		}

		private void DoTexture() {
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Fog);
			GL.Disable(EnableCap.Lighting);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			if (Texture > 0)
				GL.DeleteTextures(1, new uint[] { Texture });
			GL.GenTextures(1, new uint[] { Texture });
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEXTURE_SIZE * 4, TEXTURE_SIZE,
				0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			RenderCanvasBegin(0, TEXTURE_SIZE, 0, TEXTURE_SIZE, TEXTURE_SIZE);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMFilter.Nearest);
			Byte[] buffer = new Byte[TEXTURE_SIZE * TEXTURE_SIZE * 4];
			for (int i = 0; i < 4; i++) {
				GL.ClearColor(1, 0, 1, 0);
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				if (i == 0)
					DrawBark();
				else if (i == 1)
					DrawLeaves();
				else if (i == 2)
					DrawVines();
				else
					DrawFacer();
				//CgShaderSelect (FSHADER_MASK_TRANSFER);
				GL.BindTexture(TextureTarget.Texture2D, Texture);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
				//GL.CopyTexSubImage2D (TextureTarget.Texture2D, 0, TEXTURE_SIZE * i, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE);
				GL.ReadPixels(0, 0, TEXTURE_SIZE, TEXTURE_SIZE, PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
				//CgShaderSelect (FSHADER_MASK_TRANSFER);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, TEXTURE_SIZE * i, 0, TEXTURE_SIZE, TEXTURE_SIZE,
					PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
				//CgShaderSelect (FSHADER_NONE);
			}
			RenderCanvasEnd();
		}

		//Given the value of 0 (root) to 1 (top), return the center of the trunk 
		//at that height.
		private Vector3 TrunkPosition(float delta, out float radius_in) {
			float delta_curve;
			if (_funnel_trunk) {
				delta_curve = 1 - delta;
				delta_curve *= delta_curve;
				delta_curve = 1 - delta_curve;
			} else
				delta_curve = delta;
			float radius;
			if (_canopy) //canopy trees are thick all the way up, do not taper to a point
				radius = _current_base_radius * (1 - delta_curve / 2);
			else
				radius = _current_base_radius * (1 - delta_curve);

			radius = Math.Max(radius, MIN_RADIUS);
			float bend = delta * delta;
			Vector3 trunk = new Vector3();
			switch (_trunk_style) {
				case TreeTrunkStyle.Bent:
					trunk.X = bend * _current_height / 3.0f;
					trunk.Y = 0;
					break;
				case TreeTrunkStyle.Jagged:
					trunk.X = bend * _current_height / 2.0f;
					trunk.Y = (float) (Math.Sin(delta * _current_bend_frequency) * _current_height / 3);
					break;
				case TreeTrunkStyle.Normal:
				default:
					trunk.X = 0;
					trunk.Y = 0;
					break;
			}
			trunk.Z = delta * _current_height;
			radius_in = radius;
			return trunk;
		}

		private void Build() {
			//_branches = 3 + WorldNoisei (_seed_current++) % 3;
			//_trunk_bend_frequency = 3.0f + WorldNoisef (_seed_current++) * 4.0f;
			_seed_current = _seed;
			for (int alt = 0; alt < TREE_ALTS; alt++) {
				_current_angle_offset = WorldNoisef(_seed_current++) * 360f;
				_current_height = _default_height * (0.5f + WorldNoisef(_seed_current++));
				_current_base_radius = _default_base_radius * (0.5f + WorldNoisef(_seed_current++));
				_current_branches = _default_branches + WorldNoisei(_seed_current++) % 3;
				_current_bend_frequency = _default_bend_frequency + WorldNoisef(_seed_current++);
				_current_lowest_branch = _default_lowest_branch + WorldNoisef(_seed_current++) * 0.2f;
				foreach (LOD lod in Enum.GetValues(typeof(LOD))) {
					_meshes[alt, (int) lod].Clear();
					DoTrunk(_meshes[alt, (int) lod], lod);
					//The facers use hand-made normals, so don't recalculate them.
					if (lod != LOD.Low)
						_meshes[alt, (int) lod].CalculateNormalsSeamless();
				}
			}
		}

		public void Create(bool is_canopy, float moisture, float temp_in, int seed_in) {
			//Prepare, clear the tables, etc.
			_leaf_list.Clear();
			_seed = seed_in;
			_seed_current = _seed;
			_moisture = moisture;
			_canopy = is_canopy;
			_temperature = temp_in;
			_seed_current = _seed;
			//We want our height to fall on a bell curve
			_default_height = 8.0f + WorldNoisef(_seed_current++) * 4.0f + WorldNoisef(_seed_current++) * 4.0f;
			_default_bend_frequency = 1 + WorldNoisef(_seed_current++) * 2.0f;
			_default_base_radius = 0.2f + (_default_height / 20.0f) * WorldNoisef(_seed_current++);
			_default_branches = 2 + WorldNoisei(_seed_current) % 2;
			//Keep branches away from the ground, since they don't have collision
			_default_lowest_branch = (3.0f / _default_height);
			//Funnel trunk trees taper off quickly at the base.
			_funnel_trunk = (WorldNoisei(_seed_current++) % 6) == 0;
			if (_funnel_trunk) {//Funnel trees need to be bigger and taller to look right
				_default_base_radius *= 1.2f;
				_default_height *= 1.5f;
			}
			_trunk_style = (TreeTrunkStyle) (WorldNoisei(_seed_current) % Enum.GetNames(typeof(TreeTrunkStyle)).Count);
			_foliage_style = (TreeFoliageStyle) (WorldNoisei(_seed_current++) % Enum.GetNames(typeof(TreeFoliageStyle)).Count);
			_lift_style = (TreeLiftStyle) (WorldNoisei(_seed_current++) % Enum.GetNames(typeof(TreeLiftStyle)).Count);
			_leaf_style = (TreeLeafStyle) (WorldNoisei(_seed_current++) % Enum.GetNames(typeof(TreeLeafStyle)).Count);
			_evergreen = _temperature + (WorldNoisef(_seed_current++) * 0.25f) < 0.5f;
			_has_vines = _moisture > 0.6f && _temperature > 0.5f;
			//Narrow trees can gorw on top of hills. (Big ones will stick out over cliffs, so we place them low.)
			GrowsHigh = (_default_base_radius <= 1);
			_branch_reach = 1 + WorldNoisef(_seed_current++) * 0.5f;
			_branch_lift = 1 + WorldNoisef(_seed_current++);
			_foliage_size = 1;
			_leaf_size = 0.125f;
			_leaf_color = TerraformColorGenerate(SurfaceColor.Grass, moisture, _temperature, _seed_current++);
			_bark_color2 = TerraformColorGenerate(SurfaceColor.Dirt, moisture, _temperature, _seed_current++);
			_bark_color1 = _bark_color2.Scale(0.5f);
			//1 in 8 non-tropical trees has white bark
			if (!_has_vines && !(WorldNoisei(_seed_current++) % 8))
				_bark_color2 = Color4Utils.FromLuminance(1);
			//These foliage styles don't look right on evergreens.
			if (_evergreen && _foliage_style == TreeFoliageStyle.Bowl)
				_foliage_style = TreeFoliageStyle.Umbrella;
			if (_evergreen && _foliage_style == TreeFoliageStyle.Shield)
				_foliage_style = TreeFoliageStyle.Umbrella;
			if (_evergreen && _foliage_style == TreeFoliageStyle.Panel)
				_foliage_style = TreeFoliageStyle.Sag;
			if (_canopy) {
				_foliage_style = TreeFoliageStyle.Umbrella;
				_default_height = Math.Max(_default_height, 16.0f);
				_default_base_radius = 1.5f;
				_foliage_size = 2.0f;
				_trunk_style = TreeTrunkStyle.Normal;
			}
			Build();
			DoLeaves();
			DoTexture();
		}

		//Render a single tree. Very slow. Used for debugging. 
		public void Render(Vector3 pos, uint alt, LOD lod) {
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.PushMatrix();
			GL.Translate(pos.X, pos.Y, pos.Z);
			_meshes[alt, (int) lod].Render();
			GL.PopMatrix();
		}

		public void TexturePurge() {
			if (Texture > 0)
				DoTexture();
		}

		public Mesh Mesh(int alt, LOD lod) {
			return _meshes[alt % TREE_ALTS, (int) lod];
		}

		public void Info() {
			TextPrint("TREE:\nSeed:%d Moisture: %f Temp: %f", _seed, _moisture, _temperature);
		}
	}
}