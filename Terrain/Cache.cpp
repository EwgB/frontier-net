/*-----------------------------------------------------------------------------

  Cache.cpp

-------------------------------------------------------------------------------

  This generates, stores, and fetches the pages of terrain data.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cpage.h"
#include "entropy.h"
#include "sdl.h"
#include "text.h"
#include "world.h"

#define PAGE_GRID   (WORLD_SIZE_METERS / PAGE_SIZE)


static CPage*       page[PAGE_GRID][PAGE_GRID];
static int          page_count;
static GLcoord      walk;

/* Static Functions *************************************************************/

inline int CPageFromPos (int cell)
{

  return cell / PAGE_SIZE;

}

static CPage* page_lookup (int world_x, int world_y) 
{

  int     page_x, page_y;
  
  if (world_x < 0 || world_y < 0)
    return NULL;
  page_x = CPageFromPos (world_x);
  page_y = CPageFromPos (world_y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return NULL;
  return page[page_x][page_y];


}

/* Various lookup functions **************************************************/


float CacheDetail (int world_x, int world_y)
{

  CPage*   p;
  
  p = page_lookup (world_x, world_y);
  if (!p) 
    return 0;
  return p->Detail (world_x % PAGE_SIZE, world_y % PAGE_SIZE);

}

float CacheElevation (int world_x, int world_y)
{
  
  CPage*   p;
  
  p = page_lookup (world_x, world_y);
  if (!p) 
    return 0;
  return p->Elevation (world_x % PAGE_SIZE, world_y % PAGE_SIZE);

}

float CacheElevation (float x, float y)
{

  int     cell_x;
  int     cell_y;
  float   a;
  float   b;
  float   c;
  float   y0, y1, y2, y3;
  float   dx;
  float   dy;

  cell_x = (int)x;
  cell_y = (int)y;
  dx = (x - (float)cell_x);
  dy = (y - (float)cell_y);
  y0 = CacheElevation (cell_x, cell_y);
  y1 = CacheElevation (cell_x + 1, cell_y);
  y2 = CacheElevation (cell_x, cell_y + 1);
  y3 = CacheElevation (cell_x + 1, cell_y + 1);
  if (dx < dy) {
    c = y2 - y0; 
    b = y3 - y2; 
    a = y0;
  } else {
    c = y3 - y1; 
    b = y1 - y0; 
    a = y0;
  }
  return (a + b * dx + c * dy);

}

GLvector CacheNormal (int world_x, int world_y)
{
  
  CPage*   p;
  
  p = page_lookup (world_x, world_y);
  if (!p) 
    return glVector (0.0f, 0.0f, 1.0f);
  return p->Normal (world_x % PAGE_SIZE, world_y % PAGE_SIZE);

}


bool CachePointAvailable (int world_x, int world_y)
{

  int     page_x, page_y;
  CPage*  p;

  world_x = max (0, world_x);
  world_y = max (0, world_y);
  page_x = CPageFromPos (world_x);
  page_y = CPageFromPos (world_y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return false;
  p = page[page_x][page_y];
  if (!p) {
    p = new CPage;
    p->Cache (page_x, page_y);
    page[page_x][page_y] = p;
    page_count++;
  }
  return p->Ready ();

}

GLvector CachePosition (int world_x, int world_y)
{
  
  CPage*   p;
  
  p = page_lookup (world_x, world_y);
  if (!p) 
    return glVector ((float)world_x, (float)world_y, 0.0f);
  return p->Position (world_x % PAGE_SIZE, world_y % PAGE_SIZE);

}

SurfaceType CacheSurface (int world_x, int world_y)
{

  CPage*   p;

  p = page_lookup (world_x, world_y);
  if (!p) 
    return SURFACE_NULL;
  return p->Surface(world_x % PAGE_SIZE, world_y % PAGE_SIZE);

}

GLrgba CacheSurfaceColor (int world_x, int world_y, SurfaceColor sc)
{

  CPage*   p;

  p = page_lookup (world_x, world_y);
  if (!p) 
    return glRgba (1.0f, 0.0f, 1.0f); //Pink, so we notice
  switch (sc) {
  case SURFACE_COLOR_GRASS:
    return p->ColorGrass (world_x % PAGE_SIZE, world_y % PAGE_SIZE);
  case SURFACE_COLOR_DIRT:
    return p->ColorDirt (world_x % PAGE_SIZE, world_y % PAGE_SIZE);
  case SURFACE_COLOR_ROCK:
    return p->ColorRock (world_x % PAGE_SIZE, world_y % PAGE_SIZE);
  case SURFACE_COLOR_BLACK:
    return glRgba (0, 0, 0);
  case SURFACE_COLOR_SAND:
    return glRgba (0.98f, 0.82f, 0.42f);
  case SURFACE_COLOR_SNOW:
    return glRgba (1.0f);
  }
  //Shouldn't happen.
  return glRgba (1, 0, 1); //Bright pink, so we notice the problem.

}

/* Module functions ******************************************************/

void CachePurge ()
{

  int     x, y;

  for (y = 0; y < PAGE_GRID; y++) {
    for (x = 0; x < PAGE_GRID; x++) {
      if (page[x][y])
        delete page[x][y];
      page[x][y] = NULL;
    }
  }

}

void CacheRenderDebug ()
{

  int     x, y;

  for (y = 0; y < PAGE_GRID; y++) {
    for (x = 0; x < PAGE_GRID; x++) {
      if (page[x][y])
        page[x][y]->Render ();
    }
  }

}

void CacheUpdate (long stop)
{

  TextPrint ("%d pages. (%s)", page_count, TextBytes (sizeof (CPage) * page_count));
  //Pass over the table a bit at a time and do garbage collection
  for (int i = 0; i < PAGE_GRID / 2; i++) {
    if (page[walk.x][walk.y] && page[walk.x][walk.y]->Expired ()) {
      delete page[walk.x][walk.y];
      page[walk.x][walk.y] = NULL;
    }
    walk.Walk (PAGE_GRID);
  }  

}


/*-----------------------------------------------------------------------------
  Request an update to a specific zone.  This can be called by Terrains, 
  which are waiting for the zone.
-----------------------------------------------------------------------------*/

void CacheUpdatePage (int world_x, int world_y, long stop)
{

  CPage*   p;
  
  p = page_lookup (world_x, world_y);
  if (!p) 
    return;
  p->Build (stop);

}

