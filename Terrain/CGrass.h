#define GRASS_SIZE        32

enum
{
  GRASS_STAGE_BEGIN,
  GRASS_STAGE_BUILD,
  GRASS_STAGE_COMPILE,
  GRASS_STAGE_DONE,
};

class CGrass
{
  GLcoord           _position;
  GLcoord           _origin;
  GLvector*         _vertex_list;
  GLvector*         _normal_list;
  GLvector2*        _uv_list;
  int               _list_size;
  int               _density;
  GLcoord           _walk;
  vector<GLrgba>    _color;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLvector2> _uv;
  vector<UINT>      _index;
  class VBO         _vbo;
  int               _stage;
  short             _color_index[GRASS_SIZE][GRASS_SIZE];
  GLbbox            _bbox;

  void              Build (long stop);
  void              VertexPush (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv);
  void              QuadPush (int n1, int n2, int n3, int n4);
  bool              ZoneCheck ();

public:
  CGrass ();
  GLcoord           Position () { return _position; }
  void              Set (int origin_x, int origin_y, int density);
  void              Render ();
  void              Update (long stop);
  bool              Ready () { return _stage == GRASS_STAGE_DONE; };
};