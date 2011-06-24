/*-----------------------------------------------------------------------------

  Grid.cpp


-------------------------------------------------------------------------------

  The grid manager handles various types of objects that make up the world. 
  Terrain, blocks of trees, grass, etc.  It takes tables of GridData objects
  and shuffles them around, rendering them and prioritizing their updates
  to favor things closest to the player.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "grid.h"
#include "input.h"

struct Dist
{
  GLcoord   offset;
  float     distancef;
  unsigned  distancei;
};

#define TABLE_SIZE  32
#define TABLE_HALF  (TABLE_SIZE / 2)

static vector<Dist> distance_list;
static vector<Dist> foo2;
static bool         list_ready;

/*-----------------------------------------------------------------------------
Here we build a list of offsets.  These are used to walk a grid outward in
concentric circles.  This is used to make sure we update the items closest to 
the player first.
-----------------------------------------------------------------------------*/

int dist_sort (const void* elem1, const void* elem2)
{

  Dist*   d1 = (Dist*)elem1;
  Dist*   d2 = (Dist*)elem2;

  if (d1->distancef < d2->distancef)
    return -1;
  else if (d1->distancef > d2->distancef)
    return 1;
  return 0;

}

static void do_list ()
{
  
  int       x, y;
  int       i;
  Dist*     d;
  GLvector2 to_center;

  list_ready = true;
  distance_list.resize (TABLE_SIZE *  TABLE_SIZE);
  foo2.resize (TABLE_SIZE *  TABLE_SIZE);
  i = 0;
  for (x = 0; x < TABLE_SIZE; x++) {
    for (y = 0; y < TABLE_SIZE; y++) {
      d = &distance_list[i];
      d->offset.x = x - TABLE_HALF;
      d->offset.y = y - TABLE_HALF;
      to_center.x = (float)d->offset.x;
      to_center.y = (float)d->offset.y;
      d->distancef = to_center.Length ();
      d->distancei = (int)d->distancef;
      i++;
    }
  }
  qsort (&distance_list[0], distance_list.size (), sizeof (Dist), dist_sort);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GridManager::GridManager ()
{

  _item = NULL;
  _grid_size = _grid_half = _item_size = _item_count = 0;

}

GridData* GridManager::Item (GLcoord c)
{

  int       index;
  char*     ptr;

  //Dicey pointer arithmetic. C++ is an awesome language!
  index = (c.x % _grid_size) + (c.y  % _grid_size) * _grid_size;
  ptr = (char*)&_item[0] + (index * _item_bytes);
  return (GridData*)ptr;

}

GridData* GridManager::Item (unsigned index)
{

  char*     ptr;

  ptr = (char*)&_item[0] + (index * _item_bytes);
  return (GridData*)ptr;

}

void GridManager::Init (GridData* itemptr, unsigned grid_size, unsigned item_size)
{

  GLcoord     viewer;
  GridData*   gd;
  GLcoord     walk;

  if (!list_ready)
    do_list ();
  _item = itemptr;
  _grid_size = grid_size;
  _grid_half = _grid_size / 2;
  _item_size = item_size;
  _item_bytes = _item[0].Sizeof ();
  _item_count = _grid_size * _grid_size;
  viewer = ViewPosition (CameraPosition ());
  _last_viewer = viewer;
  walk.Clear ();
  do {
    gd = Item (walk);
    gd->Invalidate ();
    gd->Set (viewer.x + walk.x - _grid_half, viewer.y + walk.y - _grid_half, 0);
    //gd->Set (viewer.x + walk.x - _grid_half, viewer.y + walk.y - _grid_half, 0);
  } while (!walk.Walk (_grid_size));

}

GLcoord GridManager::ViewPosition (GLvector eye)
{

  GLcoord   result;

  result.x = (int)(eye.x - _item_size / 2) / _item_size;
  result.y = (int)(eye.y - _item_size / 2) / _item_size;
  return result;

}

void GridManager::Update (long stop)
{

  GLcoord     viewer;
  GLcoord     pos;
  GLcoord     grid_pos;
  unsigned    dist;

  viewer = ViewPosition (CameraPosition ());
  //If the player has moved to a new spot on the grid, restart our
  //outward walk.
  if (viewer != _last_viewer) {
    _last_viewer = viewer;
    _list_pos = 0;
  }
  //figure out where the player is in our rolling grid
  grid_pos.x = _grid_half + viewer.x % _grid_size;
  grid_pos.y = _grid_half + viewer.y % _grid_size;
  //Now offset that with the position being updated.
  grid_pos += distance_list[_list_pos].offset;
  //Bring it back into bounds.
  if (grid_pos.x < 0)
    grid_pos.x += _grid_size;
  if (grid_pos.y < 0)
    grid_pos.y += _grid_size;
  grid_pos.x %= _grid_size;
  grid_pos.y %= _grid_size;
  pos = Item(grid_pos)->GridPosition ();
  if (viewer.x - pos.x > (int)_grid_half)
    pos.x += _grid_size;
  if (pos.x - viewer.x > (int)_grid_half)
    pos.x -= _grid_size;
  if (viewer.y - pos.y > (int)_grid_half)
    pos.y += _grid_size;
  if (pos.y - viewer.y > (int)_grid_half)
    pos.y -= _grid_size;
  dist = max (abs (pos.x - viewer.x), abs(pos.y - viewer.y));
  Item(grid_pos)->Set (pos.x, pos.y, dist);
  Item(grid_pos)->Update (stop);
  if (Item(grid_pos)->Ready () && InputKeyPressed (SDLK_p)) {
    _list_pos++;
    //If we reach the outer ring, move back to the center and begin again.
    if (distance_list[_list_pos].distancei >= _grid_size)
      _list_pos = 0;
  }

}

void GridManager::Render ()
{

  unsigned      i;

  glDisable (GL_LIGHTING);
  GLrgba c;

  for (i = 0; i < _item_count; i++) {
    c = glRgbaUnique (i);
    glColor3fv (&c.red);
    Item(i)->Render ();
  }


}
