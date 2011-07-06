#ifndef CANIM_H
#include "canim.h"
#endif

struct BWeight
{
  unsigned          _index;
  float             _weight;
};


struct PWeight
{
  BoneId            _bone;
  float             _weight;
};

struct Bone
{
  BoneId            _id;
  BoneId            _id_parent;
  GLvector          _origin;
  GLvector          _position;
  GLvector          _rotation;
  GLrgba            _color;
  vector<unsigned>  _children;
  vector<BWeight>   _vertex_weights;
  GLmatrix          _matrix;
};

class CFigure
{
  void              RotateHierarchy (unsigned id, GLvector offset, GLmatrix m);
  void              RotatePoints (unsigned id, GLvector offset, GLmatrix m);
public:
  vector<Bone>      _bone;
  GLvector          _position;
  GLvector          _rotation;
  unsigned          _bone_index[BONE_COUNT];
  unsigned          _unknown_count;
  GLmesh            _skin_static;//The original, "read only"
  GLmesh            _skin_deform;//Altered
  GLmesh            _skin_render;//Updated every frame
  

  CFigure ();
  void              Animate (CAnim* anim, float delta);
  void              Clear ();
  bool              LoadX (char* filename);
  BoneId            IdentifyBone (char* name);
  void              PositionSet (GLvector pos) { _position = pos; };
  void              RotationSet (GLvector rot);
  void              RotateBone (BoneId id, GLvector angle);
  void              PushBone (BoneId id, unsigned parent, GLvector pos);
  void              PushWeight (unsigned id, unsigned index, float weight);
  void              Prepare ();
  void              BoneInflate (BoneId id, float distance, bool do_children);

  void              Render ();
  void              RenderSkeleton ();
  GLmesh*           Skin () { return &_skin_static; };
  void              Update ();
};
