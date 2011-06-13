/*-----------------------------------------------------------------------------

  World.cpp

-------------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

#define PAGE_GRID      256

#include "stdafx.h"
#include "camera.h"
#include "cpage.h"
#include "entropy.h"
#include "region.h"
#include "sdl.h"
#include "text.h"
#include "world.h"


static CPage*       page[PAGE_GRID][PAGE_GRID];
static int          page_count;
static GLcoord      walk;

/* Static Functions *************************************************************/

static void page_update (int x, int y, long stop)
{

  if (x < 0 || x >= PAGE_GRID || y < 0 || y >= PAGE_GRID)
    return;
  if (page[x][y] == NULL) 
    return;
  page[x][y]->Build (stop);

}

inline int CPageFromPos (int cell)
{

  return cell / PAGE_SIZE;

}

/* Module Functions *************************************************************/

SurfaceType WorldSurface (int x, int y)
{

  int     page_x, page_y;
  CPage*   z;
  
  x = max (0, x);
  y = max (0, y);
  page_x = CPageFromPos (x);
  page_y = CPageFromPos (y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return SURFACE_NULL;
  z = page[page_x][page_y];
  if (!z) 
    return SURFACE_NULL;
  return z->Surface(x % PAGE_SIZE, y % PAGE_SIZE);

}

GLrgba WorldSurfaceColor (int x, int y, SurfaceColor sc)
{

  int     page_x, page_y;
  CPage*   z;
  
  x = max (0, x);
  y = max (0, y);
  page_x = CPageFromPos (x);
  page_y = CPageFromPos (y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return glRgba (0.0f);
  z = page[page_x][page_y];
  if (!z) 
    return glRgba (0.0f);
  switch (sc) {
  case SURFACE_COLOR_GRASS:
    return z->ColorGrass (x % PAGE_SIZE, y % PAGE_SIZE);
  case SURFACE_COLOR_DIRT:
    return z->ColorDirt (x % PAGE_SIZE, y % PAGE_SIZE);
  case SURFACE_COLOR_ROCK:
    return z->ColorRock (x % PAGE_SIZE, y % PAGE_SIZE);
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


float WorldElevation (int x, int y)
{

  int     page_x, page_y;
  CPage*   z;
  
  x = max (0, x);
  y = max (0, y);
  page_x = CPageFromPos (x);
  page_y = CPageFromPos (y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return 0;
  z = page[page_x][page_y];
  if (!z) 
    return 0;
  return z->Elevation (x % PAGE_SIZE, y % PAGE_SIZE);

}

float WorldElevation (float x, float y)
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
  y0 = WorldElevation (cell_x, cell_y);
  y1 = WorldElevation (cell_x + 1, cell_y);
  y2 = WorldElevation (cell_x, cell_y + 1);
  y3 = WorldElevation (cell_x + 1, cell_y + 1);
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

bool WorldPointAvailable (int x, int y)
{

  int     page_x, page_y;
  CPage*   z;

  x = max (0, x);
  y = max (0, y);
  page_x = CPageFromPos (x);
  page_y = CPageFromPos (y);
  if (page_x < 0 || page_x >= PAGE_GRID || page_y < 0 || page_y >= PAGE_GRID)
    return false;
  z = page[page_x][page_y];
  if (!z) {
    z = new CPage;
    z->Cache (page_x, page_y);
    page[page_x][page_y] = z;
    page_count++;
  }
  return z->Ready ();

}

GLvector WorldPosition (int x, int y)
{

  GLvector pos;

  pos.x = (float)x;
  pos.y = (float)y;
  pos.z = WorldElevation (x, y);
  return pos;

}


void WorldPurge ()
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

void WorldRenderDebug ()
{

  int     x, y;

  for (y = 0; y < PAGE_GRID; y++) {
    for (x = 0; x < PAGE_GRID; x++) {
      if (page[x][y])
        page[x][y]->Render ();
    }
  }

}

void WorldUpdate (long stop)
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

void WorldUpdateZone (int world_x, int world_y, long stop)
{

  world_x /= PAGE_SIZE;
  world_y /= PAGE_SIZE;
  page_update (world_x, world_y, stop);

}

