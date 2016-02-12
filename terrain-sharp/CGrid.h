#ifndef GRID
#define GRID


//A virtual class.  Anything to be managed should be a subclass of this

class GridData
{
protected:
  GLcoord           _grid_position;
  GLbbox            _bbox;
public:
  GLcoord           GridPosition () const { return _grid_position; };
  virtual bool      Ready () { return true; };
  virtual void      Render () {};
  virtual void      Set (int grid_x, int grid_y, int grid_distance) {};
  virtual void      Update (long stop) {};
  virtual void      Invalidate () {}; 
  virtual unsigned  Sizeof () { return sizeof (this); }; 
};

//The grid manager. You need one of these for each type of object you plan to manage.

class GridManager
{
protected:
  GridData*             _item;       //Our list of items
  unsigned              _grid_size;  //The size of the grid of items to manage. Should be odd. Bigger = see farther.
  unsigned              _grid_half;  //The mid-point of the grid
  unsigned              _item_size;  //Size of an item in world units.
  unsigned              _item_count; //How many total items in the table?
  unsigned              _item_bytes; //size of items, in bytes
  unsigned              _view_items; //How many items in the table are withing the viewable circle?
  GLcoord               _last_viewer;
  unsigned              _list_pos;

  GLcoord               ViewPosition (GLvector eye);
  GridData*             Item (GLcoord c);
  GridData*             Item (unsigned index);
public:
  GridManager ();
  void                  Clear ();
  void                  Init (GridData* items, unsigned grid_size, unsigned item_size);
  unsigned              ItemsReady () { return _list_pos; }
  unsigned              ItemsViewable () { return _view_items; }
  void                  Update (long stop);
  void                  Render ();
  void                  RestartProgress () { _list_pos = 0; };

};

#endif