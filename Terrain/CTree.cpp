/*-----------------------------------------------------------------------------

  CTree.cpp

-------------------------------------------------------------------------------

  

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ctree.h"
#include "math.h"
#include "render.h"
#include "terraform.h"
#include "texture.h"
#include "vbo.h"
#include "world.h"

#define SEGMENTS_PER_METER    0.25f
#define MIN_SEGMENTS          3
#define TEXTURE_SIZE          256
#define TEXTURE_HALF          (TEXTURE_SIZE / 2)
#define MIN_RADIUS            0.3f

static int      ii;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CTree::PushTriangle (int n1, int n2, int n3)
{

  _index.push_back (n1);
  _index.push_back (n2);
  _index.push_back (n3);

}

void CTree::DoFoliage (GLvector pos, float fsize, float angle)
{

  GLuvbox   uv;
  int       base_index;

  pos.z += 1.0f;
  uv.Set (glVector (0.5f, 0.0f), glVector (1.0f, 1.0f));
  base_index = _vertex.size ();


  if (_foliage_style == TREE_FOLIAGE_UMBRELLA || _foliage_style == TREE_FOLIAGE_BOWL) {
    float  tip_height;

    tip_height = fsize / 2.0f;
    if (_foliage_style == TREE_FOLIAGE_BOWL)
      tip_height *= -1.0f;
    _vertex.push_back (glVector (0.0f, 0.0f, 0.0f));
    _normal.push_back (glVector (0.0f, 0.0f, 1.0f));
    _uv.push_back (uv.Center ());

    _vertex.push_back (glVector (-fsize, -fsize, -tip_height));
    _normal.push_back (glVector (-0.5f, -0.5f, 0.0f));
    _uv.push_back (uv.Corner (0));

    _vertex.push_back (glVector (fsize, -fsize, -tip_height));
    _normal.push_back (glVector ( 0.5f, -0.5f, 0.0f));
    _uv.push_back (uv.Corner (1));

    _vertex.push_back (glVector (fsize, fsize, -tip_height));
    _normal.push_back (glVector ( 0.5f, 0.5f, 0.0f));
    _uv.push_back (uv.Corner (2));

    _vertex.push_back (glVector (-fsize, fsize, -tip_height));
    _normal.push_back (glVector ( -0.5f, 0.5f, 0.0f));
    _uv.push_back (uv.Corner (3));
    PushTriangle (base_index, base_index + 2, base_index + 1);
    PushTriangle (base_index, base_index + 3, base_index + 2);
    PushTriangle (base_index, base_index + 4, base_index + 3);
    PushTriangle (base_index, base_index + 1, base_index + 4);
  }
  
  if (_foliage_style == TREE_FOLIAGE_PANEL) {
    GLvector    p;
    GLvector    n;
    //first panel
    p = glVector (-fsize, 0.0f, -fsize);
    n = pos;
    n.Normalize ();
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (0));

    p = glVector (fsize, 0.0f, -fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (1));

    p = glVector (fsize, 0.0f, fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (2));

    p = glVector (-fsize, 0.0f, fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (3));
    //Second panel
    p = glVector (0.0f, -fsize, -fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (0));

    p = glVector (0.0f, fsize, -fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (1));

    p = glVector (0.0f, fsize, fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (2));

    p = glVector (0.0f, -fsize, fsize);
    n = glVectorNormalize (pos);
    _vertex.push_back (p);
    _normal.push_back (n);
    _uv.push_back (uv.Corner (3));




    PushTriangle (base_index, base_index + 1, base_index + 2);
    PushTriangle (base_index, base_index + 2, base_index + 3);
    //return;
    PushTriangle (base_index + 4, base_index + 5, base_index + 6);
    PushTriangle (base_index + 4, base_index + 7, base_index + 6);
  }
  
  if (1) {
    //float     angle;
    GLmatrix  m;
    unsigned  i;
    //angle = MathAngle (pos.x, pos.y, 0.0f, 0.0f);
    angle += 45.0f;
    m.Identity ();
    m.Rotate (angle, 0.0f, 0.0f, 1.0f);
    for (i = base_index; i < _vertex.size (); i++) {
      _vertex[i] = glMatrixTransformPoint (m, _vertex[i]);
      _vertex[i] += pos;
    }

  }

}


void CTree::DoBranch (BranchAnchor anchor, float branch_angle)
{
  
  int           ring, tier, tier_count;
  int           radial_steps, radial_edge;
  float         circumference;
  float         radius;
  float         angle;
  float         horz_pos;
  float         curve;
  GLvector      core;
  GLvector      pos;
  unsigned      base_index;
  GLmatrix      m;

  if (anchor.length < 2.0f)
    return;
  tier_count = (int)(anchor.length * SEGMENTS_PER_METER);
  tier_count = max (tier_count, MIN_SEGMENTS);
  base_index = _vertex.size ();
  m.Identity ();
  m.Rotate (branch_angle, 0.0f, 0.0f, 1.0f);
  circumference = (float)PI * (anchor.radius * anchor.radius);
  radial_steps = (int)(circumference * SEGMENTS_PER_METER);
  radial_steps = max (radial_steps, MIN_SEGMENTS);
  radial_edge = radial_steps + 1;
  core = anchor.root;
  radius = anchor.radius;
  for (tier = 0; tier <= tier_count; tier++) {
    horz_pos = (float)tier / (float)(tier_count);
    curve = horz_pos * horz_pos;
    radius = max (MIN_RADIUS, anchor.radius * (1.0f - horz_pos));
    core.z = anchor.root.z + anchor.lift * curve * _branch_lift;
    for (ring = 0; ring <= radial_steps; ring++) {
      angle = (float)ring * (360.0f / (float)radial_steps);
      angle *= DEGREES_TO_RADIANS;
      pos.x = sin (angle) * radius;
      pos.y = anchor.length * horz_pos;
      pos.z = cos (angle) * radius;
      pos = glMatrixTransformPoint (m, pos);
      _vertex.push_back (pos + core);
      _normal.push_back (glVector (pos.x, 0.0f, pos.z));
      _uv.push_back (glVector (((float)ring / (float) radial_steps) * 0.5f, (float)tier / 3.0f));
    }
  }
  //Make the triangles for the branch
  for (tier = 0; tier < tier_count; tier++) {
    for (ring = 0; ring < radial_steps; ring++) {
      _index.push_back (base_index + (ring + 0) + (tier + 0) * (radial_edge));
      _index.push_back (base_index + (ring + 1) + (tier + 0) * (radial_edge));
      _index.push_back (base_index + (ring + 1) + (tier + 1) * (radial_edge));
      
      _index.push_back (base_index + (ring + 0) + (tier + 0) * (radial_edge));
      _index.push_back (base_index + (ring + 1) + (tier + 1) * (radial_edge));
      _index.push_back (base_index + (ring + 0) + (tier + 1) * (radial_edge));
    }
  }
  pos = glVector (0.0f, anchor.length, 0.0f);
  pos = glMatrixTransformPoint (m, pos);
  DoFoliage (pos + core, anchor.length * 0.96f, branch_angle);

    
}

//static float  nnn;

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
    *radius = _base_radius * (1.0f - delta_curve);
    *radius = max (*radius, MIN_RADIUS);
  }
  bend = delta * delta;
  trunk.x = bend * _trunk_bend;
  trunk.y = 0.0f;
  trunk.z = delta * _height;
  return trunk;
  
}

void CTree::Build (GLvector pos)
{

  int                   ring, tier, tier_count;
  int                   radial_steps, radial_edge;
  float                 angle;
  float                 radius;
  float                 x, y;
  float                 tier_height;
  float                 vertical_pos;
  float                 circumference;
  GLvector              core;
  vector<BranchAnchor>  branch_list;
  BranchAnchor          branch;
  unsigned              i;

  _vertex.clear ();
  _normal.clear ();
  _uv.clear ();
  _index.clear ();

  _funnel_trunk = (WorldNoisei (ii++) % 6) == 0;
  _height = 10.0f + WorldNoisef (ii++) * 12.0f;
  _base_radius = 0.3f + WorldNoisef (ii++) * 2.0f;
  if (_funnel_trunk) {
    _base_radius *= 2.0f;
    _height *= 1.5f;
  }
  _branch_reach = 1.0f + WorldNoisef (ii++) * 0.5f;
  _branch_lift = WorldNoisef (ii++);
  _trunk_style = (TreeTrunkStyle)(WorldNoisei (ii) % TREE_TRUNK_TYPES); 
  _foliage_style = (TreeFoliageStyle)(WorldNoisei (ii++) % TREE_FOLIAGE_TYPES);
  _lowest_branch = 0.5f + WorldNoisef (ii++) * 0.3f;
  _branches = 4 + WorldNoisei (ii++) % 4;
  //_branches = 1;
  _foliage_size = 1.0f;
  _leaf_size = 0.125f;
  _trunk_bend = WorldNoisef (ii++) * _height / 3.0f;
  _bark_color1 = TerraformColorGenerate (SURFACE_COLOR_DIRT, WorldNoisef (ii++), WorldNoisef (ii++), ii++);
  _bark_color2 = TerraformColorGenerate (SURFACE_COLOR_DIRT, WorldNoisef (ii++), WorldNoisef (ii++), ii++);
  _leaf_color = TerraformColorGenerate (SURFACE_COLOR_GRASS, WorldNoisef (ii++), WorldNoisef (ii++), ii++);



  DoTexture ();

  //_height = 15.0f;
  //_base_radius = 2.0f;
  _trunk_style = TREE_TRUNK_NORMAL; 
  //_branch_delta = 0.25f;

  circumference = _base_radius * _base_radius * (float)PI;
  radial_steps = (int)(circumference * SEGMENTS_PER_METER);
  radial_steps = max (radial_steps, MIN_SEGMENTS) + 1;
  radial_edge = radial_steps + 1;
  radius = 1.0f;
  core = glVector (0.0f, 0.0f, 0.0f);
  tier_count = (int)(_height * SEGMENTS_PER_METER);
  tier_count = max (tier_count, MIN_SEGMENTS);
  tier_height = _height / (float)tier_count;
  for (tier = 0; tier < tier_count; tier++) {
    //0.0f is base of tree, 1.0f is top
    vertical_pos = (float)tier / (float)(tier_count);
    //radius = 0.3f + _base_radius * (1.0f - vertical_pos);
    core = TrunkPosition (vertical_pos, &radius);
    for (ring = 0; ring <= radial_steps; ring++) {
      angle = (float)ring * (360.0f / (float)radial_steps);
      angle *= DEGREES_TO_RADIANS;
      x = sin (angle);
      y = cos (angle);
      _vertex.push_back (core + glVector (x * radius, y * radius, 0.0f));
      _normal.push_back (glVector (x, y, 0.0f));
      _uv.push_back (glVector (((float)ring / (float) radial_steps) * 0.5f, core.z / 3.0f));

    }
    /*
    switch (_trunk_style) {
    case TREE_TRUNK_NORMAL:
      break;
    case TREE_TRUNK_JAGGED:
      core.x = tier % 2 ? radius : 0.0f;
      core.y = (tier / 2) % 2 ? radius : 0.0f; break;
    case TREE_TRUNK_BENT:
      core.x = vertical_pos * vertical_pos * _base_radius * 2.0f; break;
    }
    core.z += tier_height;
    */
  }
  //Push one more point, for the very tip of the tree
  _vertex.push_back (TrunkPosition (1.0f, NULL));
  _normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  _uv.push_back (glVector (0.0f, 0.0f));
  //Make the triangles for the main trunk.
  for (tier = 0; tier < tier_count - 1; tier++) {
    for (ring = 0; ring < radial_steps; ring++) {
      _index.push_back ((ring + 0) + (tier + 0) * (radial_edge));
      _index.push_back ((ring + 1) + (tier + 0) * (radial_edge));
      _index.push_back ((ring + 1) + (tier + 1) * (radial_edge));
      
      _index.push_back ((ring + 0) + (tier + 0) * (radial_edge));
      _index.push_back ((ring + 1) + (tier + 1) * (radial_edge));
      _index.push_back ((ring + 0) + (tier + 1) * (radial_edge));
    }
  }
  
  //Make the triangles for the tip
  for (ring = 0; ring < radial_steps; ring++) {
    _index.push_back ((ring + 0) + (tier_count - 1) * radial_edge);
    _index.push_back (_vertex.size () - 1);
    _index.push_back ((ring + 1) + (tier_count - 1) * radial_edge);
  }
  
  //DoFoliage (TrunkPosition (vertical_pos, NULL), vertical_pos * _height);

  //nnn += 15.0f;

  //Determine the branch locations
  float branch_spacing;

  branch_spacing = (0.95f - _lowest_branch) / (float)_branches;
  for (int i = 0; i < _branches; i++) {
    vertical_pos = _lowest_branch + branch_spacing * (float)i;
    branch.root = TrunkPosition (vertical_pos, &branch.radius);
    //branch.length = (_height - branch.root.z) * _branch_reach;
    branch.length = (_height - branch.root.z) * _branch_reach;
    branch.lift = (branch.root.z) / 2;
    branch_list.push_back (branch);
  }
  //Add the branches
  
  for (i = 0; i < branch_list.size (); i++) {
    angle = (float)i * ((360.0f / (float)branch_list.size ()) + 180.0f);
    DoBranch (branch_list[i], angle);
  }
  /*
  for (i = 0; i < branch_list.size (); i++) {
    angle = (float)i * ((360.0f / (float)branch_list.size ()));
    DoFoliage (branch_list[i].root, branch_list[i].length, angle);
  }
  */


  //DEV - move tree to requested origin
  for (i = 0; i < _vertex.size (); i++) 
    _vertex[i] += pos;
  _vbo.Create (GL_TRIANGLES, _index.size (), _vertex.size (), &_index[0], &_vertex[0], &_normal[0], NULL, &_uv[0]);
  _polygons = _index.size ();
  ii++;

}

void CTree::Render ()
{

  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBindTexture (GL_TEXTURE_2D, _texture);
  //glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("tree.bmp"));
  _vbo.Render ();

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

  total_steps = 5;
  current_steps = (float)total_steps;
  for (current_steps = (float)total_steps; current_steps >= 1.0f; current_steps -= 1.0f) {
    size = (TEXTURE_HALF / 2) / (1.0f + ((float)total_steps - current_steps));
    radius = (TEXTURE_SIZE - size * 2.0f);
    circ = (float)PI * radius * 2;
    step_size = 360.0f / current_steps;
    for (x = 0.0f; x < 360.0f; x += step_size) {
      rad = x * DEGREES_TO_RADIANS;
      l.size = size;
      l.position.x = TEXTURE_HALF + sin (rad) * l.size;
      l.position.y = TEXTURE_HALF + cos (rad) * l.size;
      l.angle = -MathAngle (TEXTURE_HALF, TEXTURE_HALF, l.position.x, l.position.y);
      l.brightness = 1.0f - (current_steps / (float)total_steps) * WorldNoisef (ii++) * 0.5f;
      _leaf_list.push_back (l);
    }
  }
  for (i = 0; i < _leaf_list.size (); i++) 
    _leaf_list[i].position.x += TEXTURE_SIZE;//Move to the right side of our texture




}

void CTree::DoTexture ()
{

  GLtexture*  t;
  GLuvbox     uvframe;
  int         frames;
  int         frame;
  float       frame_size;
  GLvector2   uv;

  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  if (!_texture) {
    glGenTextures (1, &_texture); 
    glBindTexture(GL_TEXTURE_2D, _texture);
    glTexImage2D (GL_TEXTURE_2D, 0, GL_RGBA, TEXTURE_SIZE * 2, TEXTURE_SIZE, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
  }
  RenderCanvasBegin (0, TEXTURE_SIZE * 2, 0, TEXTURE_SIZE * 2, TEXTURE_SIZE * 2);
  glClearColor (0.0f, 0.0f, 0.0f, 0.0f);
  glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
	//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  //glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	


  //glColor3f (0.45f, 0.3f, 0.0f);
  glColor3fv (&_bark_color1.red);
  glBindTexture (GL_TEXTURE_2D, 0);
  glBegin (GL_QUADS);
  glTexCoord2f (0, 0); glVertex2i (0, 0);
  glTexCoord2f (1, 0); glVertex2i (TEXTURE_SIZE, 0);
  glTexCoord2f (1, 1); glVertex2i (TEXTURE_SIZE, TEXTURE_SIZE);
  glTexCoord2f (0, 1); glVertex2i (0, TEXTURE_SIZE);
  glEnd ();
  
  t = TextureFromName ("bark1.bmp", MASK_LUMINANCE);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  frames = max (t->height / t->width, 1);
  frame_size = 1.0f / (float)frames;
  frame = WorldNoisei (ii++) % frames;
  uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
  glBindTexture (GL_TEXTURE_2D, t->id);
  //glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBlendFunc (GL_SRC_COLOR, GL_ZERO);
  glColorMask (true, true, true, false);
  //glColor3f (0.7f, 0.6f, 0.4f);
  glColor3fv (&_bark_color2.red);
  glBegin (GL_QUADS);
  uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2i (0, 0);
  uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, 0);
  uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE, TEXTURE_SIZE);
  uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2i (0, TEXTURE_SIZE);
  glEnd ();
  glColorMask (true, true, true, true);
  /*
  glBindTexture (GL_TEXTURE_2D, 0);
  glDisable (GL_BLEND);
  glColor3f (0.7f, 0.0f, 0.99f);
  glBegin (GL_QUADS);
  uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE + 10, 10);
  uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE * 2 - 10, 10);
  uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE * 2 - 10, TEXTURE_SIZE - 10);
  uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2i (TEXTURE_SIZE + 10, TEXTURE_SIZE - 10);
  glEnd ();
  */

  
  if (1) {
    
    //GLvector          pos;
    //float             tile;
    unsigned          i;
    //vector<Leaf>      leaves;
//    unsigned          leaf_count;
    //float             fade;
    Leaf              l;
    //GLrgba            leaf_color;
    GLrgba            color;
    /*
    t = TextureFromName ("foliage1.bmp");
    glBindTexture (GL_TEXTURE_2D, t->id);
    glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    frames = max (t->height / t->width, 1);
    frame_size = 1.0f / (float)frames;
    frame = WorldNoisei (ii++) % frames;
    uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
    leaf_color = glRgba (0.3f, 0.6f, 0.0f);
    leaf_count = 32;
    for (i = 0; i < leaf_count; i++) {
      fade = (float)i / (float)leaf_count;
      l.size = TEXTURE_SIZE * _leaf_size;
      l.size = l.size - WorldNoisef(ii++) * (0.5f + fade * 0.5f) * l.size;
      if (i) {
        l.position.x = TEXTURE_HALF + ((WorldNoisef(ii++) - 0.5f) * (TEXTURE_SIZE - l.size * 2.0f));
        l.position.y = TEXTURE_HALF + ((WorldNoisef(ii++) - 0.5f) * (TEXTURE_SIZE - l.size * 2.0f));
        l.angle = -MathAngle (TEXTURE_HALF, TEXTURE_HALF, l.position.x, l.position.y);
      } else {
        l.position.x = TEXTURE_HALF;
        l.position.y = TEXTURE_HALF;
        l.angle = 0.0f;
      }
      //l.position.x = (float)((i % 8) * 32);
      //l.angle = -MathAngle (TEXTURE_HALF, TEXTURE_HALF, l.position.x, 0);
      l.brightness = 0.25f + fade * 0.75f;
      _leaf_list.push_back (l);
    }
    */
    DoLeaves ();
    
    /*
    l.position = glVector (384, 128);
    l.angle = 0.0f;
    l.brightness = 1.0f;
    l.size = 32.0f;
    leaves.push_back (l);
    */
    glBindTexture (GL_TEXTURE_2D, 0);

    
    /*
    for (i = 0; i < _leaf_list.size (); i++) {
      unsigned    j;
      float       nearest;
      float       consider;
      GLvector2   delta;

      nearest = 9999.9f;
      _leaf_list[i].neighbor = 0;
      for (j = 0; j < _leaf_list.size (); j++) {
        if (j == i)
          continue;
        delta.x = abs (_leaf_list[i].position.x - _leaf_list[j].position.x);
        delta.y = abs (_leaf_list[i].position.y - _leaf_list[j].position.y);
        consider = delta.Length ();
        if (consider < nearest) {
          nearest = consider;
          _leaf_list[i].neighbor = j;
        }
      }

    }

    for (i = 0; i < _leaf_list.size (); i++) 
      _leaf_list[i].position.x += TEXTURE_SIZE;//Move to the right side of our texture

      */


    glLineWidth (5.0f);
    glColor3f (0.45f, 0.3f, 0.0f);
    //glBegin (GL_LINES);
    /*
    for (i = 0; i < _leaf_list.size (); i++) {
      //glVertex2f (TEXTURE_SIZE + TEXTURE_HALF, TEXTURE_HALF);
      glVertex2fv (&_leaf_list[_leaf_list[i].neighbor].position.x);
      glVertex2fv (&_leaf_list[i].position.x);
    }
    glEnd ();

    glLineWidth (3.0f);
    glColor3f (0.75f, 0.5f, 0.2f);
    glBegin (GL_LINES);
    for (i = 0; i < _leaf_list.size (); i++) {
      //glVertex2f (TEXTURE_SIZE + TEXTURE_HALF, TEXTURE_HALF);
      glVertex2fv (&_leaf_list[_leaf_list[i].neighbor].position.x);
      glVertex2fv (&_leaf_list[i].position.x);
    }
    glEnd ();
    */
    t = TextureFromName ("foliage1.bmp");
    frames = max (t->height / t->width, 1);
    frame_size = 1.0f / (float)frames;
    frame = WorldNoisei (ii++) % frames;
    uvframe.Set (glVector (0.0f, (float)frame * frame_size), glVector (1.0f, (float)(frame + 1) * frame_size));
    glBindTexture (GL_TEXTURE_2D, t->id);
    glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    for (i = 0; i < _leaf_list.size (); i++) {
      l = _leaf_list[i];
      glPushMatrix ();
      glTranslatef (l.position.x, l.position.y, 0);
      glRotatef (l.angle, 0.0f, 0.0f, 1.0f);
      glTranslatef (-l.position.x, -l.position.y, 0);

      //col = surface_color * layers[stage].luminance;/
      //glColor4fv (&col.red);
      color = _leaf_color * l.brightness;
      glColor3fv (&color.red);
      glBegin (GL_QUADS);
      uv = uvframe.Corner (0); glTexCoord2fv (&uv.x); glVertex2f (l.position.x - l.size, l.position.y - l.size);
      uv = uvframe.Corner (1); glTexCoord2fv (&uv.x); glVertex2f (l.position.x + l.size, l.position.y - l.size);
      uv = uvframe.Corner (2); glTexCoord2fv (&uv.x); glVertex2f (l.position.x + l.size, l.position.y + l.size);
      uv = uvframe.Corner (3); glTexCoord2fv (&uv.x); glVertex2f (l.position.x - l.size, l.position.y + l.size);
      glEnd ();
      glPopMatrix ();
    }
    
  }

    

  glBindTexture(GL_TEXTURE_2D, _texture);
  glCopyTexSubImage2D (GL_TEXTURE_2D, 0, 0, 0, 0, 0, TEXTURE_SIZE * 2, TEXTURE_SIZE);
  RenderCanvasEnd ();


}