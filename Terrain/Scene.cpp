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
#include "cbrush.h"
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
#include "sky.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "water.h"
#include "world.h"

#define BRUSH_GRID      7

#define FOREST_GRID     7
#define FOREST_HALF     (FOREST_GRID / 2)
#define GRASS_GRID      5
#define GRASS_HALF      (GRASS_GRID / 2)
#define TERRAIN_GRID    9

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
static GridManager        gm_brush;
static vector<CBrush>     il_brush;
//static unsigned           world_seed = 2;

/* Module Functions *************************************************************/

void SceneClear ()
{

  il_grass.clear ();
  il_brush.clear ();
  il_forest.clear ();
  il_terrain.clear ();
  gm_grass.Clear ();
  gm_brush.Clear ();
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

  il_brush.clear ();
  il_brush.resize (BRUSH_GRID * BRUSH_GRID);
  gm_brush.Init (&il_brush[0], BRUSH_GRID, BRUSH_SIZE);


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

  il_brush.clear ();
  il_brush.resize (BRUSH_GRID * BRUSH_GRID);
  gm_brush.Init (&il_brush[0], BRUSH_GRID, BRUSH_SIZE);

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
  *total = min (gm_terrain.ItemsViewable (), 3);

}

void SceneUpdate (long stop)
{

  static unsigned update_type;

  if (!GameRunning ())
    return;
  //We don't want any grid to starve the others, so we rotate the order of priority.
  update_type++;
  switch (update_type % 4) {
  case 0: gm_terrain.Update (stop); break;
  case 1: gm_grass.Update (stop); break;
  case 2: gm_forest.Update (stop); break;
  case 3: gm_brush.Update (stop); break;
  }
  //any time left over goes to the losers...
  gm_terrain.Update (stop);
  gm_grass.Update (stop);
  gm_forest.Update (stop);
  gm_brush.Update (stop);
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
  SkyRender ();
  glDisable(GL_CULL_FACE);
  CgShaderSelect (VSHADER_TREES);
  CgShaderSelect (FSHADER_GREEN);
  glColor3f (1,1,1);
  gm_forest.Render ();
  glEnable(GL_CULL_FACE);
  CgShaderSelect (VSHADER_NORMAL);
  glColor3f (1,1,1);
  gm_terrain.Render ();
  WaterRender ();
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("grass3.png"));
  CgShaderSelect (VSHADER_GRASS);
  glColorMask (false, false, false, false);
  gm_grass.Render ();
  gm_brush.Render ();
  glColorMask (true, true, true, true);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  gm_grass.Render ();
  gm_brush.Render ();
  CgShaderSelect (VSHADER_NORMAL);
  AvatarRender ();
  CgShaderSelect (FSHADER_NONE);

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
  gm_brush.Render ();
  glColor3f (1,1,1);
  AvatarRender ();
  WaterRender ();

}