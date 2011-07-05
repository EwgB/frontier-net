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
#include "ctree.h"
#include "entropy.h"
#include "file.h"
#include "sdl.h"
#include "world.h"

/*-----------------------------------------------------------------------------
 
-----------------------------------------------------------------------------*/

static char* page_file_name (GLcoord p)
{

  static char     name[256];

  
  sprintf (name, "%s//cache%d-%d.pag", WorldDirectory (), p.x, p.y);
  return name;

}


/*-----------------------------------------------------------------------------
 
-----------------------------------------------------------------------------*/

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
  return _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].pos;

}

GLvector CPage::Normal (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[(x % PAGE_SIZE)][(y % PAGE_SIZE)].normal;

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


unsigned CPage::Tree (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[x][y].tree_id;

}

float CPage::Elevation (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[x][y].pos.z;

}

float CPage::Detail (int x, int y)
{

  _last_touched = SdlTick ();
  return _cell[x][y].detail;

}

bool CPage::Expired ()
{

  return (_last_touched + PAGE_EXPIRE) < SdlTick ();

}

void CPage::DoPosition ()
{

  int     world_x, world_y;
  Cell    c;

  world_x = (_origin.x * PAGE_SIZE + _walk.x);
  world_y = (_origin.y * PAGE_SIZE + _walk.y);
  //_cell[_walk.x][_walk.y].elevation = RegionElevation (world_x, world_y);
  c = WorldCell (world_x, world_y);
  //c.elevation = c.water_level;
  _cell[_walk.x][_walk.y].pos = glVector ((float)world_x, (float)world_y, c.elevation);
  _cell[_walk.x][_walk.y].detail = c.detail;
  _cell[_walk.x][_walk.y].water_level = c.water_level;
  _cell[_walk.x][_walk.y].tree_id = 0;
  //_cell[_walk.x][_walk.y].elevation = _cell[_walk.x][_walk.y].pt.elevation;
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
    _cell[_walk.x][_walk.y].grass = WorldColorGet (world_x, world_y, SURFACE_COLOR_GRASS);
  if (_cell[_walk.x][_walk.y].surface == SURFACE_DIRT || _cell[_walk.x][_walk.y].surface == SURFACE_DIRT_DARK || _cell[_walk.x][_walk.y].surface == SURFACE_FOREST)
    _cell[_walk.x][_walk.y].dirt = WorldColorGet (world_x, world_y, SURFACE_COLOR_DIRT);
  _cell[_walk.x][_walk.y].rock = WorldColorGet (world_x, world_y, SURFACE_COLOR_ROCK);
  if (_walk.Walk (PAGE_SIZE))
    _stage++;

}


void CPage::DoNormal ()
{

  GLvector        normal_y, normal_x;

  if (_walk.x < 1 || _walk.x >= PAGE_SIZE - 1) 
    normal_x = glVector (-1, 0, 0);
  else
    normal_x = _cell[_walk.x - 1][_walk.y].pos - _cell[_walk.x + 1][_walk.y].pos;
  if (_walk.y < 1 || _walk.y >= PAGE_SIZE - 1) 
    normal_y = glVector (0, -1, 0);
  else
    normal_y = _cell[_walk.x][_walk.y - 1].pos - _cell[_walk.x][_walk.y + 1].pos;
  _cell[_walk.x][_walk.y].normal = glVectorCrossProduct (normal_x, normal_y);
  _cell[_walk.x][_walk.y].normal.z *= NORMAL_SCALING;
  _cell[_walk.x][_walk.y].normal.Normalize ();
  if (_walk.Walk (PAGE_SIZE))
    _stage++;

}


void CPage::DoTrees ()
{

  GLcoord   worldpos;
  Region    region;
  pcell*    c;
  int       x, y;
  GLcoord   plant;
  bool      valid;
  float     best;
  CTree*    tree;

  worldpos.x = _origin.x * PAGE_SIZE + _walk.x;
  worldpos.y = _origin.y * PAGE_SIZE + _walk.y;
  region = WorldRegionFromPosition (worldpos.x, worldpos.y);
  valid = false;

  tree = WorldTree (region.tree_type);
  if (tree->GrowsHigh ())
    best = -99999.9f;
  else
    best = 99999.9f;
  for (x = 0; x < TREE_SPACING - 2; x++) {
    for (y = 0; y < TREE_SPACING - 2; y++) {
      c = &_cell[_walk.x * TREE_SPACING + x][_walk.y * TREE_SPACING + y];
      if (c->surface != SURFACE_GRASS && c->surface != SURFACE_SNOW && c->surface != SURFACE_FOREST)
        continue;
      //Don't spawn trees that might touch water.  Looks odd.
      if (c->pos.z < c->water_level + 1.2f)
        continue;
      if (tree->GrowsHigh() && (c->detail + region.tree_threshold) > 1.0f && c->pos.z > best) {
        plant.x = _walk.x * TREE_SPACING + x;
        plant.y = _walk.y * TREE_SPACING + y;
        best = c->pos.z;
        valid = true;
      }
      if (!tree->GrowsHigh() && (c->detail - region.tree_threshold) < 0.0f && c->pos.z < best) {
        plant.x = _walk.x * TREE_SPACING + x;
        plant.y = _walk.y * TREE_SPACING + y;
        best = c->pos.z;
        valid = true;
      }
    }
  }
  if (valid) {
    c = &_cell[plant.x][plant.y];
    c->tree_id = region.tree_type;
  }
  if (_walk.Walk (TREE_MAP))
    _stage++;

}


void CPage::DoSurface ()
{

  float     high, low, delta;
  float     fade;
  int       xx, yy;
  int       neighbor_x, neighbor_y;
  GLcoord   worldpos;
  Region    region;
  pcell*    c;

  worldpos.x = _origin.x * PAGE_SIZE + _walk.x;
  worldpos.y = _origin.y * PAGE_SIZE + _walk.y;
  region = WorldRegionFromPosition (worldpos.x, worldpos.y);
  c = &_cell[_walk.x][_walk.y];
  if (_stage == PAGE_STAGE_SURFACE1) {
    //Get the elevation of our neighbors
    high = low = c->pos.z;
    for (xx = -2; xx <= 2; xx++) {
      neighbor_x = _walk.x + xx;
      if (neighbor_x < 0 || neighbor_x >= PAGE_SIZE) 
        continue;
      for (yy = -2; yy <= 2; yy++) {
        neighbor_y = _walk.y + yy;
        if (neighbor_y < 0 || neighbor_y >= PAGE_SIZE) 
          continue;
        high = max (high, _cell[neighbor_x][neighbor_y].pos.z);
        low = min (low, _cell[neighbor_x][neighbor_y].pos.z);
      }
    }
    delta = high - low;
    //Default surface. If the climate can support life, default to grass.
    if (region.temperature > 0.1f && region.moisture > 0.1f)
      c->surface = SURFACE_GRASS;
    else //Too cold or dry
      c->surface = SURFACE_ROCK;
    if (region.climate == CLIMATE_DESERT)
      c->surface = SURFACE_SAND;
    //Sand is only for coastal regions
    if (low <= 2.0f && (region.climate == CLIMATE_COAST))
      c->surface = SURFACE_SAND;
    //Forests are for... forests?
    if (c->detail < 0.75f && c->detail > 0.25f && (region.climate == CLIMATE_FOREST))
      c->surface = SURFACE_FOREST;
    if (delta >= region.moisture * 6)
      c->surface = SURFACE_DIRT;
    if (low <= region.geo_water && region.climate != CLIMATE_SWAMP)
      c->surface = SURFACE_DIRT;
    if (low <= region.geo_water && region.climate != CLIMATE_SWAMP)
      c->surface = SURFACE_DIRT_DARK;
    //The colder it is, the more surface becomes snow, beginning at the lowest points.
    if (region.temperature < FREEZING) {
      fade = region.temperature / FREEZING;
      if ((1.0f - c->detail) > fade)
        c->surface = SURFACE_SNOW;
    }
    //dirt touched by water is dark
    if (region.climate != CLIMATE_SWAMP) {
      if (c->surface == SURFACE_SAND && low <= 0)
        c->surface = SURFACE_SAND_DARK;
      if (low <= c->water_level)
        c->surface = SURFACE_DIRT_DARK;
    }
    if (low <= 2.5f && (region.climate == CLIMATE_OCEAN))
      c->surface = SURFACE_SAND;
    if (low <= 2.5f && (region.climate == CLIMATE_COAST))
      c->surface = SURFACE_SAND;
    if (delta > 4.0f && region.temperature > 0.0f)
      c->surface = SURFACE_ROCK;
  } else {
    if (c->surface == SURFACE_GRASS && _walk.x > 0 && _walk.x < PAGE_SIZE - 1 && _walk.y > 0 && _walk.y < PAGE_SIZE - 1) {  
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
        c->surface = SURFACE_GRASS_EDGE;
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
    case PAGE_STAGE_POSITION:
      DoPosition ();
      break;
    case PAGE_STAGE_NORMAL:
      DoNormal ();
      break;
    case PAGE_STAGE_SURFACE1:
    case PAGE_STAGE_SURFACE2:
      DoSurface ();
      break;
    case PAGE_STAGE_COLOR:
      DoColor ();
      break;
    case PAGE_STAGE_TREES:
      DoTrees ();
      break;
    case PAGE_STAGE_SAVE:
      _stage++;
      Save ();
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
  if (FileExists (page_file_name (_origin))) {
    char*   buf;
    long    size;
    long    my_size;

    my_size = sizeof (CPage);
    buf = NULL;
    buf = FileBinaryLoad (page_file_name (_origin), &size);
    if (buf && size == my_size) 
      memcpy (this, buf, size);
    if (buf)  
      free (buf);
  }  
  _walk.Clear ();
  _last_touched = SdlTick ();
 
}

void CPage::Save ()
{

  if (!CVarUtils::GetCVar<bool> ("cache.active"))
    return;
  if (_stage == PAGE_STAGE_DONE)
    FileSave (page_file_name (_origin), (char*)this, sizeof (CPage));

}