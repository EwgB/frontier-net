/*-----------------------------------------------------------------------------

  CPage.cpp


-------------------------------------------------------------------------------

  The Page class is used to generate and cache pages of world texture data.  

  The pages are generated by combining the topographical data (elevations)
  with the region data (modifying the evevation to make the different land
  formations) and then is used to generate the table of surface data, which
  describes how to paint the textures for the given area.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cpage.h"
#include "entropy.h"
#include "region.h"
#include "sdl.h"
#include "world.h"

SurfaceType CPage::Surface (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[x][y].surface;

}

bool CPage::Ready ()
{

  _last_touched = SdlTick ();
  return _stage == PAGE_STAGE_DONE;

}

GLvector CPage::Position (int x, int y)
{

  _last_touched = SdlTick ();
  return glVector ((float)x, (float)y, _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].elevation);

}

GLrgba CPage::ColorGrass (int x, int y)
{

  return _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].grass;

}

GLrgba CPage::ColorDirt (int x, int y)
{

  return _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].dirt;

}

GLrgba CPage::ColorRock (int x, int y)
{

  return _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].rock;

}


void CPage::Render ()
{

  int     elapsed;
  float   n;

  glDisable (GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  elapsed = SdlTick () - _last_touched;
  n = (float)elapsed / PAGE_EXPIRE;
  n = clamp (n, 0.0f, 1.0f);
  glColor3f (n, 1.0f - n, 0.0f);
  _bbox.Render ();

}

float CPage::Elevation (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[x][y].elevation;

}

bool CPage::Expired ()
{

  return (_last_touched + PAGE_EXPIRE) < SdlTick ();

}

void CPage::DoElevation ()
{

  int     world_x, world_y;

  world_x = (_origin.x * PAGE_SIZE + _walk.x);
  world_y = (_origin.y * PAGE_SIZE + _walk.y);
  _cell[_walk.x][_walk.y].elevation = RegionElevation (world_x, world_y);
  _bbox.ContainPoint (Position (world_x, world_y));
  if (_walk.Walk (PAGE_SIZE))
    _stage++;

}

void CPage::DoColor ()
{

  int     world_x, world_y;

  world_x = (_origin.x * PAGE_SIZE + _walk.x);
  world_y = (_origin.y * PAGE_SIZE + _walk.y);
  if (_cell[_walk.x][_walk.y].surface == SURFACE_GRASS || _cell[_walk.x][_walk.y].surface == SURFACE_GRASS_EDGE)
    _cell[_walk.x][_walk.y].grass = RegionColorGet (world_x, world_y, SURFACE_COLOR_GRASS);
  if (_cell[_walk.x][_walk.y].surface == SURFACE_DIRT || _cell[_walk.x][_walk.y].surface == SURFACE_DIRT_DARK)
    _cell[_walk.x][_walk.y].dirt = RegionColorGet (world_x, world_y, SURFACE_COLOR_DIRT);
  _cell[_walk.x][_walk.y].rock = RegionColorGet (world_x, world_y, SURFACE_COLOR_ROCK);
  if (_walk.Walk (PAGE_SIZE))
    _stage++;

}

void CPage::DoSurface ()
{

  float     high, low, here, delta;
  int       xx, yy;
  int       neighbor_x, neighbor_y;
  GLcoord   worldpos;
  Region    region;

  worldpos.x = _origin.x * PAGE_SIZE + _walk.x;
  worldpos.y = _origin.y * PAGE_SIZE + _walk.y;
  region = RegionGet (worldpos.x, worldpos.y);
  if (_stage == PAGE_STAGE_SURFACE1) {
    //Get the elevation of our neighbors
    here = high= low = _cell[_walk.x][_walk.y].elevation;
    for (xx = -2; xx <= 2; xx++) {
      neighbor_x = _walk.x + xx;
      if (neighbor_x < 0 || neighbor_x >= PAGE_SIZE) 
        continue;
      for (yy = -2; yy <= 2; yy++) {
        neighbor_y = _walk.y + yy;
        if (neighbor_y < 0 || neighbor_y >= PAGE_SIZE) 
          continue;
        high = max (high, _cell[neighbor_x][neighbor_y].elevation);
        low = min (low, _cell[neighbor_x][neighbor_y].elevation);
      }
    }
    delta = high - low;
    /*
    if ((Entropy (worldpos.x, worldpos.y) * 0.1f + region.temperature) > 0.25) {
      if (region.moisture > 0.1f)
        _cell[_walk.x][_walk.y].surface = SURFACE_GRASS;
      else 
        _cell[_walk.x][_walk.y].surface = SURFACE_DIRT;
    } else
      _cell[_walk.x][_walk.y].surface = SURFACE_ROCK;
      */
    _cell[_walk.x][_walk.y].surface = SURFACE_GRASS;
    if (delta >= region.moisture * 6)
      _cell[_walk.x][_walk.y].surface = SURFACE_DIRT;
    //if (high > 0 && low < 0 && (region.flags & REGION_FLAG_SWAMP))
      //_cell[_walk.x][_walk.y].surface = SURFACE_GRASS_EDGE;
    if (region.temperature < FREEZING) {
      float     snow_threshold;
      snow_threshold = FREEZING - region.temperature;
      snow_threshold *= 8;
      if (delta <= (snow_threshold))
        _cell[_walk.x][_walk.y].surface = SURFACE_SNOW;
      //if (_cell[_walk.x][_walk.y].surface == SURFACE_GRASS && region.temperature < 0.2f)
        //_cell[_walk.x][_walk.y].surface = SURFACE_SNOW;
    }
    //Sand is only for coastal regions
    if (low <= region.beach_threshold && (region.climate == CLIMATE_COAST))
      _cell[_walk.x][_walk.y].surface = SURFACE_SAND;
    if (low <= region.topography_bias + region.moisture)
      _cell[_walk.x][_walk.y].surface = SURFACE_DIRT;
    if (low <= 0 && (region.climate == CLIMATE_RIVER))
      _cell[_walk.x][_walk.y].surface = SURFACE_DIRT_DARK;
    if (low <= region.topography_bias)
      _cell[_walk.x][_walk.y].surface = SURFACE_DIRT_DARK;
    if (low <= 2.5f && (region.climate == CLIMATE_OCEAN))
      _cell[_walk.x][_walk.y].surface = SURFACE_SAND;
    //Sand touched by water is dark
    if (_cell[_walk.x][_walk.y].surface == SURFACE_SAND && low <= 0)
      _cell[_walk.x][_walk.y].surface = SURFACE_SAND_DARK;
    if (delta > 3.0f)
      _cell[_walk.x][_walk.y].surface = SURFACE_ROCK;
  } else {
    if (_cell[_walk.x][_walk.y].surface == SURFACE_GRASS
      && _walk.x > 0 && _walk.x < PAGE_SIZE - 1 && _walk.y > 0 && _walk.y < PAGE_SIZE - 1) {
      bool all_grass = true;
      for (xx = -1; xx <= 1; xx++) {
        if (!all_grass)
          break;
        for (yy = -1; yy <= 1; yy++) {
          if (_cell[_walk.x + xx][_walk.y + yy].surface != SURFACE_GRASS && _cell[_walk.x + xx][_walk.y + yy].surface != SURFACE_GRASS_EDGE) {
            all_grass = false;
            break;
          }
        }
      }
      if (!all_grass)
        _cell[_walk.x][_walk.y].surface = SURFACE_GRASS_EDGE;
    }
  }
  if (_walk.Walk (PAGE_SIZE))
    _stage++;
  
}

void CPage::Build (int stop)
{

  while (_stage != PAGE_STAGE_DONE && SdlTick () < stop) {
    switch (_stage) {
    case PAGE_STAGE_BEGIN:
      _stage++;
      break;
    case PAGE_STAGE_ELEVATION:
      DoElevation ();
      break;
    case PAGE_STAGE_SURFACE1:
    case PAGE_STAGE_SURFACE2:
      DoSurface ();
      break;
    case PAGE_STAGE_COLOR:
      DoColor ();
      break;
    }
  }

}

void CPage::Cache (int origin_x, int origin_y)
{

  _origin.x = origin_x;
  _origin.y = origin_y;
  _stage = PAGE_STAGE_BEGIN;
  _bbox.Clear ();
  _walk.Clear ();
  _last_touched = SdlTick ();
 
}