/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the grass object class.  Little bits of grass all over!

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cache.h"
#include "entropy.h"
#include "sdl.h"
#include "CGrass.h"
#include "Render.h"
#include "texture.h"
#include "world.h"

#define GRASS_TYPES   8
#define MAX_TUFTS     9

struct tuft
{
  GLvector            v[4];
};

static GLuvbox        box_grass[GRASS_TYPES];
static GLuvbox        box_flower[GRASS_TYPES];
static bool           prep_done;
static tuft           tuft_list[MAX_TUFTS];

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_prep ()
{

  int           i, j;
  GLmatrix      m;
  float         angle_step;

  for (i = 0; i < GRASS_TYPES; i++) {
    box_grass[i].Set (i, 0, GRASS_TYPES, 2);
    box_flower[i].Set (i, 1, GRASS_TYPES, 2);
  }
  angle_step = 360.0f / MAX_TUFTS;
  for (i = 0; i < MAX_TUFTS; i++) {
    tuft_list[i].v[0] = glVector (-1, -1, 0);
    tuft_list[i].v[1] = glVector ( 1, -1, 0);
    tuft_list[i].v[2] = glVector ( 1,  1, 0);
    tuft_list[i].v[3] = glVector (-1,  1, 0);
    m.Identity ();
    m.Rotate (angle_step * (float)i, 0.0f, 0.0f, 1.0f);
    for (j = 0; j < 4; j++) 
      tuft_list[i].v[j] = m.TransformPoint (tuft_list[i].v[j]);
  }
  prep_done = true;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

CGrass::CGrass () 
{


  GridData ();
  _origin.x = 0;
  _origin.y = 0;
  _current_distance = 0;
  _valid = false;
  _bbox.Clear ();
  _grid_position.Clear ();
  _walk.Clear ();
  _stage = GRASS_STAGE_BEGIN;
  if (!prep_done) 
    do_prep ();

}

void CGrass::Set (int x, int y, int density)
{

  //density = max (density, 1); //detail 0 and 1 are the same level. (Maximum density.)
  density = 1;
  if (_origin.x == x * GRASS_SIZE && _origin.y == y * GRASS_SIZE && density == _current_distance)
    return;
  _grid_position.x = x;
  _grid_position.y = y;
  _current_distance = density;
  _origin.x = x * GRASS_SIZE;
  _origin.y = y * GRASS_SIZE;
  _stage = GRASS_STAGE_BEGIN;
  _color.clear ();
  _vertex.clear ();
  _normal.clear ();
  _uv.clear ();
  _index.clear ();
  _bbox.Clear ();

}

void CGrass::VertexPush (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv)
{

  _vertex.push_back (vert);
  _normal.push_back (normal);
  _color.push_back (color);
  _uv.push_back (uv);
  _bbox.ContainPoint (vert);

}

void CGrass::QuadPush (int n1, int n2, int n3, int n4)
{

  _index.push_back (n1);
  _index.push_back (n2);
  _index.push_back (n3);
  _index.push_back (n4);

}

bool CGrass::ZoneCheck ()
{

  if (!CachePointAvailable (_origin.x, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + GRASS_SIZE, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + GRASS_SIZE,_origin.y + GRASS_SIZE))
    return false;
  if (!CachePointAvailable (_origin.x, _origin.y + GRASS_SIZE))
    return false;
  return true;

}

void CGrass::Build (long stop)
{

  int       world_x, world_y;
  bool      do_grass;

  world_x = _origin.x + _walk.x;
  world_y = _origin.y + _walk.y;
  do_grass = CacheSurface (world_x, world_y) == SURFACE_GRASS;
  if (_walk.x % _current_distance || _walk.y  % _current_distance)
    do_grass = false;
  if (do_grass) {
    GLvector    v[8];
    GLvector    normal;
    GLrgba      color;
    int         current;
    GLvector    root;
    GLvector2   size;
    Region      r;
    float       height;
    int         index;
    bool        do_flower;
    int         patch;
    tuft*       this_tuft;
    //GLmatrix    mat;
    unsigned    i;

    r = WorldRegionFromPosition (world_x, world_y);
    index = world_x + world_y * GRASS_SIZE;
    this_tuft = &tuft_list[index % MAX_TUFTS];
    root.x = (float)world_x + (WorldNoisef (index) -0.5f) * 2.0f;
    root.y = (float)world_y + (WorldNoisef (index) -0.5f) * 2.0f;
    root.z = 0.0f;
    height = 0.05f + r.moisture * r.temperature;
    size.x = 0.4f + WorldNoisef (index) * 0.5f;
    size.y = WorldNoisef (index) * height + (height / 2);
    do_flower = r.has_flowers;
    if (do_flower) //flowers are shorter than grass
      size.y /= 2;
    size.y = max (size.y, 0.3f);
    color = CacheSurfaceColor (world_x, world_y, SURFACE_COLOR_GRASS);
    color.alpha = 1.0f;
    //Now we construct our grass panels
    for (i = 0; i < 4; i++) { 
      v[i] = this_tuft->v[i] * glVector (size.x, size.x, 0.0f);
      v[i + 4] = this_tuft->v[i] * glVector (size.x, size.x, 0.0f);
      v[i + 4].z += size.y;
    }
    for (i = 0; i < 8; i++) {
      v[i] += root;
      v[i].z += CacheElevation (v[i].x, v[i].y);
    }
    patch = r.flower_shape[index % FLOWERS] % GRASS_TYPES;
    current = _vertex.size ();
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
      current = _vertex.size ();
      color = r.color_flowers[index % FLOWERS];
      normal = glVector (0.0f, 0.0f, 1.0f);
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

void CGrass::Update (long stop)
{

  while (SdlTick () < stop && !Ready ()) {
    switch (_stage) {
    case GRASS_STAGE_BEGIN:
      if (!ZoneCheck ())
        return;
      _stage++;
    case GRASS_STAGE_BUILD:
      Build (stop);
      break;
    case GRASS_STAGE_COMPILE:
      if (!_vertex.empty ())
        _vbo.Create (GL_QUADS, _index.size (), _vertex.size (), &_index[0], &_vertex[0], &_normal[0], &_color[0], &_uv[0]);
      else
        _vbo.Clear ();
      _stage++;
      _valid = true;
      break;
    }
  }


}

void CGrass::Render ()
{

  //We need at least one successful build before we can draw.
  if (!_valid)
    return;
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  glDisable (GL_CULL_FACE);
  _vbo.Render ();
  return;
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glDisable (GL_BLEND);
  //glEnable (GL_BLEND);
  //glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  //glDisable (GL_LIGHTING);
  _vbo.Render ();

  glDisable (GL_TEXTURE_2D);
  //glDisable (GL_FOG);
  glDisable (GL_LIGHTING);
  glDepthFunc (GL_EQUAL);
  glEnable (GL_BLEND);
  glBlendFunc (GL_ZERO, GL_SRC_COLOR);
  glBlendFunc (GL_DST_COLOR, GL_SRC_COLOR);
  _vbo.Render ();
  glDepthFunc (GL_LEQUAL);
  if (0) {
    glColor3f (1,0,1);
    _bbox.Render ();
  }
  glEnable (GL_TEXTURE_2D);
  glEnable (GL_LIGHTING);

}
