/*-----------------------------------------------------------------------------

  CTree.cpp

-------------------------------------------------------------------------------

  

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ctree.h"
#include "math.h"
#include "render.h"
#include "terraform.h"
#include "text.h"
#include "texture.h"
#include "vbo.h"
#include "world.h"

#define SEGMENTS_PER_METER    0.25f
#define MIN_SEGMENTS          3
//#define TEXTURE_SIZE          512
#define TEXTURE_SIZE          256
#define TEXTURE_HALF          (TEXTURE_SIZE / 2)
#define MIN_RADIUS            0.3f
#define UP                    glVector (0.0f, 0.0f, 1.0f)

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

int sort_leaves (const void* elem1, const void* elem2)
{
  Leaf*   e1 = (Leaf*)elem1;
  Leaf*   e2 = (Leaf*)elem2;

  if (e1->dist < e2->dist)
    return -1;
  else if (e1->dist > e2->dist)
    return 1;
  return 0;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLmesh* CTree::Mesh (unsigned alt, LOD lod)
{

  return &_meshes[alt % TREE_ALTS][lod];

}

//Given the value of 0.0 (root) to 1.0f (top), return the center of the trunk 
//at that height.
GLvector CTree::TrunkPosition (float delta, float* radius)
{

  GLvector    trunk;
  float       bend;
  float       delta_curve;

  if (_funnel_trunk) {
    delta_curve = 1.0f - delta;
    delta_curve *= delta_curve;
    delta_curve = 1.0f - delta_curve;
  } else 
    delta_curve = delta;
  if (radius) {
    *radius = _current_base_radius * (1.0f - delta_curve);
    *radius = max (*radius, MIN_RADIUS);
  }
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
  return trunk;
  
}

void CTree::DoFoliage (GLmesh* m, GLvector pos, float fsize, float angle)
{

  GLuvbox   uv;
  int       base_index;

  fsize *= _foliage_size;
  uv.Set (glVector (0.25f, 0.0f), glVector (0.5f, 1.0f));
  base_index = m->_vertex.size ();

  //don't let the foliage get so big it touches the ground.
  fsize = min (pos.z - 2.0f, fsize);
  if (fsize < 0.1f)
    return;
  if (_foliage_style == TREE_FOLIAGE_PANEL) {
    m->PushVertex (glVector (-0.0f, -fsize, -fsize), UP, uv.Corner (0));
    m->PushVertex (glVector (-1.0f,  fsize, -fsize), UP, uv.Corner (1));
    m->PushVertex (glVector (-1.0f,  fsize,  fsize), UP, uv.Corner (2));
    m->PushVertex (glVector (-0.0f, -fsize,  fsize), UP, uv.Corner (3));

    m->PushVertex (glVector ( 0.0f, -fsize, -fsize), UP, uv.Corner (1));
    m->PushVertex (glVector ( 1.0f,  fsize, -fsize), UP, uv.Corner (2));
    m->PushVertex (glVector ( 1.0f,  fsize,  fsize), UP, uv.Corner (3));
    m->PushVertex (glVector ( 0.0f, -fsize,  fsize), UP, uv.Corner (0));

    m->PushQuad (base_index + 0, base_index + 1, base_index + 2, base_index + 3);
    m->PushQuad (base_index + 7, base_index + 6, base_index + 5, base_index + 4);

  } else if (_foliage_style == TREE_FOLIAGE_SHIELD) {
    m->PushVertex (glVector ( fsize / 2, 0.0f,  0.0f), UP, uv.Center ());
    m->PushVertex (glVector (0.0f, -fsize, 0.0f), UP, uv.Corner (0));
    m->PushVertex (glVector (0.0f,  0.0f,  fsize), UP, uv.Corner (1));
    m->PushVertex (glVector (0.0f,  fsize, 0.0f), UP, uv.Corner (2));
    m->PushVertex (glVector (0.0f,  0.0f,  -fsize), UP, uv.Corner (3));
    m->PushVertex (glVector (-fsize / 2, 0.0f,  0.0f), UP, uv.Center ());
    //Cap
    m->PushTriangle (base_index, base_index + 1, base_index + 2);
    m->PushTriangle (base_index, base_index + 2, base_index + 3);
    m->PushTriangle (base_index, base_index + 3, base_index + 4);
    m->PushTriangle (base_index, base_index + 4, base_index + 1);
    m->PushTriangle (base_index + 5, base_index + 2, base_index + 1);
    m->PushTriangle (base_index + 5, base_index + 3, base_index + 2);
    m->PushTriangle (base_index + 5, base_index + 4, base_index + 3);
    m->PushTriangle (base_index + 5, base_index + 1, base_index + 4);
  } else if (_foliage_style == TREE_FOLIAGE_SAG) {
    /*     /\
          /__\
         /|  |\
         \|__|/
          \  /
           \/   */
    float level1   = fsize * -0.4f;
    float level2   = fsize * -1.2f;
    GLuvbox   uv_inner;

    uv_inner.Set (glVector (0.25f + 1.25f, 0.125f), glVector (0.5f - 0.125f, 1.0f - 0.125f));
    //Center
    m->PushVertex (glVector ( 0.0f, 0.0f, 0.0f), UP, uv.Center ());
    //First ring
    m->PushVertex (glVector (-fsize / 2, -fsize / 2, level1), UP, uv.Corner (GLUV_TOP_EDGE));//1
    m->PushVertex (glVector ( fsize / 2, -fsize / 2, level1), UP, uv.Corner (GLUV_RIGHT_EDGE));//2
    m->PushVertex (glVector ( fsize / 2,  fsize / 2, level1), UP, uv.Corner (GLUV_BOTTOM_EDGE));//3
    m->PushVertex (glVector (-fsize / 2,  fsize / 2, level1), UP, uv.Corner (GLUV_LEFT_EDGE));//4
    //Tips
    m->PushVertex (glVector (0.0f, -fsize, level2), UP, uv.Corner (1));//5
    m->PushVertex (glVector (fsize,  0.0f, level2), UP, uv.Corner (2));//6
    m->PushVertex (glVector (0.0f,  fsize, level2), UP, uv.Corner (3));//7
    m->PushVertex (glVector (-fsize, 0.0f, level2), UP, uv.Corner (0));//8
    //Center, but lower
    m->PushVertex (glVector ( 0.0f, 0.0f, level1 / 16), UP, uv.Center ());
    
    //Cap
    m->PushTriangle (base_index, base_index + 2, base_index + 1);
    m->PushTriangle (base_index, base_index + 3, base_index + 2);
    m->PushTriangle (base_index, base_index + 4, base_index + 3);
    m->PushTriangle (base_index, base_index + 1, base_index + 4);
    //Outer triangles
    m->PushTriangle (base_index + 5, base_index + 1, base_index + 2);
    m->PushTriangle (base_index + 6, base_index + 2, base_index + 3);
    m->PushTriangle (base_index + 7, base_index + 3, base_index + 4);
    m->PushTriangle (base_index + 8, base_index + 4, base_index + 1);
  } else if (_foliage_style == TREE_FOLIAGE_BOWL) {
    float  tip_height;

    tip_height = fsize / 4.0f;
    if (_foliage_style == TREE_FOLIAGE_BOWL)
      tip_height *= -1.0f;
    m->PushVertex (glVector (0.0f, 0.0f, tip_height), glVector (0.0f, 0.0f, 1.0f), uv.Center ());
    m->PushVertex (glVector (-fsize, -fsize, -tip_height), glVector (-0.5f, -0.5f, 0.0f), uv.Corner (0));
    m->PushVertex (glVector (fsize, -fsize, -tip_height), glVector ( 0.5f, -0.5f, 0.0f), uv.Corner (1));
    m->PushVertex (glVector (fsize, fsize, -tip_height), glVector ( 0.5f, 0.5f, 0.0f), uv.Corner (2));
    m->PushVertex (glVector (-fsize, fsize, -tip_height), glVector ( -0.5f, 0.5f, 0.0f), uv.Corner (3));
    m->PushVertex (glVector (0.0f, 0.0f, tip_height / 2), glVector (0.0f, 0.0f, 1.0f), uv.Center ());
    m->PushTriangle (base_index, base_index + 1, base_index + 2);
    m->PushTriangle (base_index, base_index + 2, base_index + 3);
    m->PushTriangle (base_index, base_index + 3, base_index + 4);
    m->PushTriangle (base_index, base_index + 4, base_index + 1);

    m->PushTriangle (base_index + 5, base_index + 2, base_index + 1);
    m->PushTriangle (base_index + 5, base_index + 3, base_index + 2);
    m->PushTriangle (base_index + 5, base_index + 4, base_index + 3);
    m->PushTriangle (base_index + 5, base_index + 1, base_index + 4);

    //m->PushQuad (base_index + 1, base_index + 4, base_index + 3, base_index + 2);
  } else if (_foliage_style == TREE_FOLIAGE_UMBRELLA) {
    float  tip_height;

    tip_height = fsize / 4.0f;
    m->PushVertex (glVector (0.0f, 0.0f, tip_height), glVector (0.0f, 0.0f, 1.0f), uv.Center ());
    m->PushVertex (glVector (-fsize, -fsize, -tip_height), glVector (-0.5f, -0.5f, 0.0f), uv.Corner (0));
    m->PushVertex (glVector (fsize, -fsize, -tip_height), glVector ( 0.5f, -0.5f, 0.0f), uv.Corner (1));
    m->PushVertex (glVector (fsize, fsize, -tip_height), glVector ( 0.5f, 0.5f, 0.0f), uv.Corner (2));
    m->PushVertex (glVector (-fsize, fsize, -tip_height), glVector ( -0.5f, 0.5f, 0.0f), uv.Corner (3));
    m->PushVertex (glVector (0.0f, 0.0f, tip_height / 2), glVector (0.0f, 0.0f, 1.0f), uv.Center ());
    //Top
    m->PushTriangle (base_index, base_index + 2, base_index + 1);
    m->PushTriangle (base_index, base_index + 3, base_index + 2);
    m->PushTriangle (base_index, base_index + 4, base_index + 3);
    m->PushTriangle (base_index, base_index + 1, base_index + 4);
  }   
  GLmatrix  mat;
  unsigned  i;
  //angle = MathAngle (pos.x, pos.y, 0.0f, 0.0f);
  //angle += 45.0f;
  mat.Identity ();
  mat.Rotate (angle, 0.0f, 0.0f, 1.0f);
  for (i = base_index; i < m->_vertex.size (); i++) {
    m->_vertex[i] = glMatrixTransformPoint (mat, m->_vertex[i]);
    m->_vertex[i] += pos;
  }

}

void CTree::DoVines (GLmesh* m, GLvector* points, unsigned segments)
{

  unsigned          base_index;
  unsigned          segment;

  if (!_has_vines)
    return;
  base_index = m->_vertex.size ();
  for (segment = 0; segment < segments; segment++) {
    float  v = (float)segment;
    m->PushVertex (points[segment], UP, glVector (0.5f, v));
    m->PushVertex (points[segment] + glVector (0.0f, 0.0f, -3.5f), UP, glVector (0.75f, v));
  }
  for (segment = 0; segment < segments - 1; segment++) {
    m->PushTriangle (
          base_index + segment * 2,
          base_index + segment * 2 + 1,
          base_index + (segment + 1) * 2 + 1);
    m->PushTriangle (
          base_index + segment * 2,
          base_index + (segment + 1) * 2 + 1,
          base_index + (segment + 1) * 2);
  }

}

void CTree::DoBranch (GLmesh* m, BranchAnchor anchor, float branch_angle, LOD lod)
{
  
  unsigned          ring, segment, segment_count;
  unsigned          radial_steps, radial_edge;
  float             radius;
  float             angle;
  float             horz_pos;
  float             curve;
  GLvector          core;
  GLvector          pos;
  unsigned          base_index;
  GLmatrix          mat;
  GLvector2         uv;  
  vector<GLvector>  underside;

  if (anchor.length < 2.0f)
    return;
  if (anchor.radius < MIN_RADIUS)
    return;
  segment_count = (int)(anchor.length * SEGMENTS_PER_METER);
  segment_count = max (segment_count, MIN_SEGMENTS);
  segment_count += 3;
  base_index = m->_vertex.size ();
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
    radius = max (MIN_RADIUS, anchor.radius * (1.0f - horz_pos));
    core.z = anchor.root.z + anchor.lift * curve * _branch_lift;
    uv.x = 0.0f;
    //if this is the last segment, don't make a ring of points. Make ONE, in the center.
    //This is so the branch can end at a point.
    if (segment== segment_count) {
      pos.x = 0.0f;
      pos.y = anchor.length * horz_pos;
      pos.z = 0.0f;
      pos = glMatrixTransformPoint (mat, pos);
      m->PushVertex (pos + core, glVector (pos.x, 0.0f, pos.z), glVector (0.25f, pos.y * _texture_tile));
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
      m->PushVertex (pos + core, glVector (pos.x, 0.0f, pos.z), glVector (((float)ring / (float) radial_steps) * 0.25f, pos.y * _texture_tile));
    }
    underside.push_back (pos + core);
  }
  //Make the triangles for the branch
  for (segment = 0; segment< segment_count; segment++) {
    for (ring = 0; ring < radial_steps; ring++) {
      if (segment< segment_count - 1) {
        m->PushQuad (base_index + (ring + 0) + (segment+ 0) * (radial_edge),
          base_index + (ring + 0) + (segment+ 1) * (radial_edge),
          base_index + (ring + 1) + (segment+ 1) * (radial_edge),
          base_index + (ring + 1) + (segment+ 0) * (radial_edge));
      } else {//this is the last segment. It ends in a single point
        m->PushTriangle (
          base_index + (ring + 1) + segment* (radial_edge),
          base_index + (ring + 0) + segment* (radial_edge),
          m->Vertices () - 1);
      }
    }
  }
  //Grab the last point and use it as the origin for the foliage
  pos = m->_vertex[m->Vertices () - 1];
  DoFoliage (m, pos, anchor.length * 0.56f, branch_angle);
  //We saved the points on the underside of the branch.
  //Use these to hang vines on the branch
  if (lod == LOD_HIGH)
    DoVines (m, &underside[0], underside.size ());

}

void CTree::DoTrunk (GLmesh* m, unsigned local_seed, LOD lod)
{

  int                   ring, segment, segment_count;
  int                   radial_steps, radial_edge;
  float                 branch_spacing;
  float                 angle;
  float                 radius;
  float                 x, y;
  float                 vertical_pos;
  float                 circumference;
  GLvector              core;
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
    uv.Set (glVector (0.75f, 0.0f), glVector (1.0f, 1.0f));
    height = _current_height;
    width = _current_height / 2.0f;
    //First panel
    m->PushVertex (glVector (-width, -width, 0.0f),   glVector (-width, -width, 0.0f), uv.Corner (0));
    m->PushVertex (glVector ( width,  width, 0.0f),   glVector ( width,  width, 0.0f), uv.Corner (1));
    m->PushVertex (glVector ( width,  width, height), glVector ( width,  width, height), uv.Corner (2));
    m->PushVertex (glVector (-width, -width, height), glVector (-width, -width, height), uv.Corner (3));
    //Second Panel
    m->PushVertex (glVector (-width,  width, 0.0f),   glVector (-width,  width, 0.0f), uv.Corner (0));
    m->PushVertex (glVector ( width, -width, 0.0f),   glVector ( width, -width, 0.0f), uv.Corner (1));
    m->PushVertex (glVector ( width, -width, height), glVector ( width, -width, height), uv.Corner (2));
    m->PushVertex (glVector (-width,  width, height), glVector (-width,  width, height), uv.Corner (3));
    for (i = 0; i < (int)m->_normal.size (); i++) 
      m->_normal[i].Normalize ();
    m->PushQuad (0, 1, 2, 3);
    m->PushQuad (4, 5, 6, 7);
    return;
  }
  //Work out the circumference of the BASE of the tree
  circumference = _current_base_radius * _current_base_radius * (float)PI;
  //The texture will repeat ONCE horizontally around the tree.  Set the vertical to repeat in the same distance.
  _texture_tile = (float)((int)circumference + 0.5f); 
  radial_steps = 3;
  if (lod == LOD_HIGH)
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
      m->PushVertex (core + glVector (x * radius, y * radius, 0.0f),
        glVector (x, y, 0.0f),
        glVector (((float)ring / (float) radial_steps) * 0.25f, core.z * _texture_tile));

    }
    segment_count++;
  }
  //Push one more point, for the very tip of the tree
  m->PushVertex (TrunkPosition (1.0f, NULL), glVector (0.0f, 0.0f, 1.0f), glVector (0.0f, 0.0f));
  //Make the triangles for the main trunk.
  for (segment = 0; segment < segment_count - 1; segment++) {
    for (ring = 0; ring < radial_steps; ring++) {
      m->PushQuad ((ring + 0) + (segment + 0) * (radial_edge),
        (ring + 1) + (segment + 0) * (radial_edge),
        (ring + 1) + (segment + 1) * (radial_edge),
        (ring + 0) + (segment + 1) * (radial_edge));

    }
  }
  
  //Make the triangles for the tip
  for (ring = 0; ring < radial_steps; ring++) {
    m->PushTriangle ((ring + 1) + (segment_count - 1) * radial_edge, m->_vertex.size () - 1,
      (ring + 0) + (segment_count - 1) * radial_edge);
  }
  DoFoliage (m, m->_vertex[m->_vertex.size () - 1] + glVector (0.0f, 0.0f, -2.0f), _current_height / 2, 0.0f);
  if (!_canopy) {
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
  }

}

void CTree::Build ()
{

  unsigned    lod;
  unsigned    alt;

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

void CTree::Create (bool is_canopy, float moisture, float temp_in, int seed_in)
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
  _has_vines = _moisture > 0.7f && _temperature > 0.7f;
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
  //These two foliage styles don't look right on evergreens.
  if (_evergreen && _foliage_style == TREE_FOLIAGE_BOWL)
    _foliage_style = TREE_FOLIAGE_UMBRELLA;
  if (_evergreen && _foliage_style == TREE_FOLIAGE_SHIELD)
    _foliage_style = TREE_FOLIAGE_UMBRELLA;
  if (_evergreen && _foliage_style == TREE_FOLIAGE_PANEL)
    _foliage_style = TREE_FOLIAGE_SAG;
  if (_canopy) {
    _foliage_style = TREE_FOLIAGE_UMBRELLA;
    _default_height = max (_default_height, 16.0f);
    _default_base_radius = 3.0f;
    _foliage_size = 2.0f;
    _trunk_style = TREE_TRUNK_NORMAL;
  }
  Build ();
  DoLeaves ();
  DoTexture ();

}

//Render a single tree. Very slow. Used for debugging. 
void CTree::Render (GLvector pos, unsigned alt, LOD lod)
{

  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBindTexture (GL_TEXTURE_2D, _texture);
  glPushMatrix ();
  glTranslatef (pos.x, pos.y, pos.z);
  _meshes[alt][lod].Render ();
  glPopMatrix ();

}

void CTree::DoLeaves ()
{


  unsigned          i;
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
    GLvector2 delta;
    unsigned  j;

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
      delta = _leaf_list[i].position - glVector (TEXTURE_HALF, TEXTURE_HALF);
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

void CTree::DrawFacer ()
{

  GLbbox    box;
  GLvector  size, center;

  glDisable (GL_BLEND);
  //We get the bounding box for the high-res tree, but we cut off the roots.  No reason to 
  //waste texture pixels on that.
  _meshes[0][LOD_HIGH].RecalculateBoundingBox ();
  box = _meshes[0][LOD_HIGH]._bbox;
  box.pmin.z = 0.0f;//Cuts off roots
  center = box.Center ();
  size = box.Size ();
  //Move our viewpoint to the middle of the texture frame 
  glTranslatef (TEXTURE_HALF, TEXTURE_HALF, 0.0f);
  glRotatef (-90.0f, 1.0f, 0.0f, 0.0f);
  //Scale so that the tree will exactly fill the rectangle
  glScalef ((1.0f / size.x) * TEXTURE_SIZE, 1.0f, (1.0f / size.z) * TEXTURE_SIZE);
  glTranslatef (-center.x, 0.0f, -center.z);
  glColor3f (1,1,1);
  Render (glVector (0.0f, 0.0f, 0.0f), 0, LOD_HIGH);

}

void CTree::DrawVines ()
{

  GLtexture*  t;
  GLuvbox     uvframe;
  int         frames;
  int         frame;
  float       frame_size;
  GLvector2   uv;
  GLrgba      color;

  glColor3fv (&_bark_color1.red);
  glBindTexture (GL_TEXTURE_2D, 0);
  t = TextureFromName ("vines.png");
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  frames = max (t->height / t->width, 1);
  frame_size = 1.0f / (float)frames;
  frame = WorldNoisei (_seed_current++) % frames;
  uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
  glBindTexture (GL_TEXTURE_2D, t->id);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  color = _leaf_color * 0.75f;
  glColor3fv (&_leaf_color.red);
  glBegin (GL_QUADS);
  uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2i (0, 0);
  uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, 0);
  uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, TEXTURE_SIZE);
  uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2i (0, TEXTURE_SIZE);
  glEnd ();


}

void CTree::DrawLeaves ()
{

  GLtexture*  t;
  GLuvbox     uvframe;
  int         frames;
  int         frame;
  float       frame_size;
  GLvector2   uv;
  unsigned    i;

  if (_leaf_style == TREE_LEAF_SCATTER) {
    GLrgba c;

    c = _bark_color1;
    c *= 0.5f;
    glBindTexture (GL_TEXTURE_2D, 0);
    glLineWidth (3.0f);
    glColor3fv (&c.red);

    glBegin (GL_LINES);
    for (i = 0; i < _leaf_list.size (); i++) {
      glVertex2fv (&_leaf_list[_leaf_list[i].neighbor].position.x);
      glVertex2fv (&_leaf_list[i].position.x);
    }
    glEnd ();

  }
  
  Leaf              l;
  //GLrgba            color;
    
  t = TextureFromName ("foliage.png");
  frames = max (t->height / t->width, 1);
  frame_size = 1.0f / (float)frames;
  frame = WorldNoisei (_seed_current++) % frames;
  uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
  glBindTexture (GL_TEXTURE_2D, t->id);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
 	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
 	//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  //glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  for (i = 0; i < _leaf_list.size (); i++) {
    l = _leaf_list[i];
    glPushMatrix ();
    glTranslatef (l.position.x, l.position.y, 0);
    glRotatef (l.angle, 0.0f, 0.0f, 1.0f);
    glTranslatef (-l.position.x, -l.position.y, 0);

    //color = _leaf_color * l.brightness;
    glColor3fv (&l.color.red);
    glBegin (GL_QUADS);
    uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2f (l.position.x - l.size, l.position.y - l.size);
    uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2f (l.position.x + l.size, l.position.y - l.size);
    uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2f (l.position.x + l.size, l.position.y + l.size);
    uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2f (l.position.x - l.size, l.position.y + l.size);
    glEnd ();
    glPopMatrix ();
  }



}

void CTree::DrawBark ()
{

  GLtexture*  t;
  GLuvbox     uvframe;
  int         frames;
  int         frame;
  float       frame_size;
  GLvector2   uv;

  glColor3fv (&_bark_color1.red);
  glBindTexture (GL_TEXTURE_2D, 0);
  glBegin (GL_QUADS);
  glTexCoord2f (0, 0); glVertex2i (0, 0);
  glTexCoord2f (1, 0); glVertex2i (TEXTURE_SIZE, 0);
  glTexCoord2f (1, 1); glVertex2i (TEXTURE_SIZE, TEXTURE_SIZE);
  glTexCoord2f (0, 1); glVertex2i (0, TEXTURE_SIZE);
  glEnd ();
  
  t = TextureFromName ("bark1.bmp");
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  frames = max (t->height / t->width, 1);
  frame_size = 1.0f / (float)frames;
  frame = WorldNoisei (_seed_current++) % frames;
  uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
  glBindTexture (GL_TEXTURE_2D, t->id);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glColorMask (true, true, true, false);
  glColor3fv (&_bark_color2.red);
  glBegin (GL_QUADS);
  uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2i (0, 0);
  uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, 0);
  uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, TEXTURE_SIZE);
  uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2i (0, TEXTURE_SIZE);
  glEnd ();
  glColorMask (true, true, true, true);


}

void CTree::DoTexture ()
{

  unsigned  i;

  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glDisable (GL_LIGHTING);
  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
 	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  if (_texture)
    glDeleteTextures (1, &_texture); 
  glGenTextures (1, &_texture); 
  glBindTexture(GL_TEXTURE_2D, _texture);
  glTexImage2D (GL_TEXTURE_2D, 0, GL_RGBA, TEXTURE_SIZE * 4, TEXTURE_SIZE, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
  RenderCanvasBegin (0, TEXTURE_SIZE, 0, TEXTURE_SIZE, TEXTURE_SIZE);
 	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  for (i = 0; i < 4; i++) {
    glClearColor (0.0f, 0.0f, 0.0f, 0.0f);
    glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    if (i == 0)       
      DrawBark ();
    else if (i == 1)
      DrawLeaves ();
    else if (i == 2)
      DrawVines ();
    else
      DrawFacer ();    
    glBindTexture(GL_TEXTURE_2D, _texture);
 	  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
    glCopyTexSubImage2D (GL_TEXTURE_2D, 0, TEXTURE_SIZE * i, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE);
  }

  RenderCanvasEnd ();
  
}

void CTree::Info ()
{

  TextPrint ("TREE:\nSeed:%d Moisture: %f Temp: %f", _seed, _moisture, _temperature);

}

void CTree::TexturePurge ()
{

  if (_texture)
    DoTexture ();

}