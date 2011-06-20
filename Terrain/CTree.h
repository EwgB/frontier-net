
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
  TREE_FOLIAGE_PANEL,
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
  TreeTrunkStyle    _trunk_style;
  TreeFoliageStyle  _foliage_style;
  vector<UINT>      _index;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLvector2> _uv;
  VBO               _vbo;

  void              PushTriangle (int n1, int n2, int n3);
  void              DoFoliage (GLvector pos);
  void              DoBranch (BranchAnchor anchor, float angle);
  void              DoTexture ();
  GLvector          TrunkPosition (float delta, float* radius);
public:
  void              Build (GLvector pos);
  void              Render ();


};