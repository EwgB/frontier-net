#define LAYERS            (sizeof (layers) / sizeof (LayerAttributes))
#define VISIBLE           1

#define PPM               16
#define TERRAIN_SIZE      64
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
  STAGE_DO_COMPILE_GRID,
  STAGE_HEIGHTMAP,
  STAGE_QUADTREE,
  STAGE_STITCH,
  STAGE_BUFFER_LOAD,
  STAGE_COMPILE,
  STAGE_VBO,
  STAGE_TEXTURE,
  STAGE_TEXTURE_FINAL,
  STAGE_DONE
};

#ifndef GRID
#include "cgrid.h"
#endif

class CTerrain : public GridData
{
private:
  GLcoord           _origin;
  GLvector          _pos[TERRAIN_EDGE][TERRAIN_EDGE];
  int               _index_map[TERRAIN_EDGE][TERRAIN_EDGE];
  bool              _point[TERRAIN_EDGE][TERRAIN_EDGE];
  GLcoord           _walk;
  unsigned          _front_texture;
  unsigned          _back_texture;
  int               _texture_desired_size;
  int               _texture_current_size;
  int               _patch_size;
  int               _patch_steps;
  int               _index_buffer_size;
  GLrgba            _color;
  class VBO         _vbo;
  int               _stage;
  bool              _surface_used[SURFACE_TYPES];
  vector<UINT>      _index_buffer;
  vector<GLvector>  _vertex_list;
  vector<GLvector>  _normal_list;
  vector<GLvector2> _uv_list;
  long              _rebuild;
  int               _neighbors[NEIGHBOR_COUNT];
  vector<GLvector>  _vert;
  unsigned          _current_distance;
  bool              _valid;


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
  void              Invalidate () { _valid = false; }

public:
  CTerrain ();

  unsigned          Sizeof () { return sizeof (CTerrain); }; 
  void              Set (int grid_x, int grid_y, int distance);
  void              Clear ();
  void              Render ();
  void              Update (long stop);
  void              TexturePurge ();
  void              TextureSize (int size);
  int               TextureSizeGet () { return _texture_current_size;};
  int               Polygons () { return _index_buffer.size () / 3; }
  GLcoord           Origin ();
  bool              Point (int x, int y) {return _point[x][y]; }
  int               Points () { return _index_buffer.size (); }
  bool              Ready () { return _stage == STAGE_DONE; };

};