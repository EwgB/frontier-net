/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the grass object class.  Little bits of grass all over!

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "cache.h"
#include "entropy.h"
#include "sdl.h"
#include "CGrass.h"
//#include "random.h"
#include "Render.h"
#include "texture.h"
#include "world.h"

#define GRASS_TYPES   4

struct uvframe
{
  GLvector2     ul; //upper left
  GLvector2     br; //bottom right
  GLvector2     UpperLeft () { return ul;};
  GLvector2     UpperRight () { return glVector (br.x, ul.y);};
  GLvector2     BottomRight () { return br;};
  GLvector2     BottomLeft () { return glVector (ul.x, br.y);};
};

static uvframe        grass[GRASS_TYPES];
static uvframe        flowers[GRASS_TYPES];
static bool           uv_done;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/
 /*(
static GLvector random_normal ()
{

  GLvector  normal;

  normal = glVector (RandomFloat () - 0.5f, RandomFloat () - 0.5f, 0.5f); 
  return glVectorNormalize (normal);

}*/

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

CGrass::CGrass () 
{

  _origin.x = 0;
  _origin.y = 0;
  _density = 0;
  _bbox.Clear ();
  _position.Clear ();
  _walk.Clear ();
  _stage = GRASS_STAGE_BEGIN;
  if (uv_done) 
    return;
  for (int i = 0; i < GRASS_TYPES; i++) {
    grass[i].br = glVector (0.5f, (float)i / GRASS_TYPES);
    grass[i].ul = glVector (0.0f, (float)i / GRASS_TYPES + 1.0f / GRASS_TYPES);
    flowers[i].br = grass[i].br + glVector (0.5f, 0.0f);
    flowers[i].ul = grass[i].ul + glVector (0.5f, 0.0f);
  }
  uv_done = true;

}

void CGrass::Set (int x, int y, int density)
{

  density = max (density, 1); //detail 0 and 1 are the same level. (Maximum density.)
  if (_origin.x == x * GRASS_SIZE && _origin.y == y * GRASS_SIZE && density == _density)
    return;
  _position.x = x;
  _position.y = y;
  _density = density;
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
  if (_walk.x % _density || _walk.y  % _density)
    do_grass = false;
  //if ((_walk.x + _walk.y) % 2)
    //do_grass = false;
  if (do_grass) {
    GLvector    vb0, vb1, vb2, vb3;
    GLvector    vt0, vt1, vt2, vt3;
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

    r = WorldRegionFromPosition (world_x, world_y);
    index = _vertex.size ();
    root.x = (float)world_x + (WorldNoisef (world_x + world_y * GRASS_SIZE) -0.5f) * 2.0f;
    root.y = (float)world_y + (WorldNoisef (world_x + world_y * GRASS_SIZE) -0.5f) * 2.0f;
    root.z = CacheElevation (root.x, root.y);
    height = 0.1f + r.moisture * r.temperature;
    size.x = 0.2f + WorldNoisef (world_x - world_y * GRASS_SIZE) * 0.5f;
    size.y = WorldNoisef (world_x + world_y * GRASS_SIZE) * height + height;
    do_flower = r.has_flowers;
    if (do_flower) //flowers are shoter than grass
      size.y /= 2;
    size.y = max (size.y, 0.3f);
    color = CacheSurfaceColor (world_x, world_y, SURFACE_COLOR_GRASS);
    color.alpha = 1.0f;
    //Now we construct our grass panels
    vb0.x = root.x - size.x * -1; vb0.y = root.y - size.x * -1; vb0.z = CacheElevation (vb0.x, vb0.y);
    vb1.x = root.x - size.x *  1; vb1.y = root.y - size.x * -1; vb1.z = CacheElevation (vb1.x, vb1.y);
    vb2.x = root.x - size.x *  1; vb2.y = root.y - size.x *  1; vb2.z = CacheElevation (vb2.x, vb2.y);
    vb3.x = root.x - size.x * -1; vb3.y = root.y - size.x *  1; vb3.z = CacheElevation (vb3.x, vb3.y);
    vt0 = vb0 + glVector (0.0f, 0.0f, size.y);
    vt1 = vb1 + glVector (0.0f, 0.0f, size.y);
    vt2 = vb2 + glVector (0.0f, 0.0f, size.y);
    vt3 = vb3 + glVector (0.0f, 0.0f, size.y);
    patch = r.flower_shape[index % FLOWERS] % GRASS_TYPES;

    current = _vertex.size ();
    normal = CacheNormal (world_x, world_y);
    VertexPush (vb0, normal, color, grass[patch].BottomLeft ());
    VertexPush (vb1, normal, color, grass[patch].BottomLeft ());
    VertexPush (vb2, normal, color, grass[patch].BottomRight ());
    VertexPush (vb3, normal, color, grass[patch].BottomRight ());
    VertexPush (vt0, normal, color, grass[patch].UpperLeft ());
    VertexPush (vt1, normal, color, grass[patch].UpperLeft ());
    VertexPush (vt2, normal, color, grass[patch].UpperRight ());
    VertexPush (vt3, normal, color, grass[patch].UpperRight ());
    QuadPush (current, current + 2, current + 6, current + 4);
    QuadPush (current + 1, current + 3, current + 7, current + 5);
    if (do_flower) {
      current = _vertex.size ();
      color = r.color_flowers[index % FLOWERS];
      normal = glVector (0.0f, 0.0f, 1.0f);
      VertexPush (vt0, normal, color, flowers[patch].UpperLeft ());
      VertexPush (vt1, normal, color, flowers[patch].UpperRight ());
      VertexPush (vt2, normal, color, flowers[patch].BottomRight ());
      VertexPush (vt3, normal, color, flowers[patch].BottomLeft ());
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
      break;
    }
  }


}

void CGrass::Render ()
{

  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  //glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  glDisable (GL_CULL_FACE);
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
  glEnable (GL_FOG);

}
