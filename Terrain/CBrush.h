#define BRUSH_SIZE        32

enum
{
  BRUSH_STAGE_BEGIN,
  BRUSH_STAGE_BUILD,
  BRUSH_STAGE_COMPILE,
  BRUSH_STAGE_DONE,
};


#ifndef GRID
#include "cgrid.h"
#endif

class CBrush : public GridData
{
  GLcoord           _grid_position;
  GLcoord           _origin;
  GLcoord           _walk;
  unsigned          _current_distance;
  //vector<GLrgba>    _color;
  //vector<GLvector>  _vertex;
  //vector<GLvector>  _normal;
  //vector<GLvector2> _uv;
  //vector<UINT>      _index;
  GLmesh            _mesh;
  class VBO         _vbo;
  int               _stage;
  GLbbox            _bbox;
  bool              _valid;

  void              Build (long stop);
  void              VertexPush (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv);
  void              QuadPush (int n1, int n2, int n3, int n4);
  bool              ZoneCheck ();

public:
  CBrush ();
  unsigned          Sizeof () { return sizeof (CBrush); }; 
  void              Set (int origin_x, int origin_y, int distance);
  void              Render ();
  void              Update (long stop);
  bool              Ready ()  { return _stage == BRUSH_STAGE_DONE; };
  void              Invalidate () { _valid = false; };
};