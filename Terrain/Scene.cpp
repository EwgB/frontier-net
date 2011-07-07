/*-----------------------------------------------------------------------------

  Scene.cpp


-------------------------------------------------------------------------------

  This manages all the various objects that need to be created, rendered,
  and deleted at various times. If it gets drawn, and if there's more than 
  one of it, then it should go here.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "cache.h"
#include "cforest.h"
#include "cg.h"
#include "cgrass.h"
#include "cterrain.h"
#include "game.h"
#include "ini.h"
#include "input.h"
#include "math.h"
#include "render.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "water.h"
#include "world.h"

//#include "ctree.h"
//#include "figure.h"

#define FOREST_GRID     7
#define FOREST_HALF     (FOREST_GRID / 2)
#define GRASS_GRID      3
#define GRASS_HALF      (GRASS_GRID / 2)
#define TERRAIN_GRID    15

static int              cached;
static int              texture_bytes;
static int              texture_bytes_counter;
static int              polygons;
static int              polygons_counter;

/*                  *************************************************************/

static GridManager        gm_terrain;
static vector<CTerrain>   il_terrain;
static GridManager        gm_forest;
static vector<CForest>    il_forest;
static GridManager        gm_grass;
static vector<CGrass>     il_grass;
static unsigned           world_seed = 2;

/* Module Functions *************************************************************/

void SceneClear ()
{

  il_grass.clear ();
  il_forest.clear ();
  il_terrain.clear ();
  gm_grass.Clear ();
  gm_forest.Clear ();
  gm_terrain.Clear ();

}

void SceneGenerate ()
{

  GLvector    camera;
  GLcoord     current;

  SceneClear ();
  WaterBuild ();
  camera = AvatarPosition ();
  current.x = (int)(camera.x) / GRASS_SIZE;

  il_grass.clear ();
  il_grass.resize (GRASS_GRID * GRASS_GRID);
  gm_grass.Init (&il_grass[0], GRASS_GRID, GRASS_SIZE);

  il_forest.clear ();
  il_forest.resize (FOREST_GRID * FOREST_GRID);
  gm_forest.Init (&il_forest[0], FOREST_GRID, FOREST_SIZE);

  il_terrain.clear ();
  il_terrain.resize (TERRAIN_GRID * TERRAIN_GRID);
  gm_terrain.Init (&il_terrain[0], TERRAIN_GRID, TERRAIN_SIZE);

}


void SceneTexturePurge ()
{

  SceneClear ();
  il_grass.clear ();
  il_grass.resize (GRASS_GRID * GRASS_GRID);
  gm_grass.Init (&il_grass[0], GRASS_GRID, GRASS_SIZE);

  il_forest.clear ();
  il_forest.resize (FOREST_GRID * FOREST_GRID);
  gm_forest.Init (&il_forest[0], FOREST_GRID, FOREST_SIZE);

  il_terrain.clear ();
  il_terrain.resize (TERRAIN_GRID * TERRAIN_GRID);
  gm_terrain.Init (&il_terrain[0], TERRAIN_GRID, TERRAIN_SIZE);

}

CTerrain* SceneTerrainGet (int x, int y)
{

  unsigned  i;
  GLcoord   gp;

  for (i = 0; i < il_terrain.size (); i++) {
    gp = il_terrain[i].GridPosition ();
    if (gp.x == x && gp.y == y)
      return &il_terrain[i];
  }
  return NULL;

}

//How far is it from the center of the terrain grid to the outer edge?
float SceneVisibleRange ()
{

  return (float)(TERRAIN_GRID / 2) * TERRAIN_SIZE;

}

//This is called to restart the terrain grid manager. After the terrains are built,
//we need to pass over them again so they can do their stitching.
void SceneRestartProgress ()
{

  gm_terrain.RestartProgress ();

}


void SceneInit ()
{


}

void SceneProgress (unsigned* ready, unsigned* total)
{

  *ready = gm_terrain.ItemsReady ();
  *total = min (gm_terrain.ItemsViewable (), 6);

}

void SceneUpdate (long stop)
{

  if (!GameRunning ())
    return;
  gm_terrain.Update (stop);
  gm_grass.Update (stop);
  gm_forest.Update (stop);
  TextPrint ("Scene: %d of %d terrains ready", gm_terrain.ItemsReady (), gm_terrain.ItemsViewable ());

}

void SceneRender ()
{

  if (!GameRunning ())
    return;
  if (!CVarUtils::GetCVar<bool> ("render.textured"))
    glDisable(GL_TEXTURE_2D);
  else
    glEnable(GL_TEXTURE_2D);

  glDisable(GL_CULL_FACE);
  CgShaderSelect (VSHADER_TREES);
  glColor3f (1,1,1);
  gm_forest.Render ();
  glEnable(GL_CULL_FACE);
  CgShaderSelect (VSHADER_NORMAL);
  glColor3f (1,1,1);
  gm_terrain.Render ();
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("grass2.png"));
  CgShaderSelect (VSHADER_GRASS);
  glColorMask (false, false, false, false);
  gm_grass.Render ();
  glColorMask (true, true, true, true);
  gm_grass.Render ();
  CgShaderSelect (VSHADER_NORMAL);
  WaterRender ();
  CgShaderSelect (VSHADER_NONE);
  AvatarRender ();

}

void SceneRenderDebug ()
{

  glEnable (GL_BLEND);
  glDisable(GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  glBlendFunc (GL_ONE, GL_ONE);
  glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
  glColor3f (1,1,1);
  gm_forest.Render ();
  gm_terrain.Render ();
  gm_grass.Render ();
  glColor3f (1,1,1);
  AvatarRender ();
  WaterRender ();

}