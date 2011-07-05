/*-----------------------------------------------------------------------------

  Game.cpp

-------------------------------------------------------------------------------

  This module handles the launching of new games, quitting games, etc.
  
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "cache.h"
#include "console.h"
#include "main.h"
#include "render.h"
#include "scene.h"
#include "text.h"
#include "sdl.h"
#include "world.h"

static unsigned     seed;
static bool         running;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void loading (float progress)
{

  SdlUpdate ();
  RenderLoadingScreen (progress);

}

void GameQuit ()
{

  ConsoleLog ("Quit Game");
  SceneClear ();
  CachePurge ();
  running = false;

}


bool GameRunning ()
{
  return running;
}

void GameNew (unsigned seed_in)
{

  int       x;
  World*    w;
  int       start, end, step;
  int       region_x;
  Region    region;
  Region    region_neighbor;
  GLvector  av_pos;
  GLcoord   world_pos;
  unsigned  ready, total;
  float     elevation;
  int       points_checked;

  if (seed_in == 0) {
    GameQuit ();
    return;
  }
  running = true;
  if (ConsoleIsOpen ())
    ConsoleToggle ();
  seed = seed_in;
  ConsoleLog ("Beginning new game with seed %d.", seed);
  SceneClear ();
  CachePurge ();
  WorldGenerate (seed);
  //Now the world is ready.  Look for a good starting point.
  //Start in the center
  w = WorldPtr ();
  if (w->wind_from_west) {
    start = WORLD_GRID_CENTER;
    end = 1;
    step = -1;
  } else {
    start = WORLD_GRID_CENTER;
    end = WORLD_GRID - 1;
    step = 1;
  }
  region_x = WORLD_GRID_CENTER;
  for (x = start; x != end; x += step) {
    region = WorldRegionGet (x, WORLD_GRID_CENTER);
    region_neighbor = WorldRegionGet (x + step, WORLD_GRID_CENTER);
    if (region.climate == CLIMATE_COAST && region_neighbor.climate == CLIMATE_OCEAN) {
      region_x = x;
      break;
    }
  }
  //now we've found our starting coastal region. Push the player 1 more regain outward,
  //then begin scanning inward for dry land.
  world_pos.x = REGION_HALF + region_x * REGION_SIZE + step * REGION_SIZE;
  world_pos.x = clamp (world_pos.x, 0, WORLD_GRID * REGION_SIZE);
  world_pos.y = WORLD_GRID_CENTER * REGION_SIZE;
  //Set these values now just in case something goes wrong
  av_pos.x = (float)world_pos.x;
  av_pos.y = (float)world_pos.y;
  av_pos.z = 0.0f;
  step *= -1;//Now scan inward, towards the landmass
  points_checked = 0;
  while (points_checked < REGION_SIZE * 3 &&  !MainIsQuit ()) { 
    TextPrint ("Scanning %d", world_pos.x);
    loading (2.0f);
    if (!CachePointAvailable (world_pos.x, world_pos.y)) {
      CacheUpdatePage (world_pos.x, world_pos.y, SDL_GetTicks () + 20);
      continue;
    }
    points_checked++;
    elevation = CacheElevation (world_pos.x, world_pos.y);
    if (elevation > 0.0f) {
      av_pos = glVector ((float)world_pos.x, (float)world_pos.y, elevation);
      break;
    }
    world_pos.x += step;
  }
  AvatarPositionSet (av_pos);
  SceneGenerate ();
  do {
    SceneProgress (&ready, &total);
    SceneUpdate (SDL_GetTicks () + 20);
    loading ((float)ready / (float)total);
  } while (ready < total && !MainIsQuit ());


}

bool GameCmd (vector<string> *args)
{

  unsigned    new_seed;

  if (args->empty ()) {
    ConsoleLog (CVarUtils::GetHelp ("game").data ());
    return true;
  }
  if (!args->data ()[0].compare ("new")) {
    if (args->size () < 2) 
      new_seed = SDL_GetTicks ();
    else 
      new_seed = atoi (args->data ()[1].c_str ());
    GameNew (new_seed);
    return true;
  }
  if (!args->data ()[0].compare ("quit")) {
    GameQuit ();
    return true;
  }
  ConsoleLog (CVarUtils::GetHelp ("game").data ());
  return true;
}


char* GameDirectory ()
{

  static char     dir[32];

  sprintf (dir, "saves//seed%d//", seed);
  return dir;

}