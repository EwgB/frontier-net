/*-----------------------------------------------------------------------------

  Scene.cpp


-------------------------------------------------------------------------------

  This manages all the various objects that need to be created, rendered,
  and deleted at various times. If it gets drawn, and if there's more than 
  one of it, then it should go here.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "camera.h"
#include "cache.h"
#include "cgrass.h"
#include "cterrain.h"
#include "ctree.h"
#include "input.h"
#include "math.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "water.h"
#include "world.h"

#define GRASS_GRID      5
#define GRASS_HALF      (GRASS_GRID / 2)
#define RENDER_DISTANCE 10
#define TERRAIN_GRID    (WORLD_SIZE_METERS / TERRAIN_SIZE)

static CTerrain*        terrain[TERRAIN_GRID][TERRAIN_GRID];
static CGrass           grass[GRASS_GRID][GRASS_GRID];
static GLcoord          terrain_walk;
static GLcoord          grass_walk;
static CTree            tree;
static int              dist_table[RENDER_DISTANCE + 1][RENDER_DISTANCE + 1];
static int              cached;
static int              texture_bytes;
static int              texture_bytes_counter;
static int              polygons;
static int              polygons_counter;

/* Static Functions *************************************************************/

static int res[]={1024, 1024, 512, 256, 256, 128, 128};

static int resolution (int dist)
{

  dist = min (dist, (sizeof (res) / sizeof (int)) - 1);
  return res[dist];

}

static void terrain_update (int x, int y, int dist, long stop)
{

  int     res;


  if (x < 0 || x >= TERRAIN_GRID || y < 0 || y >= TERRAIN_GRID)
    return;
  res = resolution (dist);
  if (terrain[x][y] == NULL) {
    terrain[x][y] = new CTerrain;
    terrain[x][y]->Set (x, y, res);
    cached++;
  }
  terrain[x][y]->TextureSize (res);
  terrain[x][y]->Update (stop);

}

/* Module Functions *************************************************************/

void SceneClear ()
{
  
  int           x, y;

  CachePurge ();
  for (x = 0; x < TERRAIN_GRID; x++) {
    for (y = 0; y < TERRAIN_GRID; y++) {
      if (terrain[x][y]) 
        delete terrain[x][y];
      terrain[x][y] = NULL;
    }
  }


}

void SceneGenerate ()
{

  GLvector    camera;
  GLcoord     current;
  int         x, y;

  SceneClear ();
  WorldGenerate ();
  WaterBuild ();
  camera = CameraPosition ();
  current.x = (int)(camera.x) / GRASS_SIZE;
  current.y = (int)(camera.y) / GRASS_SIZE;
  for (y = 0; y < GRASS_GRID; y++) {
    for (x = 0; x < GRASS_GRID; x++) {
      grass[x][y].Invalidate (); 
      grass[x][y].Set (current.x + x - GRASS_HALF, current.y + y - GRASS_HALF, 1);
    }
  }
  grass_walk.Clear ();

}


void SceneTexturePurge ()
{

  int           x, y;

  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      if (terrain[x][y]) 
        terrain[x][y]->TexturePurge ();
    }
  }

}


CTerrain* SceneTerrainGet (int x, int y)
{

  if (x < 0 || x >= WORLD_GRID || y < 0 || y >= WORLD_GRID)
    return NULL;
  return terrain[x][y];

}

void SceneInit ()
{

  int         x, y;

  //Fill in a table so we can quickly look up distances on a grid
  for (y = 0; y <= RENDER_DISTANCE; y++) {
    for (x = 0; x <= RENDER_DISTANCE; x++) {
      dist_table[x][y] = (int)MathDistance (0, 0, (float)x, (float)y);
    }
  }
  SceneGenerate ();
  grass_walk.Clear ();

}

static GLvector last_tree;

void SceneUpdate (long stop)
{

  int           x, y;
  GLcoord       current;
  GLcoord       gpos;
  GLvector      camera;
  int           offset;
  int           size;
  int           density;

  if (InputKeyPressed (SDLK_F11))
    SceneGenerate ();


  TextPrint ("%d Terrains cached: %s\nTexture memory: %s\n%d Terrain triangles", cached, TextBytes (cached * sizeof (CTerrain)), TextBytes (texture_bytes), polygons);
  TextPrint ("Tree has %d polys.", tree.Polygons ());
  camera = CameraPosition ();
  current.x = (int)(camera.x) / GRASS_SIZE;
  current.y = (int)(camera.y) / GRASS_SIZE;

  if (InputKeyPressed (SDLK_t)) {
    GLvector  apos = AvatarPosition ();
    apos.x -= 4.0f;
    apos.z = CacheElevation (apos.x, apos.y);
    tree.Build (apos);
    last_tree = apos;
  }
  if (InputKeyPressed (SDLK_r)) 
    tree.Build (last_tree);



  /*
  for (x = 0; x < GRASS_GRID; x++) {
    for (y = 0; y < GRASS_GRID; y++) {
      gpos = grass[x][y].Position ();
      density = max (abs (gpos.x - current.x), abs (gpos.y - current.y));
      if (current.x - gpos.x > GRASS_HALF)
        gpos.x += GRASS_GRID;
      if (gpos.x - current.x > GRASS_HALF)
        gpos.x -= GRASS_GRID;
      if (current.y - gpos.y > GRASS_HALF)
        gpos.y += GRASS_GRID;
      if (gpos.y - current.y > GRASS_HALF)
        gpos.y -= GRASS_GRID;
      grass[x][y].Set (gpos.x, gpos.y, density);
      grass[x][y].Update (stop);

    }
  }
  */
  gpos = grass[grass_walk.x][grass_walk.y].Position ();
  density = max (abs (gpos.x - current.x), abs (gpos.y - current.y));
  if (current.x - gpos.x > GRASS_HALF)
    gpos.x += GRASS_GRID;
  if (gpos.x - current.x > GRASS_HALF)
    gpos.x -= GRASS_GRID;
  if (current.y - gpos.y > GRASS_HALF)
    gpos.y += GRASS_GRID;
  if (gpos.y - current.y > GRASS_HALF)
    gpos.y -= GRASS_GRID;
  grass[grass_walk.x][grass_walk.y].Set (gpos.x, gpos.y, density);
  grass[grass_walk.x][grass_walk.y].Update (stop);
  if (grass[grass_walk.x][grass_walk.y].Ready ())
    grass_walk.Walk (GRASS_GRID);



  current.x = (int)(camera.x) / TERRAIN_SIZE;
  current.y = (int)(camera.y) / TERRAIN_SIZE;
  //Always update the terrain beneath us first.
  terrain_update (current.x, current.y, 0, stop);
  //Now update the ones around us, working our way outward.
  for (y = 1; y <= RENDER_DISTANCE; y++) {
    for (x = -y; x < y; x++) {
      terrain_update (current.x + x, current.y - y, y, stop);
      terrain_update (current.x + y, current.y + x, y, stop);
      terrain_update (current.x - x, current.y + y, y, stop);
      terrain_update (current.x - y, current.y - x, y, stop);
    }
    if (SdlTick () >= stop)
      break;
  }
  //Now look for terrains to release
  for (int i = 0; i < WORLD_GRID / 2; i++) {
    if (terrain[terrain_walk.x][terrain_walk.y]) {
      size = terrain[terrain_walk.x][terrain_walk.y]->TextureSizeGet ();
      texture_bytes_counter += size * size * 3;
      polygons_counter += terrain[terrain_walk.x][terrain_walk.y]->Polygons ();
      offset = max (abs (current.x - terrain_walk.x), abs (current.y - terrain_walk.y));
      if (offset > (RENDER_DISTANCE + 1)) {
        terrain[terrain_walk.x][terrain_walk.y]->Clear ();
        cached--;
        delete terrain[terrain_walk.x][terrain_walk.y];
        terrain[terrain_walk.x][terrain_walk.y] = NULL;
      }
    }
    if (terrain_walk.Walk (WORLD_GRID)) {
      texture_bytes = texture_bytes_counter;
      texture_bytes_counter = 0;
      polygons = polygons_counter;
      polygons_counter = 0;
    }
  }

}


void SceneRender ()
{

  int           x, y;
  GLcoord       current, start, end;
  GLvector      camera;
  int           dist;

  camera = CameraPosition ();
  current.x = (int)(camera.x) / TERRAIN_SIZE;
  current.y = (int)(camera.y) / TERRAIN_SIZE;
  start.x = max (current.x - RENDER_DISTANCE, 0); 
  start.y = max (current.y - RENDER_DISTANCE, 0);
  end.x = min (current.x + RENDER_DISTANCE, TERRAIN_GRID - 1); 
  end.y = min (current.y + RENDER_DISTANCE, TERRAIN_GRID - 1);

  glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
  glPolygonMode(GL_FRONT, GL_FILL);
  glPolygonMode(GL_BACK, GL_LINE);
  glDisable (GL_BLEND);
  glEnable(GL_TEXTURE_2D);
  glColor3f (1,1,1);

  for (x = start.x; x <= end.x; x++) {
    for (y = start.y; y <= end.y; y++) {
      dist = dist_table[abs (x - current.x)][abs (y - current.y)];
      if (terrain[x][y] && dist < RENDER_DISTANCE) {
        terrain[x][y]->Render ();
      }
    }
  }
  GLtexture*    t;

  t = TextureFromName ("g3.bmp", MASK_PINK);
  glBindTexture (GL_TEXTURE_2D, t->id);
  for (x = 0; x < GRASS_GRID; x++) {
    for (y = 0; y < GRASS_GRID; y++) {
      grass[x][y].Render ();
    }
  }


  WaterRender ();
  glDisable (GL_BLEND);
  t = TextureFromName ("tree.bmp", MASK_PINK);
  glBindTexture (GL_TEXTURE_2D, t->id);
  tree.Render ();
  /*
  return;
  
  glColor3f (0.4f, 0.7f, 1.0f);
  glNormal3f (0, 0, 1);
  glDisable (GL_BLEND);
  glBindTexture (GL_TEXTURE_2D, 0);
  for (y = 0; y < WORLD_GRID - 2; y++) {
    glBegin (GL_QUAD_STRIP);
    for (x = 0; x < WORLD_GRID - 1; x++) {
      glVertex3fv (&water[x][y].x);
      glVertex3fv (&water[x][y + 1].x);
    }
    glEnd ();
  }


  return;

  GLrgba    col;

  //glEnable (GL_BLEND);
  glDisable(GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  //glBlendFunc (GL_ONE, GL_ONE);
  glPolygonMode(GL_FRONT, GL_LINE);
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      if (terrain[x][y]) {
        col = glRgbaUnique (x + y * 33);
        glColor3fv (&col.red);
        //glColor3f (1,0,1);
        terrain[x][y]->Render ();
      }
    }
  }
  glPolygonMode(GL_FRONT, GL_FILL);

  */


}

void SceneRenderDebug (int style)
{

  GLrgba      col;
  int         x, y;
  GLcoord     pos;
  Region      r;

  glEnable (GL_BLEND);
  glDisable(GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  glBlendFunc (GL_ONE, GL_ONE);
  glPolygonMode(GL_FRONT, GL_LINE);
  glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      if (terrain[x][y]) {
        pos = terrain[x][y]->Origin ();
        r = WorldRegionFromPosition (pos.x, pos.y);
        switch (style) {
        case DEBUG_RENDER_UNIQUE:
          col = glRgbaUnique (1 + x + y * 34); break;
        case DEBUG_RENDER_MOIST:
          col = glRgba (1.0f - r.moisture, r.moisture, 0.0f); break;
        case DEBUG_RENDER_TEMP:
          col = glRgba (r.temperature, 0.0f, 1.0f - r.temperature); break;
        default:
          col = glRgba (1.0f);
        }
        glColor3fv (&col.red);
        //glColor3f (1,0,1);
        terrain[x][y]->Render ();
      }
    }
  }
  glPolygonMode(GL_FRONT, GL_FILL);


}