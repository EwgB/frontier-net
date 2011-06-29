#define TREE_ALTS   3

enum TreeTrunkStyle
{
  TREE_TRUNK_NORMAL,
  TREE_TRUNK_JAGGED,
  TREE_TRUNK_BENT,
  TREE_TRUNK_STYLES
};

enum TreeFoliageStyle
{
  TREE_FOLIAGE_UMBRELLA,
  TREE_FOLIAGE_BOWL,
  TREE_FOLIAGE_PANEL,
  TREE_FOLIAGE_SAG,
  TREE_FOLIAGE_STYLES
};

enum TreeLiftStyle
{
  TREE_LIFT_STRAIGHT,
  TREE_LIFT_IN,
  TREE_LIFT_OUT,
  TREE_LIFT_STYLES
};

enum TreeLeafStyle
{
  TREE_LEAF_FAN,
  TREE_LEAF_SCATTER,
  TREE_LEAF_STYLES
};

struct BranchAnchor
{
  GLvector      root;
  float         radius;
  float         length;
  float         lift;
};

struct Leaf
{

  GLvector2     position;
  float         angle;
  float         size;
  //float         brightness;
  GLrgba        color;
  float         dist;
  unsigned      neighbor;
};

class CTree
{
  TreeTrunkStyle    _trunk_style;
  TreeFoliageStyle  _foliage_style;
  TreeLiftStyle     _lift_style;
  TreeLeafStyle     _leaf_style;
  
  int               _seed;
  int               _seed_current;
  bool              _funnel_trunk;
  bool              _evergreen;
  bool              _canopy;
  bool              _grows_high;
  bool              _has_vines;
  
  int               _default_branches;
  float             _default_height;
  float             _default_bend_frequency;
  float             _default_base_radius;
  float             _default_lowest_branch;

  int               _current_branches;
  float             _current_height;
  float             _current_bend_frequency;
  float             _current_angle_offset;
  float             _current_base_radius;
  float             _current_lowest_branch;

  float             _moisture;
  float             _temperature;

  float             _texture_tile;
  
  float             _branch_lift;
  float             _branch_reach;
  float             _foliage_size;
  float             _leaf_size;
  GLrgba            _bark_color1;
  GLrgba            _bark_color2;
  GLrgba            _leaf_color;
  vector<Leaf>      _leaf_list;
  GLmesh            _meshes[TREE_ALTS][LOD_LEVELS];

  void              DrawBark ();
  void              DrawLeaves ();
  void              DrawVines ();
  void              DrawFacer ();
  void              DoVines (GLmesh* m, GLvector* points, unsigned segments);
  void              DoFoliage (GLmesh* m, GLvector pos, float size, float angle);
  void              DoBranch (GLmesh* m, BranchAnchor anchor, float angle, LOD lod);
  void              DoTrunk (GLmesh* m, unsigned local_seed, LOD lod);
  void              DoLeaves ();
  void              DoTexture ();
  GLvector          TrunkPosition (float delta, float* radius);
  void              Build ();
public:
  unsigned          _texture;
  void              Create (bool canopy, float moisture, float temperature, int seed);
  void              Render (GLvector pos, unsigned alt, LOD lod);
  unsigned          Texture () { return _texture; };
  void              TexturePurge ();
  GLmesh*           Mesh (unsigned alt, LOD lod);
  void              Info ();
  bool              GrowsHigh () { return _grows_high; };


};