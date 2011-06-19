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
  int               _density;
  GLcoord           _walk;
  vector<GLrgba>    _color;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLvector2> _uv;
  vector<UINT>      _index;
  class VBO         _vbo;
  int               _stage;
  GLbbox            _bbox;
  bool              _valid;

  void              Build (long stop);
  void              VertexPush (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv);
  void              QuadPush (int n1, int n2, int n3, int n4);
  bool              ZoneCheck ();

public:
  CGrass ();
  GLcoord           Position () const { return _position; }
  void              Set (int origin_x, int origin_y, int density);
  void              Render ();
  void              Update (long stop);
  bool              Ready ()  const { return _stage == GRASS_STAGE_DONE; };
  void              Invalidate () { _valid = false; };
};