/*-----------------------------------------------------------------------------

  Game.cpp

-------------------------------------------------------------------------------

  This module handles the launching of new games, quitting games, etc.
  
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "cache.h"
#include "console.h"
#include "game.h"
#include "file.h"
#include "input.h"
#include "main.h"
#include "player.h"
#include "render.h"
#include "scene.h"
#include "text.h"
#include "sdl.h"
#include "world.h"

#define TIME_SCALE          1000  //how many milliseconds per in-game minute
#define SECONDS_TO_DECIMAL  (1.0f / 60.0f)

static unsigned     seed;
static bool         running;
static long         seconds;
static long         minutes;
static long         hours;
static long         days;
static float        decimal_time;
static bool         loaded_previous;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void loading (float progress)
{

  SdlUpdate ();
  RenderLoadingScreen (progress);

}

static void precache ()
{

  unsigned  ready, total;

  SceneGenerate ();
  PlayerUpdate ();
  do {
    SceneProgress (&ready, &total);
    SceneUpdate (SDL_GetTicks () + 20);
    loading (((float)ready / (float)total) * 0.5f);
  } while (ready < total && !MainIsQuit ());
  SceneRestartProgress ();
  do {
    SceneProgress (&ready, &total);
    SceneUpdate (SDL_GetTicks () + 20);
    loading (0.5f + ((float)ready / (float)total) * 0.5f);
  } while (ready < total && !MainIsQuit ());


}

void GameLoad (unsigned seed_in)
{

  string            filename;
  vector<string>    sub_group;

  if (!seed_in) {
    ConsoleLog ("GameLoad: Error: Can't load a game without a valid seed.");
    return;
  }
  if (running) {
    ConsoleLog ("GameLoad: Error: Can't load while a game is in progress.");
    return;
  }
  seed = seed_in;
  filename = GameDirectory ();
  filename += "game.sav";
  if (!FileExists (filename.c_str ())) {
    seed = 0;
    ConsoleLog ("GameLoad: Error: File %s not found.", filename.c_str ());
    return;
  }
  if (ConsoleIsOpen ())
    ConsoleToggle ();
  CVarUtils::SetCVar ("last_played", seed);
  running = true;
  sub_group.push_back ("game");
  sub_group.push_back ("player");
  CVarUtils::Load (filename, sub_group);
  AvatarPositionSet (PlayerPositionGet ());
  WorldLoad (seed);
  seconds = 0;
  GameUpdate ();
  precache ();

}

void GameSave ()
{

  string            filename;
  vector<string>    sub_group;

  if (seed == 0) {
    ConsoleLog ("GameSave: Error: No valid game to save.");
    return;
  }
  filename = GameDirectory ();
  filename += "game.sav";
  sub_group.push_back ("game");
  sub_group.push_back ("player");
  CVarUtils::Save (filename, sub_group);

}

void GameInit ()
{
  CVarUtils::AttachCVar ("game.days", &days, "");
  CVarUtils::AttachCVar ("game.hours", &hours, "");
  CVarUtils::AttachCVar ("game.minutes", &minutes, "");
  seconds = 0;
}

void GameTerm ()
{

  if (running && seed)
    GameSave ();

}


void GameQuit ()
{

  ConsoleLog ("Quit Game");
  WorldSave ();
  SceneClear ();
  CachePurge ();
  GameSave ();
  seed = 0;
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
  float     elevation;
  int       points_checked;

  if (seed_in == 0) {
    GameQuit ();
    return;
  }
  days = 0;
  hours = 6;
  minutes = 30;
  seconds = 0;
  decimal_time = (float)days * 24.0f + (float)hours + (float)minutes * SECONDS_TO_DECIMAL;
  running = true;
  if (ConsoleIsOpen ())
    ConsoleToggle ();
  seed = seed_in;
  ConsoleLog ("Beginning new game with seed %d.", seed);
  FileMakeDirectory (GameDirectory ());
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
  while (points_checked < REGION_SIZE * 4 &&  !MainIsQuit ()) { 
    TextPrint ("Scanning %d", world_pos.x);
    loading (0.02f);
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
  ConsoleLog ("GameNew: Found beach in %d moves.", points_checked);
  CVarUtils::SetCVar ("last_played", seed);
  PlayerReset ();
  PlayerPositionSet (av_pos);
  GameUpdate ();
  precache (); 


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
  if (!args->data ()[0].compare ("load")) {
    if (args->size () > 1) 
      new_seed = atoi (args->data ()[1].c_str ());
    GameLoad (new_seed);
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

float GameTime ()
{
  return decimal_time;
}

void GameUpdate ()
{

  if (!loaded_previous) {
    loaded_previous = true;
    seed = CVarUtils::GetCVar<int> ("last_played");
    if (seed)
      GameLoad (seed);
    CVarUtils::SetCVar ("last_played", seed);
  }
  if (!running)
    return;
  if (InputKeyPressed (SDLK_RIGHTBRACKET))
    hours++;
  if (InputKeyPressed (SDLK_LEFTBRACKET)) {
    hours--;
    if (hours < 0)
      hours += 24;
  }
  seconds += SdlElapsed ();
  if (seconds >= TIME_SCALE) {
    seconds -= TIME_SCALE;
    minutes++;
  }
  if (minutes >= 60) {
    minutes -= 60;
    hours++;
  }
  if (hours >= 24) {
    hours -= 24;
    days++;
  }
  decimal_time = (float)days * 24.0f + (float)hours + (float)minutes * SECONDS_TO_DECIMAL;
  TextPrint ("Day %d: %02d:%02d", days + 1, hours, minutes);

}

