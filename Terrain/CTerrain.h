#define LAYERS            (sizeof (layers) / sizeof (LayerAttributes))
#define VISIBLE           1
//#define TERRAIN_GRID      256

#define PPM               16
#define TERRAIN_SIZE      32
#define TERRAIN_HALF      (TERRAIN_SIZE / 2)
#define TERRAIN_EDGE      (TERRAIN_SIZE + 1)
#define TERRAIN_PATCH     (TERRAIN_SIZE / _patch_steps)

enum
{
  NEIGHBOR_NORTH,
  NEIGHBOR_EAST,
  NEIGHBOR_SOUTH,
  NEIGHBOR_WEST,
  NEIGHBOR_COUNT
};

enum 
{
  STAGE_BEGIN,
  STAGE_CLEAR,
  STAGE_HEIGHTMAP,
  STAGE_QUADTREE,
  STAGE_STITCH,
  STAGE_INVENTORY,
  STAGE_BUFFER_LOAD,
  STAGE_COMPILE,
  STAGE_TEXTURE,
  STAGE_TEXTURE_FINAL,
  STAGE_DONE
};

class CTerrain
{
private:
  GLcoord           _walk;
  unsigned          _front_texture;
  unsigned          _back_texture;
  int               _texture_desired_size;
  int               _texture_current_size;
  int               _patch_size;
  int               _patch_steps;
  GLrgba            _color;
  class VBO         _vbo;
  int               _stage;
  bool              _surface_used[SURFACE_TYPES];
  int               _list_size;
  int               _list_pos;
  bool              _point[TERRAIN_EDGE][TERRAIN_EDGE];
  GLvector*         _vertex_list;
  GLvector*         _normal_list;
  GLvector2*        _uv_list;
  long              _rebuild;
  int               _neighbors[NEIGHBOR_COUNT];
  vector<GLvector>  _vert;


  void              DoStitch ();
  void              DoPatch (int x, int y);
  void              DoTexture ();
  void              DoHeightmap ();
  void              DoNormals ();
  void              DoQuad (int x1, int y1, int size);
  bool              DoCheckNeighbors ();
  bool              ZoneCheck (long stop);
  void              CompileBlock  (int x, int y, int size);
  void              TrianglePush (int i1, int i2, int i3);
  void              PointActivate (int x, int y);

public:
  GLcoord           _origin;
  GLvector          _pos[TERRAIN_EDGE][TERRAIN_EDGE];
  GLvector          _normal[TERRAIN_EDGE][TERRAIN_EDGE];
  GLvector2         _uv[TERRAIN_EDGE][TERRAIN_EDGE];
  GLvector2         _contour[TERRAIN_EDGE][TERRAIN_EDGE];
  int               _index_map[TERRAIN_EDGE][TERRAIN_EDGE];
  UINT*             _index_buffer;
  int               _index_buffer_size;

  void              Set (int origin_x, int origin_y, int texture_size);
  void              Clear ();
  void              Render ();
  void              Update (long stop);
  void              TexturePurge ();
  void              TextureSize (int size);
  int               TextureSizeGet () { return _texture_current_size;};
  //int         Polygons () { return TERRAIN_SIZE * TERRAIN_SIZE * 2; }
  int               Polygons () { return _list_size / 3; }
  GLcoord           Origin ();
  bool              Point (int x, int y) {return _point[x][y]; }
  int               Points () { return _list_size; }

};