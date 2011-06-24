#define FOREST_SIZE        64

enum
{
  FOREST_STAGE_BEGIN,
  FOREST_STAGE_BUILD,
  FOREST_STAGE_COMPILE,
  FOREST_STAGE_DONE
};

struct TreeMesh
{
  unsigned          _texture_id;
  GLmesh            _mesh;
};

struct TreeVBO
{
  unsigned          _texture_id;
  class VBO         _vbo;
  GLbbox            _bbox;
};

#ifndef GRID
#include "Grid.h"
#endif

class CForest : public GridData
{
  LOD               _lod;
  unsigned          _current_distance;
  bool              _swap;
  GLcoord           _origin;
  int               _stage;
  bool              _valid;
  GLcoord           _walk;
  vector<TreeMesh>  _mesh_list;
  vector<TreeVBO>   _vbo_list;

  void              Build (long stop);
  void              Compile ();
  bool              ZoneCheck ();
  unsigned          MeshFromTexture (unsigned texture_id);

public:
  CForest ();
  virtual unsigned  Sizeof () { return sizeof (CForest); }; 
  //GLcoord           GridPosition () const { return _grid_position; };
  void              Set (int x, int y, int distance);
  void              Render ();
  void              Update (long stop);
  bool              Ready ()  const { return _stage == FOREST_STAGE_DONE; };
  void              Invalidate () { _valid = false; };
};