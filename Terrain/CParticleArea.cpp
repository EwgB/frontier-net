/*-----------------------------------------------------------------------------

  CParticleArea.cpp

-------------------------------------------------------------------------------

  This is a GridData subclass, concerned with filling out the world with 
  appropriate particle effects.  You can use Particle.cpp directly to create 
  localized one-off effects, but this is where the large persistant effects
  are managed.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cache.h"
#include "cparticlearea.h"
#include "particle.h"
#include "world.h"

#define STEP_SIZE     8
#define STEP_GRID     (PARTICLE_AREA_SIZE / STEP_SIZE)

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CParticleArea::Set (int x, int y, int distance)
{

  if (_grid_position.x == x && _grid_position.y == y)
    return;
  Invalidate ();
  _stage = PARTICLE_STAGE_BEGIN;
  _grid_position.x = x;
  _grid_position.y = y;
  _origin.x = x * PARTICLE_AREA_SIZE;
  _origin.y = y * PARTICLE_AREA_SIZE;
  
}

void CParticleArea::Invalidate ()
{

  UINT    i;

  for (i = 0; i < _emitter.size (); i++) {
    ParticleDestroy (_emitter[i]);
  }
  _emitter.clear ();

}

void CParticleArea::DoFog (GLcoord world)
{

  ParticleSet   p;
  GLvector      pos;

  ParticleLoad ("groundfog", &p);
  pos = CachePosition(world.x, world.y);
  p.colors.push_back (glRgba (1.0f, 1.0f, 1.0f));
  _emitter.push_back (ParticleAdd (&p, pos));

}

void CParticleArea::DoWindFlower ()
{

  GLcoord       walk;
  GLcoord       world;
  Region        r;
  ParticleSet   p;
  UINT          i;
  GLvector      pos;

  walk.Clear ();
  ParticleLoad ("windflower", &p);
  do {
    world.x = walk.x * STEP_SIZE + _origin.x;
    world.y = walk.y * STEP_SIZE + _origin.y;
    r = WorldRegionFromPosition (world.x, world.y);
    if (r.has_flowers && CacheSurface (world.x, world.y) == SURFACE_GRASS && CacheDetail (world.x, world.y) > 0.75f) {
      pos = CachePosition(world.x, world.y);
      p.colors.clear ();
      for (i = 0; i < FLOWERS; i++) {
        p.colors.push_back (r.color_flowers[i]);
        //p.colors.push_back (glRgba (1.0f, 1.0f, 0.0f));
        _emitter.push_back (ParticleAdd (&p, pos));
      }

    }
  } while (!walk.Walk (STEP_GRID));

}


void CParticleArea::DoSandStorm (GLcoord world)
{

  ParticleSet   p;
  GLvector      pos;

  pos = CachePosition(world.x, world.y);
  ParticleLoad ("sand", &p);
  p.colors.push_back (WorldColorGet (world.x, world.y, SURFACE_COLOR_SAND));
  _emitter.push_back (ParticleAdd (&p, pos));

}


bool CParticleArea::ZoneCheck ()
{

  if (!CachePointAvailable (_origin.x, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + PARTICLE_AREA_SIZE, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + PARTICLE_AREA_SIZE,_origin.y + PARTICLE_AREA_SIZE))
    return false;
  if (!CachePointAvailable (_origin.x, _origin.y + PARTICLE_AREA_SIZE))
    return false;
  return true;

}


void CParticleArea::Update (long stop) 
{

  ParticleSet   p;
  GLcoord       world;
  Region        region;
  GLvector      pos;

  if (Ready ())
    return;
  if (!ZoneCheck ())
    return;
  world.x = _grid_position.x * PARTICLE_AREA_SIZE + PARTICLE_AREA_SIZE / 2;
  world.y = _grid_position.y * PARTICLE_AREA_SIZE + PARTICLE_AREA_SIZE / 2;
  region = WorldRegionGet (world.x / REGION_SIZE, world.y / REGION_SIZE);
  pos = CachePosition(world.x, world.y);
  if (region.climate == CLIMATE_DESERT) 
    DoSandStorm (world);
  else if (region.climate == CLIMATE_SWAMP)
    DoFog (world);
  else
    DoWindFlower ();
  _stage++;

}

void CParticleArea::Render ()
{

  //NOTHING!  Particles are actually drawn by Particle.cpp. 

}