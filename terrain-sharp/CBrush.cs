/*-----------------------------------------------------------------------------
  CBrush.cpp
-------------------------------------------------------------------------------
  This holds the brush object class.  Bushes and the like. 
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include "cache.h"
#include "cbrush.h"
#include "entropy.h"
#include "sdl.h"
#include "Render.h"
#include "texture.h"
#include "world.h"

#define BRUSH_SIZE        32

enum
{
  BRUSH_STAGE_BEGIN,
  BRUSH_STAGE_BUILD,
  BRUSH_STAGE_COMPILE,
  BRUSH_STAGE_DONE,
};


#ifndef GRID
#include "cgrid.h"
#endif

class CBrush : public GridData
{
  GLcoord           _grid_position;
  GLcoord           _origin;
  GLcoord           _walk;
  unsigned          _current_distance;
  //vector<GLrgba>    _color;
  //vector<GLvector>  _vertex;
  //vector<GLvector>  _normal;
  //vector<GLvector2> _uv;
  //vector<UINT>      _index;
  GLmesh            _mesh;
  class VBO         _vbo;
  int               _stage;
  GLbbox            _bbox;
  bool              _valid;

  void              Build (long stop);
  void              VertexPush (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv);
  void              QuadPush (int n1, int n2, int n3, int n4);
  bool              ZoneCheck ();

public:
  CBrush ();
  unsigned          Sizeof () { return sizeof (CBrush); }; 
  void              Set (int origin_x, int origin_y, int distance);
  void              Render ();
  void              Update (long stop);
  bool              Ready ()  { return _stage == BRUSH_STAGE_DONE; };
  void              Invalidate () { _valid = false; };
};

#define BRUSH_TYPES   4
#define MAX_TUFTS     9

struct tuft
{
  GLvector            v[4];
};

static GLuvbox        box[BRUSH_TYPES];
static GLuvbox        box_flower[BRUSH_TYPES];
static bool           prep_done;
static tuft           tuft_list[MAX_TUFTS];

static void do_prep ()
{
  int           i, j;
  GLmatrix      m;
  float         angle_step;

  for (i = 0; i < BRUSH_TYPES; i++) {
    box[i].Set (i, 0, BRUSH_TYPES, 2);
    box[i].lr.y *= 0.99f;
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

CBrush::CBrush () 
{
  GridData ();
  _origin.x = 0;
  _origin.y = 0;
  _current_distance = 0;
  _valid = false;
  _bbox.Clear ();
  _grid_position.Clear ();
  _walk.Clear ();
  _mesh.Clear ();
  _stage = BRUSH_STAGE_BEGIN;
  if (!prep_done) 
    do_prep ();
}

void CBrush::Set (int x, int y, int density)
{
  if (_origin.x == x * BRUSH_SIZE && _origin.y == y * BRUSH_SIZE)
    return;
  _grid_position.x = x;
  _grid_position.y = y;
  _current_distance = density;
  _origin.x = x * BRUSH_SIZE;
  _origin.y = y * BRUSH_SIZE;
  _stage = BRUSH_STAGE_BEGIN;
  _mesh.Clear ();
  _bbox.Clear ();
}

bool CBrush::ZoneCheck ()
{
  if (!CachePointAvailable (_origin.x, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + BRUSH_SIZE, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + BRUSH_SIZE,_origin.y + BRUSH_SIZE))
    return false;
  if (!CachePointAvailable (_origin.x, _origin.y + BRUSH_SIZE))
    return false;
  return true;
}

void CBrush::Build (long stop)
{
  int       world_x, world_y;
  bool      do_tuft;

  world_x = _origin.x + _walk.x;
  world_y = _origin.y + _walk.y;
  do_tuft = CacheSurface (world_x, world_y) == SURFACE_GRASS_EDGE;
  if (do_tuft) {
    GLvector    v[8];
    GLvector    normal;
    GLrgba      color;
    int         current;
    GLvector    root;
    GLvector2   size;
    Region      r;
    float       height;
    int         index;
    int         patch;
    tuft*       this_tuft;
    unsigned    i;

    r = WorldRegionFromPosition (world_x, world_y);
    index = world_x + world_y * BRUSH_SIZE;
    this_tuft = &tuft_list[index % MAX_TUFTS];
    root.x = (float)world_x;
    root.y = (float)world_y;
    root.z = 0.0f;
    height = 0.25f + (r.moisture * r.temperature) * 2.0f;
    size.x = 1.0f + WorldNoisef (index) * 1.0f;
    size.y = 1.0f + WorldNoisef (index) * height;
    size.y = max (size.x, size.y);//Don't let bushes get wider than they are tall
    color = CacheSurfaceColor (world_x, world_y);
    color *= 0.75f;
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
    patch = r.flower_shape[index % FLOWERS] % BRUSH_TYPES;
    current = _mesh.Vertices ();
    normal = CacheNormal (world_x, world_y);
    _mesh.PushVertex (v[0], normal, color, box[patch].Corner (1)); 
    _mesh.PushVertex (v[1], normal, color, box[patch].Corner (1)); 
    _mesh.PushVertex (v[2], normal, color, box[patch].Corner (0)); 
    _mesh.PushVertex (v[3], normal, color, box[patch].Corner (0)); 
    _mesh.PushVertex (v[4], normal, color, box[patch].Corner (2)); 
    _mesh.PushVertex (v[5], normal, color, box[patch].Corner (2)); 
    _mesh.PushVertex (v[6], normal, color, box[patch].Corner (3)); 
    _mesh.PushVertex (v[7], normal, color, box[patch].Corner (3)); 
    _mesh.PushQuad (current, current + 2, current + 6, current + 4);
    _mesh.PushQuad (current + 1, current + 3, current + 7, current + 5);
  }
  if (_walk.Walk (BRUSH_SIZE)) 
    _stage++;
}

void CBrush::Update (long stop)
{
  while (SdlTick () < stop && !Ready ()) {
    switch (_stage) {
    case BRUSH_STAGE_BEGIN:
      if (!ZoneCheck ())
        return;
      _stage++;
    case BRUSH_STAGE_BUILD:
      Build (stop);
      break;
    case BRUSH_STAGE_COMPILE:
      if (_mesh.Vertices ())
        _vbo.Create (&_mesh);
      else
        _vbo.Clear ();
      _stage++;
      _valid = true;
      break;
    }
  }
}

void CBrush::Render ()
{
  //We need at least one successful build before we can draw.
  if (!_valid)
    return;
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  glDisable (GL_CULL_FACE);
  _vbo.Render ();
}
*/