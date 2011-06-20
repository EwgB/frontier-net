
enum TreeTrunkStyle
{
  TREE_TRUNK_NORMAL,
  TREE_TRUNK_JAGGED,
  TREE_TRUNK_BENT,
  TREE_TRUNK_TYPES
};

enum TreeFoliageStyle
{
  TREE_FOLIAGE_UMBRELLA,
  TREE_FOLIAGE_BOWL,
  TREE_FOLIAGE_PANEL,
  TREE_FOLIAGE_TYPES
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
  bool              _funnel_trunk;
  bool              _cap_foliage;
  int               _branches;
  float             _height;
  float             _base_radius;
  float             _lowest_branch;
  float             _branch_lift;
  float             _branch_reach;
  float             _trunk_bend;
  float             _foliage_size;
  float             _leaf_size;
  unsigned          _texture;
  int               _polygons;
  GLrgba            _bark_color1;
  GLrgba            _bark_color2;
  GLrgba            _leaf_color;
  TreeTrunkStyle    _trunk_style;
  TreeFoliageStyle  _foliage_style;
  vector<Leaf>      _leaf_list;
  vector<UINT>      _index;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLvector2> _uv;
  VBO               _vbo;

  void              PushTriangle (int n1, int n2, int n3);
  void              DoFoliage (GLvector pos, float size, float angle);
  void              DoLeaves ();
  void              DoBranch (BranchAnchor anchor, float angle);
  void              DoTexture ();
  GLvector          TrunkPosition (float delta, float* radius);
public:
  void              Build (GLvector pos);
  void              Render ();
  int               Polygons () { return _polygons; };


};