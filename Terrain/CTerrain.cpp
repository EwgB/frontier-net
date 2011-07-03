/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the terrain object class.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "cache.h"
#include "sdl.h"
#include "CTerrain.h"
#include "Render.h"
#include "scene.h"
#include "Texture.h"


//Lower values make the terrain more precise at the expense of more polygons
#define TOLERANCE         0.08f

#define COMPILE_GRID      4
#define COMPILE_SIZE      (TERRAIN_SIZE / COMPILE_GRID)

static struct LayerAttributes
{
  unsigned      texture_frame;
  float         luminance;
  float         opacity;
  float         size;
  SurfaceType   surface;
  SurfaceColor   color;
} layers [] = 
{
  //{"rock.bmp",     1.0f,  1.0f,    3.0f,   SURFACE_ROCK,      SURFACE_COLOR_ROCK},
  
  {7,     0.7f,  0.3f,   1.3f,   SURFACE_SAND,       SURFACE_COLOR_SAND},
  {7,     0.8f,  0.3f,   1.2f,   SURFACE_SAND,       SURFACE_COLOR_SAND},
  {7,     1.0f,  1.0f,   1.1f,   SURFACE_SAND,       SURFACE_COLOR_SAND},

  {7,     0.6f,  1.0f,   1.5f,   SURFACE_SAND_DARK,  SURFACE_COLOR_SAND},


  //{4,    0.0f,  0.5f,   1.6f,   SURFACE_DIRT,       SURFACE_COLOR_BLACK},
  //{4,    0.0f,  0.5f,   1.5f,   SURFACE_DIRT,       SURFACE_COLOR_BLACK},
  {4,    1.0f,  1.0f,   1.4f,   SURFACE_DIRT,       SURFACE_COLOR_DIRT},
  {4,    0.6f,  1.0f,   1.6f,   SURFACE_DIRT_DARK,  SURFACE_COLOR_DIRT},

  {3,  1.0f,  1.0f,   1.6f,     SURFACE_FOREST,     SURFACE_COLOR_DIRT},

  {6,   0.0f,  0.3f,   2.3f,   SURFACE_GRASS_EDGE, SURFACE_COLOR_GRASS},
  {6,   0.0f,  0.5f,   2.2f,   SURFACE_GRASS_EDGE, SURFACE_COLOR_GRASS},
  {6,   0.0f,  0.5f,   2.1f,   SURFACE_GRASS_EDGE, SURFACE_COLOR_GRASS},
  {5,   0.0f,  0.3f,   1.5f,   SURFACE_GRASS,      SURFACE_COLOR_GRASS},
  {5,   0.0f,  0.5f,   1.3f,   SURFACE_GRASS,      SURFACE_COLOR_GRASS},
  {5,   1.0f,  1.0f,   1.2f,   SURFACE_GRASS,      SURFACE_COLOR_GRASS},
  {6,   1.0f,  1.0f,   2.0f,   SURFACE_GRASS_EDGE, SURFACE_COLOR_GRASS},

  {2,    0.0f,  0.3f,   1.9f,   SURFACE_SNOW,       SURFACE_COLOR_SNOW},
  {2,    0.6f,  0.8f,   1.6f,   SURFACE_SNOW,       SURFACE_COLOR_SNOW},
  {2,    0.8f,  0.8f,   1.55f,  SURFACE_SNOW,       SURFACE_COLOR_SNOW},
  {2,    1.0f,  1.0f,   1.5f,   SURFACE_SNOW,       SURFACE_COLOR_SNOW}

};

static bool   bound_ready;
static int    boundary[TERRAIN_SIZE];

/*-----------------------------------------------------------------------------
  //This finds the largest power-of-two denominator for the given number.  This 
  //is used to determine what level of the quadtree a grid position occupies.  
-----------------------------------------------------------------------------*/

static int Boundary (int val)
{

  if (!bound_ready) {
    for (int n = 0; n < TERRAIN_SIZE; n++) {
      boundary[n] = -1;
      if (n == 0)
        boundary[n] = TERRAIN_SIZE;
      else {
        for (int level = TERRAIN_SIZE; level > 1; level /= 2) {
          if (!(n % level)) {
            boundary[n] =  level;
            break;
          }
        }
        if (boundary[n] == -1)
          boundary[n] = 1;
      }
    }
    bound_ready = true;
  }
  return boundary[val];

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

CTerrain::CTerrain ()
{

  GridData ();
  _vertex_list = NULL;
  _normal_list = NULL;
  _uv_list = NULL;
  _index_buffer = NULL;

}


void CTerrain::DoPatch (int patch_z, int patch_y)
{

  float       tile;
  int         x, y;
  int         world_x, world_y;
  GLvector    pos;
  int         stage ;
  GLrgba      col;
  SurfaceType surface;
  int         angle;
  GLrgba      surface_color;
  GLcoord     start, end;
  GLuvbox     uvb;
  GLvector2   uv;

  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  if (_patch_steps > 1) {
    int     texture_step = TERRAIN_SIZE / _patch_steps;
    start.x = _walk.x * texture_step - 3;
    start.y = _walk.y * texture_step - 3;
    end.x = start.x + texture_step + 5;
    end.y = start.y + texture_step + 6;
  } else {
    start.x = start.y = -2;
    end.x = end.y = TERRAIN_EDGE + 2;
  }
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("terrain_rock.png"));
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  for (y = start.y; y < end.y - 1; y++) {
    glBegin (GL_QUAD_STRIP);
    for (x = start.x; x < end.x; x++) {
      world_x = _origin.x + x;
      world_y = _origin.y + y;
      glTexCoord2f ((float)x / 8, (float)y / 8);
      surface_color = CacheSurfaceColor (world_x, world_y, SURFACE_COLOR_ROCK);
      glColor3fv (&surface_color.red);
      glVertex2f ((float)x, (float)y);
      glTexCoord2f ((float)x / 8, (float)(y + 1) / 8);
      surface_color = CacheSurfaceColor (world_x, world_y, SURFACE_COLOR_ROCK);
      glColor3fv (&surface_color.red);
      glVertex2f ((float)x, (float)(y + 1));
    }
    glEnd ();
  }
  for (stage = 0; stage < LAYERS; stage++) {
    //Special layer to give the sand & rock some more depth
    if (stage == 3) {
      glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
      glColor4f (1,1,1, 0.5f);
      glColor3f (1,1,1);
      glBlendFunc (GL_DST_COLOR, GL_SRC_COLOR);
      glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("terrain_shading.png"));
	    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
      glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
      glBegin (GL_QUADS);
      glTexCoord2f (0, 0); glVertex2i (0, 0);
      glTexCoord2f (0, 2); glVertex2i (TERRAIN_SIZE, 0);
      glTexCoord2f (2, 2); glVertex2i (TERRAIN_SIZE, TERRAIN_SIZE);
      glTexCoord2f (2, 0); glVertex2i (0, TERRAIN_SIZE);
      glEnd ();
      glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    }
    if (!_surface_used[layers[stage].surface])
      continue;
    glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("terrain.png"));
    uvb.Set (0, layers[stage].texture_frame, 1, 8);
	  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
    for (y = start.y; y < end.y - 1; y++) {
      for (x = start.x; x < end.x; x++) {
        world_x = _origin.x + x;
        world_y = _origin.y + y;
        surface = CacheSurface (world_x, world_y);
        if (surface != layers[stage].surface)
          continue;
        pos.x = (float)x;
        pos.y = (float)y;
        tile = 0.66f * layers[stage].size; 
        glPushMatrix ();
        glTranslatef (pos.x - 0.5f, pos.y - 0.5f, 0);
        angle = (world_x + world_y * 2) * 25;
        angle %= 360;
        glRotatef ((float)angle, 0.0f, 0.0f, 1.0f);
        glTranslatef (-pos.x, -pos.y, 0);
        if (layers[stage].color == SURFACE_COLOR_BLACK)
          surface_color = glRgba (0.0f);
        else
          surface_color = CacheSurfaceColor (world_x, world_y, layers[stage].color);
        col = surface_color * layers[stage].luminance;
        col.alpha = layers[stage].opacity;
        glColor4fv (&col.red);
        glBegin (GL_QUADS);
        uv = uvb.Corner (0); glTexCoord2fv (&uv.x); glVertex2f (pos.x - tile, pos.y - tile);
        uv = uvb.Corner (1); glTexCoord2fv (&uv.x); glVertex2f (pos.x + tile, pos.y - tile);
        uv = uvb.Corner (2); glTexCoord2fv (&uv.x); glVertex2f (pos.x + tile, pos.y + tile);
        uv = uvb.Corner (3); glTexCoord2fv (&uv.x); glVertex2f (pos.x - tile, pos.y + tile);
        glEnd ();
        glPopMatrix ();
      }
    }
  }




}

void CTerrain::DoTexture ()
{
 if (!_back_texture) {
    glGenTextures (1, &_back_texture); 
    glBindTexture(GL_TEXTURE_2D, _back_texture);
 	  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
    _patch_size = min (RenderMaxDimension (), _texture_desired_size);
    _patch_steps = _texture_desired_size / _patch_size;
    _patch_steps = max (_patch_steps, 1);//Avoid div by zero. Trust me, it's bad.
    glTexImage2D (GL_TEXTURE_2D, 0, GL_RGB, _texture_desired_size, _texture_desired_size, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL);
  }
  RenderCanvasBegin (_walk.x * TERRAIN_PATCH, _walk.x * TERRAIN_PATCH + TERRAIN_PATCH, _walk.y * TERRAIN_PATCH, _walk.y * TERRAIN_PATCH + TERRAIN_PATCH, _patch_size);
  DoPatch (_walk.x, _walk.y);
  glBindTexture(GL_TEXTURE_2D, _back_texture);
  glCopyTexSubImage2D (GL_TEXTURE_2D, 0, _walk.x * _patch_size, _walk.y * _patch_size, 0, 0, _patch_size, _patch_size);
  RenderCanvasEnd ();
  if (_walk.Walk (_patch_steps))
    _stage++;
    
}

void CTerrain::DoHeightmap ()
{

  GLcoord         world;
  GLvector        pos;
  GLvector2       delta;

  world.x = _origin.x + _walk.x;
  world.y = _origin.y + _walk.y;
  _surface_used[CacheSurface (world.x, world.y)] = true;
  pos.x = (float)world.x;
  pos.y = (float)world.y;
  pos.z = CacheElevation (world.x, world.y);
  _pos[_walk.x][_walk.y] = pos;
  _uv[_walk.x][_walk.y] = glVector ((float)_walk.x / TERRAIN_SIZE, (float)_walk.y / TERRAIN_SIZE);
  if (!_walk.x)
    _contour[_walk.x][_walk.y].x = 0;
  else {
    delta.y = 1;
    delta.x = _pos[_walk.x][_walk.y].z - _pos[_walk.x - 1][_walk.y].z;
    _contour[_walk.x][_walk.y].x = _contour[_walk.x - 1][_walk.y].x + glVectorLength (delta);
  }
  if (!_walk.y)
    _contour[_walk.x][_walk.y].y = 0;
  else {
    delta.x = 1;
    delta.y = _pos[_walk.x][_walk.y].z - _pos[_walk.x][_walk.y - 1].z;
    _contour[_walk.x][_walk.y].y = _contour[_walk.x][_walk.y - 1].y + glVectorLength (delta);
    _contour[_walk.x][_walk.y].y = _contour[_walk.x][_walk.y - 1].y + 1;
  }
  _normal[_walk.x][_walk.y] = CacheNormal (world.x, world.y); 
  if (_walk.Walk (TERRAIN_EDGE))
    _stage++;

}

/*-----------------------------------------------------------------------------

  In order to avoid having gaps between adjacent terrains, we have to "stitch"
  them togather.  We analyze the points used along the shared edge, and
  activate any points used by our neighbor.  

-----------------------------------------------------------------------------*/

void CTerrain::DoStitch ()
{

  int          ii;
  CTerrain*    e;
  CTerrain*    s;
  CTerrain*    w;
  CTerrain*    n;
  int          b;

  w = SceneTerrainGet (_grid_position.x - 1, _grid_position.y);
  e = SceneTerrainGet (_grid_position.x + 1, _grid_position.y);
  s = SceneTerrainGet (_grid_position.x, _grid_position.y + 1);
  n = SceneTerrainGet (_grid_position.x, _grid_position.y - 1);
  for (ii = 0; ii < TERRAIN_EDGE; ii++) {
    b = Boundary (ii);
    if (w && w->Point (TERRAIN_SIZE, ii)) {
      PointActivate (0, ii);
      PointActivate (b, ii);
    }
    if (e && e->Point (0, ii)) {
      PointActivate (TERRAIN_SIZE - b, ii);
      PointActivate (TERRAIN_SIZE, ii);
    }
    if (s && s->Point (ii, 0)) {
      PointActivate (ii, TERRAIN_SIZE);
      PointActivate (ii, TERRAIN_SIZE - b);
    }
    if (n && n->Point (ii, TERRAIN_SIZE)) {
      PointActivate (ii, b);
      PointActivate (ii, 0);
    }
  }
  //Now save a snapshot of how many points our neighbors are using. 
  //If these change, they have added detail and we'll need to re-stitch.
  for (ii = 0; ii < NEIGHBOR_COUNT; ii++)
    _neighbors[ii] = 0;
  if (w)
    _neighbors[NEIGHBOR_WEST] = w->Points ();
  if (e)
    _neighbors[NEIGHBOR_EAST] = e->Points ();
  if (n)
    _neighbors[NEIGHBOR_NORTH] = n->Points ();
  if (s)
    _neighbors[NEIGHBOR_SOUTH] = s->Points ();

}


/*-----------------------------------------------------------------------------

  Look at our neighbors and see if they have added detail since our last 
  rebuild.

-----------------------------------------------------------------------------*/

bool CTerrain::DoCheckNeighbors ()
{

  CTerrain*    e;
  CTerrain*    s;
  CTerrain*    w;
  CTerrain*    n;

  w = SceneTerrainGet (_grid_position.x - 1, _grid_position.y);
  e = SceneTerrainGet (_grid_position.x + 1, _grid_position.y);
  s = SceneTerrainGet (_grid_position.x, _grid_position.y + 1);
  n = SceneTerrainGet (_grid_position.x, _grid_position.y - 1);
  if (w && w->Points () != _neighbors[NEIGHBOR_WEST])
    return true;
  if (s && s->Points () != _neighbors[NEIGHBOR_SOUTH])
    return true;
  if (e && e->Points () != _neighbors[NEIGHBOR_EAST])
    return true;
  if (n && n->Points () != _neighbors[NEIGHBOR_NORTH])
    return true;
  return false;

}

/*-----------------------------------------------------------------------------
This is tricky stuff.  When this is called, it means the given point is needed
for the terrain we are working on.  Each point, when activated, will recusivly 
require two other points at the next lowest level of detail.  This is what 
causes the "shattering" effect that breaks the terrain into triangles.  
If you want to know more, Google for Peter Lindstrom, the inventor of this 
very clever system.  
-----------------------------------------------------------------------------*/

void CTerrain::PointActivate (int x, int y)
{

  int           xl;
  int           yl;
  int           level;

  if (x < 0 || x > TERRAIN_SIZE || y < 0 || y > TERRAIN_SIZE)
    return;
  if (Point (x,y))
    return;
  _point[x][y] = true;
  xl = Boundary (x);
  yl = Boundary (y);
  level = min (xl, yl);
  if (xl > yl) {
    PointActivate (x - level, y);
    PointActivate (x + level, y);
  } else if (xl < yl) {
    PointActivate (x, y + level);
    PointActivate (x, y - level);
  } else {
    int     x2;
    int     y2;

    x2 = x & (level * 2);
    y2 = y & (level * 2);
    if (x2 == y2) {
      PointActivate (x - level, y + level);
      PointActivate (x + level, y - level);
    } else {
      PointActivate (x + level, y + level);
      PointActivate (x - level, y - level);
    }
  }

}

/*-----------------------------------------------------------------------------

            upper         
         ul-------ur        
          |\      |      
         l| \     |r     
         e|  \    |i      
         f|   c   |g    
         t|    \  |h         
          |     \ |t         
          |      \|          
         ll-------lr         
            lower            

This considers a quad for splitting. This is done by looking to see how 
coplanar the quad is.  The elevation of the corners are averaged, and compared 
to the elevation of the center.  The geater the difference between these two 
values, the more non-coplanar this quad is.
-----------------------------------------------------------------------------*/

void CTerrain::DoQuad (int x1, int y1, int size)
{

  int       xc, yc, x2, y2;
  int       half;
  float     ul, ur, ll, lr, center;
  float     average; 
  float     delta;

  half = size / 2;
  xc = x1 + half;   x2 = x1 + size;
  yc = y1 + half;   y2 = y1 + size;
  if (x2 > TERRAIN_SIZE || y2 > TERRAIN_SIZE || x1 < 0 || y1 < 0)
    return;
  ul = _pos[x1][y1].z;
  ur = _pos[x2][y1].z;
  ll = _pos[x1][y2].z;
  lr = _pos[x2][y2].z;
  center = _pos[xc][yc].z;
  average = (ul + lr + ll + ur) / 4.0f;
  //look for a delta between the center point and the average elevation
  delta = abs (average - center);
  //scale the delta based on the size of the quad we are dealing with
  delta /= (float)size;
  if (delta > TOLERANCE)
    PointActivate (xc, yc);

}

void CTerrain::TrianglePush (int i1, int i2, int i3)
{

  _index_buffer = (unsigned int*)realloc (_index_buffer, sizeof (int) * (_index_buffer_size + 3));
  _index_buffer[_index_buffer_size] = i1;
  _index_buffer[_index_buffer_size + 1] = i2;
  _index_buffer[_index_buffer_size + 2] = i3;
  _index_buffer_size += 3;

}

/*-----------------------------------------------------------------------------
                          North                 N    
    *-------*           *---+---*           *---*---*     *---+---*
    |\      |           |\     /|           |\Nl|Nr/|     |   |   |
    | \ Sup |           | \   / |           | \ | / |     | A | B |
    |  \    |           |  \ /  |           |Wr\|/El|     |   |   |
    |   \   |       West+   *   +East      W*---*---*E    *---+---*   
    |    \  |           |  / \  |           |Wl/|\Er|     |   |   |
    | Inf \ |           | /   \ |           | / | \ |     | C | D |
    |      \|           |/     \|           |/Sr|Sl\|     |   |   |
    *-------*           *---+---*           *---*---*     *---*---*
                          South                 S      

    Figure a            Figure b            Figure c      Figure d

This takes a single quadtree block and decides how to divide it for rendering. 
If the center point in not included in the mesh (or if there IS no center 
because we are at the lowest level of the tree), then the block is simply 
cut into two triangles. (Figure a)

If the center point is active, but none of the edges, the block is cut into
four triangles.  (Fig. b)  If the edges are active, then the block is cut 
into a combination of smaller triangles (Fig. c) and sub-blocks (Fig. d).   

-----------------------------------------------------------------------------*/

void CTerrain::CompileBlock (int x, int y, int size)
{

  int     x2;
  int     y2;
  int     xc;
  int     yc;
  int     next_size;
  int     n0, n1, n2, n3, n4, n5, n6, n7, n8;

  //Define the shape of this block.  x and y are the upper-left (Northwest)
  //origin, xc and yc define the center, and x2, y2 mark the lower-right 
  //(Southeast) corner, and next_size is half the size of this block.
  next_size = size / 2;
  x2 = x + size;
  y2 = y + size;
  xc = x + next_size;
  yc = y + next_size;
  /*    n0--n1--n2
        |        |
        n3  n4  n5
        |        |
        n6--n7--n8    */
  n0 = _index_map[x][y];
  n1 = _index_map[xc][y];
  n2 = _index_map[x2][y];
  n3 = _index_map[x][yc];
  n4 = _index_map[xc][yc];
  n5 = _index_map[x2][yc];
  n6 = _index_map[x][y2];
  n7 = _index_map[xc][y2];
  n8 = _index_map[x2][y2];
  //If this is the smallest block, or the center is inactive, then just
  //Cut into two triangles as shown in Figure a
  if (size == 1 || !Point (xc, yc)) {
    if ((x / size + y / size) % 2) {
      TrianglePush (n0, n8, n2);
      TrianglePush (n0, n6, n8);
    } else {
      TrianglePush (n0, n6, n2);
      TrianglePush (n2, n6, n8);
    }
    return;
  } 
  //if the edges are inactive, we need 4 triangles (fig b)
  if (!Point (xc, y) && !Point (xc, y2) && !Point (x, yc) && !Point (x2, yc)) {
      TrianglePush (n0, n4, n2);//North
      TrianglePush (n2, n4, n8);//East
      TrianglePush (n8, n4, n6);//South
      TrianglePush (n6, n4, n0);//West
      return;
  }
  //if the top & bottom edges are inactive, it is impossible to have 
  //sub-blocks.
  if (!Point (xc, y) && !Point (xc, y2)) {
    TrianglePush (n0, n4, n2);//North
    TrianglePush (n8, n4, n6);//South
    if (Point (x, yc)) {
      TrianglePush (n3, n4, n0);//Wr
      TrianglePush (n6, n4, n3);//Wl
    } else 
      TrianglePush (n6, n4, n0);//West
    if (Point (x2, yc)) {
      TrianglePush (n2, n4, n5);//El
      TrianglePush (n5, n4, n8);//Er
    } else 
      TrianglePush (n2, n4, n8);//East
    return;
  }
  
  //if the left & right edges are inactive, it is impossible to have 
  //sub-blocks.
  if (!Point (x, yc) && !Point (x2, yc)) {
    TrianglePush (n2, n4, n8);//East
    TrianglePush (n6, n4, n0);//West
    if (Point (xc, y)) {
      TrianglePush (n0, n4, n1);//Nl
      TrianglePush (n1, n4, n2);//Nr
    } else
      TrianglePush (n0, n4, n2);//North
    if (Point (xc, y2)) {
      TrianglePush (n7, n4, n6);//Sr
      TrianglePush (n8, n4, n7);//Sl
    } else
    TrianglePush (n8, n4, n6);//South
    return;
  }
  //none of the other tests worked, which means this block is a combination 
  //of triangles and sub-blocks. Brace yourself, this is not for the timid.
  //the first step is to find out which triangles we need
  if (!Point (xc, y)) {  //is the top edge inactive?
    TrianglePush (n0, n4, n2);//North
    if (Point (x, yc))
      TrianglePush (n3, n4, n0);//Wr
    if (Point (x2, yc))
      TrianglePush (n2, n4, n5);//El
  }
  if (!Point (xc, y2)) {//is the bottom edge inactive?
    TrianglePush (n8, n4, n6);//South
    if (Point (x, yc))
      TrianglePush (n6, n4, n3);//Wl
    if (Point (x2, yc)) 
      TrianglePush (n5, n4, n8);//Er
  }
  if (!Point (x, yc)) {//is the left edge inactive?
    TrianglePush (n6, n4, n0);//West
    if (Point (xc, y))
      TrianglePush (n0, n4, n1);//Nl
    if (Point (xc, y2)) 
      TrianglePush (n7, n4, n6);//Sr
  }
  if (!Point (x2, yc)) {//is the right edge inactive?
    TrianglePush (n2, n4, n8);//East
    if (Point (xc, y))
      TrianglePush (n1, n4, n2);//Nr
    if (Point (xc, y2)) 
      TrianglePush (n8, n4, n7);//Sl
  }
  //now that the various triangles have been added, we add the 
  //various sub-blocks.  This is recursive.
  if (Point (xc, y) && Point (x, yc)) 
    CompileBlock (x, y, next_size); //Sub-block A
  if (Point (xc, y) && Point (x2, yc)) 
    CompileBlock (x + next_size, y, next_size); //Sub-block B
  if (Point (x, yc) && Point (xc, y2)) 
    CompileBlock (x, y + next_size, next_size); //Sub-block C
  if (Point (x2, yc) && Point (xc, y2)) 
    CompileBlock (x + next_size, y + next_size, next_size); //Sub-block D

}

/*-----------------------------------------------------------------------------
  This checks the four corners of zone data that will be used by this terrain.
  Returns true if the data is ready and terrain building can proceed. This
  will also "touch" the zone, letting the zone know it's still in use.
-----------------------------------------------------------------------------*/

bool CTerrain::ZoneCheck (long stop)
{

  //If we're waiting on a zone, give it our update allotment
  if (!CachePointAvailable (_origin.x, _origin.y)) {
    CacheUpdatePage (_origin.x, _origin.y, stop);
    return false;
  }
  if (!CachePointAvailable (_origin.x + TERRAIN_EDGE, _origin.y + TERRAIN_EDGE)) {
    CacheUpdatePage (_origin.x + TERRAIN_EDGE, _origin.y + TERRAIN_EDGE, stop);
    return false;
  }
  if (!CachePointAvailable (_origin.x + TERRAIN_EDGE, _origin.y)) {
    CacheUpdatePage (_origin.x + TERRAIN_EDGE, _origin.y, stop);
    return false;
  }
  if (!CachePointAvailable (_origin.x, _origin.y + TERRAIN_EDGE)) {
    CacheUpdatePage (_origin.x, _origin.y + TERRAIN_EDGE, stop);
    return false;
  }
  return true;

}


void CTerrain::Update (long stop)
{

  while (SdlTick () < stop) {
    switch (_stage) {
    case STAGE_BEGIN: 
      if (!ZoneCheck (stop)) 
        break;
      for (int i =0; i < SURFACE_TYPES; i++)
        _surface_used[i] = false;
      for (int i =0; i < NEIGHBOR_COUNT; i++)
        _neighbors[i] = 0;
      _list_size = 0;
      _walk.Clear ();
      _rebuild = SdlTick ();
      _stage++;
      break;
    case STAGE_CLEAR: 
      _point[_walk.x][_walk.y] = false;
      _index_map[_walk.x][_walk.y] = -1;
      if (_walk.Walk (TERRAIN_EDGE))
        _stage++;
      break;
    case STAGE_DO_COMPILE_GRID: 
      PointActivate (_walk.x * COMPILE_SIZE, _walk.y * COMPILE_SIZE);
      if (_walk.Walk (COMPILE_GRID + 1))
        _stage++;
      break;
    case STAGE_HEIGHTMAP: 
      DoHeightmap ();
      break;
    case STAGE_QUADTREE:
      if (!Point (_walk.x, _walk.y)) {
        int   xx, yy, level;

        xx = Boundary (_walk.x);
        yy = Boundary (_walk.y);
        level = min (xx, yy);
        DoQuad (_walk.x - level, _walk.y - level, level * 2);
      }
      if (_walk.Walk (TERRAIN_SIZE))
        _stage++;
      break;  
    case STAGE_STITCH:
      DoStitch ();
      _stage++;
      break;
    case STAGE_INVENTORY_PREPARE:
      _list_size = 0;
      if (_vertex_list)
        delete _vertex_list;
      if (_normal_list)
        delete _normal_list;
      if (_uv_list)
        delete _uv_list;
      if (_index_buffer)
        delete _index_buffer;
      _index_buffer_size = 0;
      _index_buffer = NULL;
      _vertex_list = NULL;
      _normal_list = NULL;
      _uv_list = NULL;
      _stage++;
      break;  
    case STAGE_INVENTORY:
      if (Point (_walk.x, _walk.y)) 
        _list_size++;
      if (_walk.Walk (TERRAIN_EDGE)) {
        _vertex_list = new GLvector [_list_size];
        _normal_list = new GLvector [_list_size];
        _uv_list = new GLvector2 [_list_size];
        _list_pos = 0;
        _stage++;
      }
      break;  
    case STAGE_BUFFER_LOAD: 
      if (Point (_walk.x, _walk.y)) {
        _vertex_list[_list_pos] = _pos[_walk.x][_walk.y];
        _normal_list[_list_pos] = _normal[_walk.x][_walk.y];
        _uv_list[_list_pos] = glVector ((float)_walk.x / TERRAIN_SIZE, (float)_walk.y / TERRAIN_SIZE);
        _index_map[_walk.x][_walk.y] = _list_pos;
        _list_pos++;
      }
      if (_walk.Walk (TERRAIN_EDGE))
        _stage++;
      break; 
    case STAGE_COMPILE:
      CompileBlock (_walk.x * COMPILE_SIZE, _walk.y * COMPILE_SIZE, COMPILE_SIZE);
      if (_walk.Walk (COMPILE_GRID))
        _stage++;
      break;
    case STAGE_VBO:
      if (_vbo.Ready ())
        _vbo.Clear ();
      _vbo.Create (GL_TRIANGLES, _index_buffer_size, _list_size, _index_buffer, _vertex_list, _normal_list, NULL, _uv_list);
      if (_index_buffer)
        free (_index_buffer);
      _index_buffer = NULL;
      _stage++;
      break;
    case STAGE_TEXTURE: 
      DoTexture ();
      break;
    case STAGE_TEXTURE_FINAL: 
      if (_front_texture) 
        glDeleteTextures (1, &_front_texture); 
      _front_texture = _back_texture;
      _back_texture = 0;
      _texture_current_size = _texture_desired_size;
      _stage++;
      break;
    case STAGE_DONE:
      _valid = true;
      if (SdlTick () < _rebuild) 
        return;
      ZoneCheck (stop);//touch the zones to keep them in memory
      _rebuild = SdlTick () + 1000;
      if (DoCheckNeighbors ())
        _stage = STAGE_QUADTREE;
      return;
    default: //any stages not used end up here, skip it
      _stage++;
      break;

    }
  }

}


/*-----------------------------------------------------------------------------
  De-allocate and cleanup 
-----------------------------------------------------------------------------*/


void CTerrain::Clear ()
{

  if (_front_texture) 
    glDeleteTextures (1, &_front_texture); 
  if (_back_texture) 
    glDeleteTextures (1, &_back_texture); 
  _front_texture = 0;
  _back_texture = 0;
  if (_vertex_list)
    delete _vertex_list;
  if (_normal_list)
    delete _normal_list;
  if (_uv_list)
    delete _uv_list;
  if (_index_buffer)
    delete _index_buffer;
  _index_buffer = NULL;
  _vertex_list = NULL;
  _normal_list = NULL;
  _uv_list = NULL;
  _stage = STAGE_BEGIN;
  _texture_current_size = 0;
  _walk.Clear ();



}

void CTerrain::TextureSize (int size)
{

  //We can't resize in the middle of rendering the texture
  if (_stage == STAGE_TEXTURE || _stage == STAGE_TEXTURE_FINAL)
    return;
  if (size != _texture_current_size) {
    _texture_desired_size = size;
    if (_stage == STAGE_DONE)
      _stage = STAGE_TEXTURE;
  }

}

GLcoord CTerrain::Origin ()
{

  return _origin;

}


void CTerrain::TexturePurge ()
{

  if (_front_texture) 
    glDeleteTextures (1, &_front_texture); 
  if (_back_texture) 
    glDeleteTextures (1, &_back_texture); 
  _front_texture = 0;
  _back_texture = 0;
  _texture_current_size = 0;
  _texture_desired_size = 64;
  if (_stage >= STAGE_TEXTURE) {
    _stage = STAGE_TEXTURE;
    _walk.Clear ();
  }
  _index_buffer = NULL;
  _vertex_list = NULL;
  _normal_list = NULL;
  _uv_list = NULL;
  _list_size = 0;

}

void CTerrain::Set (int grid_x, int grid_y, int distance)
{

  unsigned  sup;

  if (distance > 0)
    distance--;
  distance = min (distance, 4);
  if (grid_x == _grid_position.x && grid_y == _grid_position.y && _current_distance == distance)
    return;
  //If this terrain is now in a new location, we have to kill it entirely
  if (grid_x != _grid_position.x || grid_y != _grid_position.y)
    Clear ();
  _grid_position.x = grid_x;
  _grid_position.y = grid_y;
  _current_distance = distance;
  _origin.x = grid_x * TERRAIN_SIZE;
  _origin.y = grid_y * TERRAIN_SIZE;
  _color = glRgbaUnique (_grid_position.x + _grid_position.y * 16);
  sup = (unsigned)pow (2.0f, (float)distance);
  _texture_desired_size = 2048 / sup;
  _walk.Clear ();
  _list_size = 0;
  _stage = STAGE_BEGIN;

}

void CTerrain::Render ()
{

  if (_front_texture && _valid) {
    glColor3fv (&_color.red);
    glBindTexture (GL_TEXTURE_2D, _front_texture);
    _vbo.Render ();
  }

}