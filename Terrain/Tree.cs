using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace Frontier {
	class Tree {
		#region Enums and structs
		enum TreeTrunkStyle { Normal, Jagged, Bent }
		enum TreeFoliageStyle { Umbrella, Bowl, Shield, Panel, Sag }
		enum TreeLiftStyle { Straight, In, Out }
		enum TreeLeafStyle { Fan, Scatter }

		struct BranchAnchor {
			public Vector3 root;
			public float radius, length, lift;
		}

		struct Leaf {
			public Vector2     position;
			public float       angle;
			public float       size;
			//public float      brightness;
			public Color4     color;
			public float       dist;
			public int         neighbor;
		}
		#endregion

		#region Member variables and properties
		private const int
			TREE_ALTS				= 3,
			MIN_SEGMENTS		= 3,
			TEXTURE_SIZE		= 256,
			TEXTURE_HALF		= (TEXTURE_SIZE / 2);

		private const float
			SEGMENTS_PER_METER = 0.25f,
			MIN_RADIUS         = 0.3f;

		private TreeTrunkStyle    mTrunkStyle;
		private TreeFoliageStyle  mFoliageStyle;
		private TreeLiftStyle     mLiftStyle;
		private TreeLeafStyle     mLeafStyle;

		private int mSeed, mSeedCurrent;

		private bool mFunnelTrunk, mEvergreen, mCanopy, mHasVines;

		private int mDefaultBranches;
		private float
			mDefaultHeight,
			mDefaultBendFrequency,
			mDefaultBaseRadius,
			mDefaultLowestBranch;

		private int mCurrentBranches;

		private float
			mCurrentHeight,
			mCurrentBendFrequency,
			mCurrentAngleOffset,
			mCurrentBaseRadius,
			mCurrentLowestBranch,

			mMoisture,
			mTemperature,

			mTextureTile,
  
			mBranchLift,
			mBranchReach,
			mFoliageSize,
			mLeafSize;

		private Color4
			mBarkColor1,
			mBarkColor2,
			mLeafColor;

		List<Leaf> mLeafList;
		Mesh[,] mMeshes = new Mesh[TREE_ALTS, LOD_LEVELS];

		public int Texture { get; private set; }
		public bool GrowsHigh { get; private set; }
		#endregion

		#region Private methods
		private static int SortLeaves(Leaf a, Leaf b) {
			if (a.dist < b.dist)
				return -1;
			else if (a.dist > b.dist)
				return 1;
			return 0;
		}

		public Mesh Mesh(int alt, LOD lod) { return mMeshes[alt % TREE_ALTS, (int) lod]; }

		//Given the value of 0.0 (root) to 1.0f (top), return the center of the trunk at that height.
		private Vector3 TrunkPosition(float delta, ref float radius_in) {
			float delta_curve;
			if (mFunnelTrunk) {
				delta_curve = 1.0f - delta;
				delta_curve *= delta_curve;
				delta_curve = 1.0f - delta_curve;
			} else
				delta_curve = delta;

			// Canopy trees are thick all the way Vector3.UnitZ, do not taper to a point
			float radius;
			if (mCanopy)
				radius = mCurrentBaseRadius * (1.0f - delta_curve * 0.5f);
			else
				radius = mCurrentBaseRadius * (1.0f - delta_curve);

			radius = Math.Max(radius, MIN_RADIUS);
			float bend = delta * delta;

			Vector3 trunk;
			switch (mTrunkStyle) {
				case TreeTrunkStyle.Bent:
					trunk.X = bend * mCurrentHeight / 3.0f;
					trunk.Y = 0.0f;
					break;
				case TreeTrunkStyle.Jagged:
					trunk.X = bend * mCurrentHeight / 2.0f;
					trunk.Y = (float) Math.Sin(delta * mCurrentBendFrequency) * mCurrentHeight / 3.0f;
					break;
				case TreeTrunkStyle.Normal:
				default:
					trunk.X = 0.0f;
					trunk.Y = 0.0f;
					break;
			}
			trunk.Z = delta * mCurrentHeight;

			if (radius_in != 0)
				radius_in = radius;
			return trunk;
		}

		private void DoFoliage(Mesh m, Vector3 pos, float fsize, float angle) {
			UVBox uv = new UVBox();

			fsize *= mFoliageSize;
			uv.Set(new Vector2(0.25f, 0.0f), new Vector2(0.5f, 1.0f));
			int base_index = m.vertices.Count;

			// Don't let the foliage get so big it touches the ground.
			fsize = Math.Min(pos.Z - 2.0f, fsize);
			if (fsize < 0.1f)
				return;

			float  tip_height;
			switch (mFoliageStyle) {
				case TreeFoliageStyle.Panel:
					#region Panel style foliage
					m.PushVertex(new Vector3(-0.0f, -fsize, -fsize), Vector3.UnitZ, uv.Corner(0));
					m.PushVertex(new Vector3(-1.0f, fsize, -fsize), Vector3.UnitZ, uv.Corner(1));
					m.PushVertex(new Vector3(-1.0f, fsize, fsize), Vector3.UnitZ, uv.Corner(2));
					m.PushVertex(new Vector3(-0.0f, -fsize, fsize), Vector3.UnitZ, uv.Corner(3));

					m.PushVertex(new Vector3(0.0f, -fsize, -fsize), Vector3.UnitZ, uv.Corner(1));
					m.PushVertex(new Vector3(1.0f, fsize, -fsize), Vector3.UnitZ, uv.Corner(2));
					m.PushVertex(new Vector3(1.0f, fsize, fsize), Vector3.UnitZ, uv.Corner(3));
					m.PushVertex(new Vector3(0.0f, -fsize, fsize), Vector3.UnitZ, uv.Corner(0));

					m.PushQuad(base_index + 0, base_index + 1, base_index + 2, base_index + 3);
					m.PushQuad(base_index + 7, base_index + 6, base_index + 5, base_index + 4);
					break;
					#endregion
				case TreeFoliageStyle.Shield:
					#region Shield style foliage
					m.PushVertex(new Vector3(fsize / 2, 0.0f, 0.0f), Vector3.UnitZ, uv.Center());
					m.PushVertex(new Vector3(0.0f, -fsize, 0.0f), Vector3.UnitZ, uv.Corner(0));
					m.PushVertex(new Vector3(0.0f, 0.0f, fsize), Vector3.UnitZ, uv.Corner(1));
					m.PushVertex(new Vector3(0.0f, fsize, 0.0f), Vector3.UnitZ, uv.Corner(2));
					m.PushVertex(new Vector3(0.0f, 0.0f, -fsize), Vector3.UnitZ, uv.Corner(3));
					m.PushVertex(new Vector3(-fsize / 2, 0.0f, 0.0f), Vector3.UnitZ, uv.Center());
					//Cap
					m.PushTriangle(base_index, base_index + 1, base_index + 2);
					m.PushTriangle(base_index, base_index + 2, base_index + 3);
					m.PushTriangle(base_index, base_index + 3, base_index + 4);
					m.PushTriangle(base_index, base_index + 4, base_index + 1);
					m.PushTriangle(base_index + 5, base_index + 2, base_index + 1);
					m.PushTriangle(base_index + 5, base_index + 3, base_index + 2);
					m.PushTriangle(base_index + 5, base_index + 4, base_index + 3);
					m.PushTriangle(base_index + 5, base_index + 1, base_index + 4);
					break;
					#endregion
				case TreeFoliageStyle.Sag:
					#region Sag style foliage
					/*     /\
					 *    /__\
					 *   /|  |\
					 *   \|__|/
					 *    \  /
					 *     \/   */
					float level1   = fsize * -0.4f;
					float level2   = fsize * -1.2f;
					UVBox   uv_inner;

					uv_inner.Set(new Vector2(0.25f + 1.25f, 0.125f), new Vector2(0.5f - 0.125f, 1.0f - 0.125f));
					// Center
					m.PushVertex(new Vector3(0.0f, 0.0f, 0.0f), Vector3.UnitZ, uv.Center());
					// First ring
					m.PushVertex(new Vector3(-fsize / 2, -fsize / 2, level1), Vector3.UnitZ, uv.Corner(GLUV_TOP_EDGE));		// 1
					m.PushVertex(new Vector3(fsize / 2, -fsize / 2, level1), Vector3.UnitZ, uv.Corner(GLUV_RIGHT_EDGE));	// 2
					m.PushVertex(new Vector3(fsize / 2, fsize / 2, level1), Vector3.UnitZ, uv.Corner(GLUV_BOTTOM_EDGE));	// 3
					m.PushVertex(new Vector3(-fsize / 2, fsize / 2, level1), Vector3.UnitZ, uv.Corner(GLUV_LEFT_EDGE));		// 4
					// Tips
					m.PushVertex(new Vector3(0.0f, -fsize, level2), Vector3.UnitZ, uv.Corner(1));	// 5
					m.PushVertex(new Vector3(fsize, 0.0f, level2), Vector3.UnitZ, uv.Corner(2));	// 6
					m.PushVertex(new Vector3(0.0f, fsize, level2), Vector3.UnitZ, uv.Corner(3));	// 7
					m.PushVertex(new Vector3(-fsize, 0.0f, level2), Vector3.UnitZ, uv.Corner(0));	// 8
					// Center, but lower
					m.PushVertex(new Vector3(0.0f, 0.0f, level1 / 16), Vector3.UnitZ, uv.Center());

					// Cap
					m.PushTriangle(base_index, base_index + 2, base_index + 1);
					m.PushTriangle(base_index, base_index + 3, base_index + 2);
					m.PushTriangle(base_index, base_index + 4, base_index + 3);
					m.PushTriangle(base_index, base_index + 1, base_index + 4);
					// Outer triangles
					m.PushTriangle(base_index + 5, base_index + 1, base_index + 2);
					m.PushTriangle(base_index + 6, base_index + 2, base_index + 3);
					m.PushTriangle(base_index + 7, base_index + 3, base_index + 4);
					m.PushTriangle(base_index + 8, base_index + 4, base_index + 1);
					break;
					#endregion
				case TreeFoliageStyle.Bowl:
					#region Bowl style foliage
					tip_height = fsize / -4.0f;
					m.PushVertex(new Vector3(0.0f, 0.0f, tip_height), new Vector3(0.0f, 0.0f, 1.0f), uv.Center());
					m.PushVertex(new Vector3(-fsize, -fsize, -tip_height), new Vector3(-0.5f, -0.5f, 0.0f), uv.Corner(0));
					m.PushVertex(new Vector3(fsize, -fsize, -tip_height), new Vector3(0.5f, -0.5f, 0.0f), uv.Corner(1));
					m.PushVertex(new Vector3(fsize, fsize, -tip_height), new Vector3(0.5f, 0.5f, 0.0f), uv.Corner(2));
					m.PushVertex(new Vector3(-fsize, fsize, -tip_height), new Vector3(-0.5f, 0.5f, 0.0f), uv.Corner(3));
					m.PushVertex(new Vector3(0.0f, 0.0f, tip_height / 2), new Vector3(0.0f, 0.0f, 1.0f), uv.Center());
					m.PushTriangle(base_index, base_index + 1, base_index + 2);
					m.PushTriangle(base_index, base_index + 2, base_index + 3);
					m.PushTriangle(base_index, base_index + 3, base_index + 4);
					m.PushTriangle(base_index, base_index + 4, base_index + 1);

					m.PushTriangle(base_index + 5, base_index + 2, base_index + 1);
					m.PushTriangle(base_index + 5, base_index + 3, base_index + 2);
					m.PushTriangle(base_index + 5, base_index + 4, base_index + 3);
					m.PushTriangle(base_index + 5, base_index + 1, base_index + 4);

					//m.PushQuad (baseIndex + 1, baseIndex + 4, baseIndex + 3, baseIndex + 2);
					break;
					#endregion
				case TreeFoliageStyle.Umbrella:
					#region Umbrella style foliage
					tip_height = fsize / 4.0f;
					m.PushVertex(new Vector3(0.0f, 0.0f, tip_height), new Vector3(0.0f, 0.0f, 1.0f), uv.Center());
					m.PushVertex(new Vector3(-fsize, -fsize, -tip_height), new Vector3(-0.5f, -0.5f, 0.0f), uv.Corner(0));
					m.PushVertex(new Vector3(fsize, -fsize, -tip_height), new Vector3(0.5f, -0.5f, 0.0f), uv.Corner(1));
					m.PushVertex(new Vector3(fsize, fsize, -tip_height), new Vector3(0.5f, 0.5f, 0.0f), uv.Corner(2));
					m.PushVertex(new Vector3(-fsize, fsize, -tip_height), new Vector3(-0.5f, 0.5f, 0.0f), uv.Corner(3));
					m.PushVertex(new Vector3(0.0f, 0.0f, tip_height / 2), new Vector3(0.0f, 0.0f, 1.0f), uv.Center());
					//Top
					m.PushTriangle(base_index, base_index + 2, base_index + 1);
					m.PushTriangle(base_index, base_index + 3, base_index + 2);
					m.PushTriangle(base_index, base_index + 4, base_index + 3);
					m.PushTriangle(base_index, base_index + 1, base_index + 4);
					break;
					#endregion
			}
			//angle = FMath.Angle (pos.x, pos.y, 0.0f, 0.0f);
			//angle += 45.0f;
			Matrix4 mat = Matrix4.CreateRotationZ(angle);
			for (int i = base_index; i < m.vertices.Count; i++) {
				m.vertices[i] = Matrix4.
				m.vertices[i] = glMatrixTransformPoint(mat, m.vertices[i]);
				m.vertices[i] += pos;
			}
		}

		private void DoVines(Mesh m, List<Vector3> points) {
			if (mHasVines) {
				int baseIndex = m.VertexCount;
				for (int segment = 0; segment < points.Count; segment++) {
					m.PushVertex(points[segment], Vector3.UnitZ, new Vector2(0.75f, segment));
					m.PushVertex(points[segment] + new Vector3(0.0f, 0.0f, -3.5f), Vector3.UnitZ, new Vector2(0.5f, segment));
				}
				for (int segment = 0; segment < points.Count - 1; segment++) {
					m.PushTriangle(
								baseIndex + segment * 2,
								baseIndex + segment * 2 + 1,
								baseIndex + (segment + 1) * 2 + 1);
					m.PushTriangle(
								baseIndex + segment * 2,
								baseIndex + (segment + 1) * 2 + 1,
								baseIndex + (segment + 1) * 2);
				}
			}
		}

		private void DoBranch(Mesh m, BranchAnchor anchor, float branch_angle, LOD lod) {
			if ((anchor.length >= 2.0f) && (anchor.radius >= MIN_RADIUS)) {
				//int segmentCount = Math.Max((int) (anchor.length * SEGMENTS_PER_METER), MIN_SEGMENTS) + 3;
				int baseIndex = m.VertexCount;
				Matrix4 mat = Matrix4.CreateRotationZ(branch_angle);

				int segmentCount, radialSteps;
				switch (lod) {
					case LOD.Low:
						segmentCount = 2;
						radialSteps = 2;
						break;
					case LOD.Med:
						radialSteps = 2;
						segmentCount = 3;
						break;
					default:
						segmentCount = 5;
						radialSteps = 6;
						break;
				}

				#region Make vertices for the branch
				int radialEdge = radialSteps + 1;
				Vector3 core = anchor.root;
				float radius = anchor.radius;
				float curve;
				Vector3 pos;
				List<Vector3> underside = new List<Vector3>();

				for (int segment = 0; segment <= segmentCount; segment++) {
					float horzPos = (float) segment / (float) (segmentCount + 1);

					switch (mLiftStyle) {
						case TreeLiftStyle.Out:
							curve = horzPos * horzPos;
							break;
						case TreeLiftStyle.In:
							curve = 1.0f - horzPos;
							curve *= curve * curve; ;
							curve = 1.0f - curve;
							break;
						default: // Straight
							curve = horzPos;
							break;
					}

					radius = Math.Max(MIN_RADIUS, anchor.radius * (1.0f - horzPos));
					core.Z = anchor.root.Z + anchor.lift * curve * mBranchLift;

					if (segment == segmentCount) {
						// If this is the last segment, don't make a ring of points. Make ONE, in the center.
						// This is so the branch can end at a point.
						pos = new Vector3(0.0f, anchor.length * horzPos, 0.0f);
						pos = glMatrixTransformPoint(mat, pos);
						m.PushVertex(pos + core, new Vector3(pos.X, 0.0f, pos.Z), new Vector2(0.249f, pos.Y * mTextureTile));
					} else {
						for (int ring = 0; ring <= radialSteps; ring++) {
							// Make sure the final edge perfectly matches the starting one. Can't leave this to floating-point math.
							float angle;
							if (ring == radialSteps || ring == 0)
								angle = 0.0f;
							else
								angle = (float) ring * (360.0f / (float) radialSteps);
							angle *= DEGREES_TO_RADIANS;
							pos.x = -Math.Sin(angle) * radius;
							pos.y = anchor.length * horzPos;
							pos.z = -Math.Cos(angle) * radius;
							pos = glMatrixTransformPoint(mat, pos);
							m.PushVertex(pos + core, new Vector3(pos.X, 0.0f, pos.Z), new Vector2(((float) ring / (float) radialSteps) * 0.249f, pos.Y * mTextureTile));
						}
					}
					underside.Add(pos + core);
				}
				#endregion

				#region Make the triangles for the branch
				for (int segment = 0; segment < segmentCount; segment++) {
					for (int ring = 0; ring < radialSteps; ring++) {
						if (segment < segmentCount - 1) {
							m.PushQuad(baseIndex + (ring + 0) + (segment + 0) * (radialEdge),
								baseIndex + (ring + 0) + (segment + 1) * (radialEdge),
								baseIndex + (ring + 1) + (segment + 1) * (radialEdge),
								baseIndex + (ring + 1) + (segment + 0) * (radialEdge));
						} else {//this is the last segment. It ends in a single point
							m.PushTriangle(
								baseIndex + (ring + 1) + segment * (radialEdge),
								baseIndex + (ring + 0) + segment * (radialEdge),
								m.VertexCount - 1);
						}
					}
				}
				#endregion

				// Grab the last point and use it as the origin for the foliage
				pos = m.vertices[m.VertexCount - 1];
				DoFoliage(m, pos, anchor.length * 0.56f, branch_angle);

				// We saved the points on the underside of the branch. Use these to hang vines on the branch
				if (lod == LOD.High)
					DoVines(m, underside);
			}
		}

		private void DoTrunk(Mesh m, int localSeed, LOD lod) {
			List<BranchAnchor> BranchList = new List<BranchAnchor>();

			#region Determine the branch locations
			float branch_spacing = (0.95f - mCurrentLowestBranch) / (float) mCurrentBranches;
			for (int i = 0; i < mCurrentBranches; i++) {
				float vertical_pos = mCurrentLowestBranch + branch_spacing * (float) i;

				BranchAnchor branch;
				branch.root = TrunkPosition(vertical_pos, branch.radius);
				branch.length = (mCurrentHeight - branch.root.Z) * mBranchReach;
				branch.length = Math.Min(branch.length, mCurrentHeight / 2);
				branch.lift = (branch.length) / 2;

				BranchList.Add(branch);
			}
			#endregion

			if (lod == LOD.Low) {
				#region Just make a 2-panel facer
				//Use the fourth frame of our texture
				UVBox uv;
				uv.Set(new Vector2(0.75f, 0.0f), new Vector2(1.0f, 1.0f));
				float height = mCurrentHeight;
				float width = mCurrentHeight / 2.0f;
				//First panel
				m.PushVertex(new Vector3(-width, -width, 0.0f), new Vector3(-width, -width, 0.0f), uv.Corner(0));
				m.PushVertex(new Vector3(width, width, 0.0f), new Vector3(width, width, 0.0f), uv.Corner(1));
				m.PushVertex(new Vector3(width, width, height), new Vector3(width, width, height), uv.Corner(2));
				m.PushVertex(new Vector3(-width, -width, height), new Vector3(-width, -width, height), uv.Corner(3));
				//Second Panel
				m.PushVertex(new Vector3(-width, width, 0.0f), new Vector3(-width, width, 0.0f), uv.Corner(0));
				m.PushVertex(new Vector3(width, -width, 0.0f), new Vector3(width, -width, 0.0f), uv.Corner(1));
				m.PushVertex(new Vector3(width, -width, height), new Vector3(width, -width, height), uv.Corner(2));
				m.PushVertex(new Vector3(-width, width, height), new Vector3(-width, width, height), uv.Corner(3));
				for (int i = 0; i < (int) m.NormalCount; i++)
					m.normals[i].Normalize();
				m.PushQuad(0, 1, 2, 3);
				m.PushQuad(4, 5, 6, 7);
				#endregion
			} else {
				// Work out the circumference of the BASE of the tree
				float circumference = mCurrentBaseRadius * mCurrentBaseRadius * (float) Math.PI;
				// The texture will repeat ONCE horizontally around the tree. Set the vertical to repeat in the same distance.
				mTextureTile = 1;	//(float)((int)circumference + 0.5f); 
				int radialSteps = 3;
				if (lod == LOD.High)
					radialSteps = 7;
				int radialEdge = radialSteps + 1;
				int segmentCount = 0;

				#region Work our way Vector3.UnitZ the tree, building rings of verts
				float radius;
				Vector3 core;
				for (int i = -1; i < (int) BranchList.Count; i++) {
					if (i < 0) { //-1 is the bottom rung, the root. Put it underground, widen it a bit
						core = TrunkPosition(0.0f, radius);
						radius *= 1.5f;
						core.Z -= 2.0f;
					} else {
						core = BranchList[i].root;
						radius = BranchList[i].radius;
					}

					for (int ring = 0; ring <= radialSteps; ring++) {
						//Make sure the final edge perfectly matches the starting one. Can't leave this to floating-point math.
						float angle, x, y;
						if (ring == radialSteps || ring == 0)
							angle = 0.0f;
						else
							angle = (float) ring * (360.0f / (float) radialSteps);
						angle *= DEGREES_TO_RADIANS;
						x = (float) Math.Sin(angle);
						y = (float) Math.Cos(angle);
						m.PushVertex(core + new Vector3(x * radius, y * radius, 0.0f),
							new Vector3(x, y, 0.0f),
							new Vector2(((float) ring / (float) radialSteps) * 0.249f, core.Z * mTextureTile));

					}
					segmentCount++;
				}
				#endregion

				#region Push one more point, for the very tip of the tree
				float rad = 0;
				m.PushVertex(TrunkPosition(1.0f, ref rad), Vector3.UnitZ, Vector2.Zero);
				// Make the triangles for the main trunk.
				for (int segment = 0; segment < segmentCount - 1; segment++) {
					for (int ring = 0; ring < radialSteps; ring++) {
						m.PushQuad((ring + 0) + (segment + 0) * (radialEdge),
							(ring + 1) + (segment + 0) * (radialEdge),
							(ring + 1) + (segment + 1) * (radialEdge),
							(ring + 0) + (segment + 1) * (radialEdge));
					}
				}
				#endregion

				#region Make the triangles for the tip
				for (int ring = 0; ring < radialSteps; ring++)
					m.PushTriangle((ring + 1) + (segmentCount - 1) * radialEdge, m.VertexCount - 1, (ring + 0) + (segmentCount - 1) * radialEdge);

				DoFoliage(m, m.vertices[m.VertexCount - 1], mCurrentHeight / 2, 0.0f);
				//if (!mCanopy) {
				//DoFoliage (TrunkPosition (vertical_pos, Null), vertical_pos * _height, 0.0f);
				if (mEvergreen) { //just rings of foliage, like an evergreen
					for (int i = 0; i < (int) BranchList.Count; i++) {
						float angle = (float) i * ((360.0f / (float) BranchList.Count));
						DoFoliage(m, BranchList[i].root, BranchList[i].length, angle);
					}
				} else { //has branches
					for (int i = 0; i < (int) BranchList.Count; i++) {
						float angle = mCurrentAngleOffset + (float) i * ((360.0f / (float) BranchList.Count) + 180.0f);
						DoBranch(m, BranchList[i], angle, lod);
					}
				}
				//}
				#endregion
			}
		}

		private void Build() {
			//_branches = 3 + WorldNoisei (mSeedCurrent++) % 3;
			//_trunk_bend_frequency = 3.0f + WorldNoisef (mSeedCurrent++) * 4.0f;
			mSeedCurrent = mSeed;
			for (int alt = 0; alt < TREE_ALTS; alt++) {
				mCurrentAngleOffset = WorldNoisef(mSeedCurrent++) * 360.0f;
				mCurrentHeight = mDefaultHeight * (0.5f + WorldNoisef(mSeedCurrent++));
				mCurrentBaseRadius = mDefaultBaseRadius * (0.5f + WorldNoisef(mSeedCurrent++));
				mCurrentBranches = mDefaultBranches + WorldNoisei(mSeedCurrent++) % 3;
				mCurrentBendFrequency = mDefaultBendFrequency + WorldNoisef(mSeedCurrent++);
				mCurrentLowestBranch = mDefaultLowestBranch + WorldNoisef(mSeedCurrent++) * 0.2f;
				for (int lod = 0; lod < LOD_LEVELS; lod++) {
					mMeshes[alt, lod].Clear();
					DoTrunk(mMeshes[alt, lod], mSeedCurrent + alt, (LOD) lod);
					//The facers use hand-made normals, so don't recalculate them.
					if ((LOD) lod != LOD.Low)
						mMeshes[alt,lod].CalculateNormalsSeamless();
				}
			}
		}

		private void DoLeaves() {
			if (mLeafStyle == TreeLeafStyle.Fan) {
				#region Fan leaves
				int totalSteps = 5;
				for (float currentSteps = totalSteps; currentSteps >= 1.0f; currentSteps -= 1.0f) {
					float size = (TEXTURE_HALF / 2) / (1.0f + (totalSteps - currentSteps));
					float radius = (TEXTURE_HALF - size * 2.0f);
					float circ = (float) (Math.PI * radius * 2);
					float stepSize = 360.0f / currentSteps;
					for (float x = 0.0f; x < 360.0f; x += stepSize) {
						float rad = x * DEGREES_TO_RADIANS;
						Leaf l;
						l.size = size;
						l.position.X = (float) (TEXTURE_HALF + Math.Sin(rad) * l.size);
						l.position.Y = (float) (TEXTURE_HALF + Math.Cos(rad) * l.size);
						l.angle = -FMath.Angle(TEXTURE_HALF, TEXTURE_HALF, l.position.X, l.position.Y);
						//l.brightness = 1.0f - (currentSteps / (float)totalSteps) * WorldNoisef (mSeedCurrent++) * 0.5f;
						//l.brightness = 1.0f - WorldNoisef (mSeedCurrent++) * 0.2f;
						//l.color = ColorInterpolate (mLeafColor, Color (0.0f, 0.5f, 0.0f), WorldNoisef (mSeedCurrent++) * 0.25f);
						mLeafList.Add(l);
					}
				}
				#endregion
			} else if (mLeafStyle == TreeLeafStyle.Scatter) {
				#region Scattered leaves
				// Put one big leaf in the center
				float leafSize = TEXTURE_HALF / 3;
				Leaf l;
				l.size = leafSize;
				l.position.X = TEXTURE_HALF;
				l.position.Y = TEXTURE_HALF;
				l.angle = 0.0f;
				mLeafList.Add(l);

				// Now scatter other leaves around
				for (int i = 0; i < 50; i++) {
					l.size = leafSize * 0.5f;//  * (0.5f + WorldNoisef (mSeedCurrent++);
					l.position.X = TEXTURE_HALF + (WorldNoisef(mSeedCurrent++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					l.position.Y = TEXTURE_HALF + (WorldNoisef(mSeedCurrent++) - 0.5f) * (TEXTURE_HALF - l.size) * 2.0f;
					Vector2 delta = mLeafList[i].position - new Vector2(TEXTURE_HALF, TEXTURE_HALF);
					l.dist = delta.Length;
					//Leaves get smaller as we move from the center of the texture
					l.size = (0.25f + ((TEXTURE_HALF - l.dist) / TEXTURE_HALF) * 0.75f) * leafSize;
					l.angle = 0.0f;
					//l.brightness = 0.7f + ((float)i / 50) * 0.3f;
					//l.color = 
					mLeafList.Add(l);
				}
				//Sort our list of leaves, inward out
				qsort(mLeafList, mLeafList.Count, sizeof(Leaf), sort_leaves);
				//now look at each leaf and figure out its closest neighbor
				for (int i = 0; i < mLeafList.Count; i++) {
					Leaf l_i = mLeafList[i];
					l_i.neighbor = 0;
					Vector2 delta = l_i.position - mLeafList[0].position;
					float nearest = delta.Length;
					for (int j = 1; j < i; j++) {
						//Don't connect this leaf to itself!
						if (j == i)
							continue;
						delta = mLeafList[i].position - mLeafList[j].position;
						float distance = delta.Length;
						if (distance < nearest) {
							l_i.neighbor = j;
							nearest = distance;
						}
					}
				}

				// Now we have the leaves, and we know their neighbors. Get the angles between them
				for (int i = 1; i < mLeafList.Count; i++) {
					int j = mLeafList[i].neighbor;
					Leaf l_i = mLeafList[i];
					l_i.angle = -FMath.Angle(
						mLeafList[j].position.X, mLeafList[j].position.Y,
						mLeafList[i].position.X, mLeafList[i].position.Y);
				}
				#endregion
			}

			for (int i = 0; i < mLeafList.Count; i++)
				mLeafList[i].color = ColorInterpolate(mLeafColor, new Color4(0.0f, 0.5f, 0.0f, 0.0f), WorldNoisef(mSeedCurrent++) * 0.33f);
		}

		private void DrawFacer() {
			GL.Disable(EnableCap.Blend);

			// We get the bounding box for the high-res tree, but we cut off the roots. No reason to waste texture pixels on that.
			mMeshes[0, LOD_HIGH].RecalculateBoundingBox();
			BBox box = mMeshes[0, LOD_HIGH]._bbox;
			box.pmin.z = 0.0f;//Cuts off roots
			Vector3 center = box.Center();
			Vector3 size = box.Size();

			// Move our viewpoint to the middle of the texture frame 
			GL.Translate(TEXTURE_HALF, TEXTURE_HALF, 0.0f);
			GL.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);

			// Scale so that the tree will exactly fill the rectangle
			GL.Scale((1.0f / size.X) * TEXTURE_SIZE, 1.0f, (1.0f / size.Z) * TEXTURE_SIZE);
			GL.Translate(-center.X, 0.0f, -center.Z);
			GL.Color3(1, 1, 1);
			Render(Vector3.Zero, 0, LOD_HIGH);
		}

		private void DrawVines() {
			GL.Color4(mBarkColor1);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GLtexture t = TextureFromName("vines.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);

			int frames = Math.Max(t.height / t.width, 1);
			float frameSize = 1.0f / (float) frames;
			int frame = WorldNoisei(mSeedCurrent++) % frames;
			UVBox uvframe;
			uvframe.Set(new Vector2(0.0f, (float) frame * frameSize), new Vector2(1.0f, (float) (frame + 1) * frameSize));

			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			Color4 color = mLeafColor * 0.75f;
			GL.Color4(mLeafColor);

			GL.Begin(BeginMode.Quads);

			Vector2 uv;
			uv = uvframe.Corner(3);		GL.TexCoord2(uv);		GL.Vertex2(0, 0);
			uv = uvframe.Corner(0);		GL.TexCoord2(uv);		GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(1);		GL.TexCoord2(uv);		GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(2);		GL.TexCoord2(uv);		GL.Vertex2(0, TEXTURE_SIZE);

			GL.End();
		}

		private void DrawLeaves() {
			if (mLeafStyle == TreeLeafStyle.Scatter) {
				Color4 c = mBarkColor1;
				c *= 0.5f;

				GL.BindTexture(TextureTarget.Texture2D, 0);
				GL.LineWidth(3.0f);
				GL.Color4(c);

				GL.Begin(BeginMode.Lines);
				for (int i = 0; i < mLeafList.Count; i++) {
					GL.Vertex2(mLeafList[mLeafList[i].neighbor].position);
					GL.Vertex2(mLeafList[i].position);
				}
				GL.End();
			}

			GLtexture t = TextureFromName("foliage.png");
			int frames = Math.Max(t.height / t.width, 1);
			float frame_size = 1.0f / (float) frames;
			int frame = WorldNoisei(mSeedCurrent++) % frames;
			UVBox uvframe;
			uvframe.Set(new Vector2(0.0f, (float) frame * frame_size), new Vector2(1.0f, (float) (frame + 1) * frame_size));

			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
			//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	

			for (int i = 0; i < mLeafList.Count; i++) {
				Leaf l = mLeafList[i];

				GL.PushMatrix();
				GL.Translate(l.position.X, l.position.Y, 0);
				GL.Rotate(l.angle, 0.0f, 0.0f, 1.0f);
				GL.Translate(-l.position.X, -l.position.Y, 0);

				//Color color = mLeafColor * l.brightness;
				GL.Color4(l.color);
				GL.Begin(BeginMode.Quads);

				Vector2   uv;
				uv = uvframe.Corner(0);		GL.TexCoord2(uv);		GL.Vertex2(l.position.X - l.size, l.position.Y - l.size);
				uv = uvframe.Corner(1);		GL.TexCoord2(uv);		GL.Vertex2(l.position.X + l.size, l.position.Y - l.size);
				uv = uvframe.Corner(2);		GL.TexCoord2(uv);		GL.Vertex2(l.position.X + l.size, l.position.Y + l.size);
				uv = uvframe.Corner(3);		GL.TexCoord2(uv);		GL.Vertex2(l.position.X - l.size, l.position.Y + l.size);

				GL.End();
				GL.PopMatrix();
			}
		}

		private void DrawBark() {
			GLtexture   t;
			UVBox     uvframe;
			int         frames;
			int         frame;
			float       frame_size;
			Vector2   uv;

			GL.Color4(mBarkColor1);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(0, 0);		GL.Vertex2(0, 0);
			GL.TexCoord2(1, 0);		GL.Vertex2(TEXTURE_SIZE, 0);
			GL.TexCoord2(1, 1);		GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			GL.TexCoord2(0, 1);		GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();

			t = TextureFromName("bark1.bmp");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			frames = Math.Max(t.height / t.width, 1);
			frame_size = 1.0f / (float) frames;
			frame = WorldNoisei(mSeedCurrent++) % frames;
			uvframe.Set(new Vector2(0.0f, (float) frame * frame_size), new Vector2(1.0f, (float) (frame + 1) * frame_size));

			GL.BindTexture(TextureTarget.Texture2D, t.id);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ColorMask(true, true, true, false);
			GL.Color4(mBarkColor2);
			GL.Begin(BeginMode.Quads);
			uv = uvframe.Corner(0);		GL.TexCoord2(uv);		GL.Vertex2(0, 0);
			uv = uvframe.Corner(1);		GL.TexCoord2(uv);		GL.Vertex2(TEXTURE_SIZE, 0);
			uv = uvframe.Corner(2);		GL.TexCoord2(uv);		GL.Vertex2(TEXTURE_SIZE, TEXTURE_SIZE);
			uv = uvframe.Corner(3);		GL.TexCoord2(uv);		GL.Vertex2(0, TEXTURE_SIZE);
			GL.End();
			GL.ColorMask(true, true, true, true);
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

			if (Texture != 0)
				GL.DeleteTextures(1, Texture);
			GL.GenTextures(1, Texture);
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEXTURE_SIZE * 4, TEXTURE_SIZE, 0, PixelFormat.Rgba, PixelType.Byte, null);
			RenderCanvasBegin(0, TEXTURE_SIZE, 0, TEXTURE_SIZE, TEXTURE_SIZE);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
			char* buffer = new char[TEXTURE_SIZE * TEXTURE_SIZE * 4];
			for (int i = 0; i < 4; i++) {
				GL.ClearColor(1.0f, 0.0f, 1.0f, 0.0f);
				GL.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
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
				//GL.CopyTexSubImage2D (GL_TEXTURE_2D, 0, TEXTURE_SIZE * i, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE);
				GL.ReadPixels(0, 0, TEXTURE_SIZE, TEXTURE_SIZE, GL_RGBA, GL_int_BYTE, buffer);
				//CgShaderSelect (FSHADER_MASK_TRANSFER);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, TEXTURE_SIZE * i, 0, TEXTURE_SIZE, TEXTURE_SIZE, GL_RGBA, GL_int_BYTE, buffer);
				//CgShaderSelect (FSHADER_NONE);
			}
			delete buffer;
			RenderCanvasEnd();
		}
		#endregion

		#region Public methods
		public void Create(bool is_canopy, float moisture, float temp_in, int seed_in) {
			//Prepare, clear the tables, etc.
			mLeafList.Clear();
			mSeed = seed_in;
			mSeedCurrent = mSeed;
			mMoisture = moisture;
			mCanopy = is_canopy;
			mTemperature = temp_in;
			mSeedCurrent = mSeed;
			//We want our height to fall on a bell curve
			mDefaultHeight = 8.0f + WorldNoisef(mSeedCurrent++) * 4.0f + WorldNoisef(mSeedCurrent++) * 4.0f;
			mDefaultBendFrequency = 1.0f + WorldNoisef(mSeedCurrent++) * 2.0f;
			mDefaultBaseRadius = 0.2f + (mDefaultHeight / 20.0f) * WorldNoisef(mSeedCurrent++);
			mDefaultBranches = 2 + WorldNoisei(mSeedCurrent) % 2;
			//Keep branches away from the ground, since they don't have collision
			mDefaultLowestBranch = (3.0f / mDefaultHeight);
			//Funnel trunk trees taper off quickly at the base.
			mFunnelTrunk = (WorldNoisei(mSeedCurrent++) % 6) == 0;
			if (mFunnelTrunk) {//Funnel trees need to be bigger and taller to look right
				mDefaultBaseRadius *= 1.2f;
				mDefaultHeight *= 1.5f;
			}
			mTrunkStyle = (TreeTrunkStyle) (WorldNoisei(mSeedCurrent) % TREE_TRUNK_STYLES);
			mFoliageStyle = (TreeFoliageStyle) (WorldNoisei(mSeedCurrent++) % TREE_FOLIAGE_STYLES);
			mLiftStyle = (TreeLiftStyle) (WorldNoisei(mSeedCurrent++) % TREE_LIFT_STYLES);
			mLeafStyle = (TreeLeafStyle) (WorldNoisei(mSeedCurrent++) % TREE_LEAF_STYLES);
			mEvergreen = mTemperature + (WorldNoisef(mSeedCurrent++) * 0.25f) < 0.5f;
			mHasVines = mMoisture > 0.6f && mTemperature > 0.5f;
			//Narrow trees can gorw on top of hills. (Big ones will stick out over cliffs, so we place them low.)
			if (mDefaultBaseRadius <= 1.0f)
				_grows_high = true;
			else
				_grows_high = false;
			mBranchReach = 1.0f + WorldNoisef(mSeedCurrent++) * 0.5f;
			mBranchLift = 1.0f + WorldNoisef(mSeedCurrent++);
			mFoliageSize = 1.0f;
			mLeafSize = 0.125f;
			mLeafColor = TerraformColorGenerate(SURFACE_COLOR_GRASS, moisture, mTemperature, mSeedCurrent++);
			mBarkColor2 = TerraformColorGenerate(SURFACE_COLOR_DIRT, moisture, mTemperature, mSeedCurrent++);
			mBarkColor1 = mBarkColor2 * 0.5f;
			//1 in 8 non-tropical trees has white bark
			if (!mHasVines && !(WorldNoisei(mSeedCurrent++) % 8))
				mBarkColor2 = Color4(1.0f);
			//These foliage styles don't look right on evergreens.
			if (mEvergreen && mFoliageStyle == TREE_FOLIAGE_BOWL)
				mFoliageStyle = TREE_FOLIAGE_UMBRELLA;
			if (mEvergreen && mFoliageStyle == TREE_FOLIAGE_SHIELD)
				mFoliageStyle = TREE_FOLIAGE_UMBRELLA;
			if (mEvergreen && mFoliageStyle == TREE_FOLIAGE_PANEL)
				mFoliageStyle = TREE_FOLIAGE_SAG;
			if (mCanopy) {
				mFoliageStyle = TREE_FOLIAGE_UMBRELLA;
				mDefaultHeight = max(mDefaultHeight, 16.0f);
				mDefaultBaseRadius = 1.5f;
				mFoliageSize = 2.0f;
				mTrunkStyle = TREE_TRUNK_NORMAL;
			}
			Build();
			DoLeaves();
			DoTexture();
		}

		//Render a single tree. Very slow. Used for debugging. 
		public void Render(Vector3 pos, int alt, LOD lod) {
			GL.Enable(GL_BLEND);
			GL.Enable(GL_TEXTURE_2D);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BindTexture(GL_TEXTURE_2D, _texture);
			GL.PushMatrix();
			GL.Translatef(pos.x, pos.y, pos.z);
			mMeshes[alt][lod].Render();
			GL.PopMatrix();
		}

		public void Info() { TextPrint("TREE:\nSeed:%d Moisture: %f Temp: %f", mSeed, mMoisture, mTemperature); }

		public void TexturePurge() {
			if (Texture)
				DoTexture();
		}
		#endregion
	}
}
