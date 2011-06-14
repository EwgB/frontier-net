#ifndef HVBO
#define HVBO


class VBO
{

  unsigned  _id_vertex;
  unsigned  _id_index;
  int       _size_vertex;
  int       _size_uv;
  int       _size_normal;
  int       _size_buffer;
  int       _polygon;
  unsigned  _index_count;
  bool      _ready;
  bool      _use_color;
  int       _size_color;

public:
  VBO ();
  ~VBO ();
  //void      Create (int polygon, int index_count, int vert_count, unsigned* index_list, GLvector* vert_list, GLvector* normal_list, GLvector2* uv_list);
  void      Create (int polygon, int index_count, int vert_count, unsigned* index_list, GLvector* vert_list, GLvector* normal_list, GLrgba* color_list, GLvector2* uv_list);
  void      Clear ();
  void      Render ();
};
#endif