#ifndef GRID
#define GRID


//A virtual class.  Anything to be managed should be a subclass of this

class GridData
{
protected:
  GLcoord           _grid_position;
  GLbbox            _bbox;
public:
  GLcoord           GridPosition () const { 
    return _grid_position; 
  };
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
  GridData*             _item;
  unsigned              _grid_size;
  unsigned              _grid_half;
  unsigned              _item_size;
  unsigned              _item_count;
  unsigned              _item_bytes;
  GLcoord               _last_viewer;
  unsigned              _list_pos;

  GLcoord               ViewPosition (GLvector eye);
  GridData*             Item (GLcoord c);
  GridData*             Item (unsigned index);
public:
  GridManager ();
  void                  Init (GridData* items, unsigned grid_size, unsigned item_size);
  void                  Update (long stop);
  void                  Render ();

};

#endif