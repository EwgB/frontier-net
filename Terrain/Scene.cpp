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
#include "cforest.h"
#include "cgrass.h"
#include "cterrain.h"
#include "ctree.h"
#include "ini.h"
#include "input.h"
#include "math.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "water.h"
#include "world.h"

#define FOREST_GRID     7
#define FOREST_HALF     (FOREST_GRID / 2)
#define GRASS_GRID      7
#define GRASS_HALF      (GRASS_GRID / 2)
#define RENDER_DISTANCE 15
//#define TERRAIN_GRID    (WORLD_SIZE_METERS / TERRAIN_SIZE)
#define TERRAIN_GRID    21

//static CTerrain*        terrain[TERRAIN_GRID][TERRAIN_GRID];
//static GLcoord          terrain_walk;

static CTree            test_tree;
//static int              dist_table[RENDER_DISTANCE + 1][RENDER_DISTANCE + 1];
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

/* Module Functions *************************************************************/

void SceneClear ()
{

  CachePurge ();

}

void SceneGenerate ()
{

  GLvector    camera;
  GLcoord     current;

  SceneClear ();
  WorldGenerate ();
  WaterBuild ();
  camera = CameraPosition ();
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

static GLvector last_tree;
static int  seed;
static bool draw_tree;

void SceneInit ()
{

  SceneGenerate ();
  last_tree = IniVector ("Treepos");
  seed = IniInt ("TreeSeed");

}


void SceneUpdate (long stop)
{

  GLvector      camera;
  Region*       r;
  CTree*        tree;

  if (InputKeyPressed (SDLK_F11))
    SceneGenerate ();


  //TextPrint ("%d Terrains cached: %s\nTexture memory: %s\n%d Terrain triangles", cached, TextBytes (cached * sizeof (CTerrain)), TextBytes (texture_bytes), polygons);
  //TextPrint ("Tree has %d polys.", tree.Polygons ());
  camera = CameraPosition ();
  r = (Region*)CameraRegion ();
  tree = WorldTree (r->tree_type);
  tree->Info ();
  if (InputKeyPressed (SDLK_r)) {
    if (draw_tree)
      seed++;
    IniVectorSet ("Treepos", last_tree);
    IniIntSet ("TreeSeed", seed);
    test_tree.Create (false, 0.5f, 0.5f, seed);
    draw_tree = true;
  }
  
  if (InputKeyPressed (SDLK_t)) {
    GLvector  apos = AvatarPosition ();
    apos.x -= 4.0f;
    apos.z = CacheElevation (apos.x, apos.y);
    last_tree = apos;
  }
  gm_terrain.Update (stop);
  gm_grass.Update (stop);
  gm_forest.Update (stop);

}


void SceneRender ()
{

  glEnable(GL_TEXTURE_2D);
  glColor3f (1,1,1);

  if (draw_tree)
    test_tree.Render (last_tree, 0, LOD_HIGH);
  GLtexture*    t;

  t = TextureFromName ("g3.bmp", MASK_PINK);
  glBindTexture (GL_TEXTURE_2D, t->id);
  gm_grass.Render ();
  glEnable(GL_CULL_FACE);
  gm_forest.Render ();
  gm_terrain.Render ();
  WaterRender ();

}

void SceneRenderDebug (int style)
{

  glEnable (GL_BLEND);
  glDisable(GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  glBlendFunc (GL_ONE, GL_ONE);
  glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
  gm_terrain.Render ();

}