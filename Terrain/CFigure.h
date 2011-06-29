#ifndef CANIM_H
#include "canim.h"
#endif

struct BWeight
{
  unsigned          _index;
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
};

class CFigure
{
  GLvector          _position;
  vector<Bone>      _bone;
  unsigned          _bone_index[BONE_COUNT];
  
  GLmesh            _skin;
  GLmesh            _skin_static;
  void              RotateHierarchy (unsigned id, GLvector offset, GLmatrix m);
  void              RotatePoints (unsigned id, GLvector offset, GLmatrix m);
public:
  

  CFigure ();
  static char*      BoneName (BoneId id);
  void              PositionSet (GLvector pos) { _position = pos; };
  void              RotateBone (unsigned id, GLvector angle);
  void              PushBone (unsigned id, unsigned parent, GLvector pos);
  void              PushWeight (unsigned id, unsigned index, float weight);
  void              Render ();
  GLmesh*           Skin () { return &_skin_static; };
  void              Update ();
};
