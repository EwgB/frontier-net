
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
  float         brightness;
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
  bool              _no_branches;
  int               _branches;
  float             _height;
  float             _texture_tile;
  float             _base_radius;
  float             _lowest_branch;
  float             _branch_lift;
  float             _branch_reach;
  float             _trunk_bend;
  float             _trunk_bend_frequency;
  float             _foliage_size;
  float             _leaf_size;
  unsigned          _texture;
  int               _polygons;
  GLrgba            _bark_color1;
  GLrgba            _bark_color2;
  GLrgba            _leaf_color;
  vector<Leaf>      _leaf_list;
  GLmesh            _mesh;
  VBO               _vbo;

  void              PushTriangle (int n1, int n2, int n3);
  void              DoFoliage (GLvector pos, float size, float angle);
  void              DoLeaves ();
  void              DoBranch (BranchAnchor anchor, float angle);
  void              DoTexture ();
  GLvector          TrunkPosition (float delta, float* radius);
  void              Build ();
public:
  void              Create ( float moisture, float temperature, int seed);
  void              Render ();
  int               Polygons () { return _polygons; };
  unsigned          Texture () { return _texture; };
  GLmesh*           Mesh () { return &_mesh; };


};