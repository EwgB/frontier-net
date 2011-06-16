

class CTree
{
  float             _height;
  vector<UINT>      _index;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLvector2> _uv;
  VBO               _vbo;

public:
  void              Build (GLvector pos);
  void              Render ();

};