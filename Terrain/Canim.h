#define CANIM_H

enum BoneId
{
  BONE_ORIGIN,
  BONE_PELVIS,
  BONE_RHIP,
  BONE_LHIP,
  BONE_RKNEE,
  BONE_LKNEE,
  BONE_RANKLE,
  BONE_LANKLE,
  BONE_RTOE,
  BONE_LTOE,
  BONE_SPINE1,
  BONE_SPINE2,
  BONE_SPINE3,
  BONE_RSHOULDER,
  BONE_LSHOULDER,
  BONE_RELBOW,
  BONE_LELBOW,
  BONE_RWRIST,
  BONE_LWRIST,
  BONE_NECK,
  BONE_HEAD,
  BONE_FACE,
  BONE_CROWN,
  BONE_COUNT,
  BONE_INVALID,
};


struct AnimJoint
{
  BoneId    id;
  GLvector  rotation;
};

struct AnimFrame
{
  vector<AnimJoint> joint;
};

class CAnim
{

public:
  vector<AnimFrame> _frame;
  bool              LoadBvh (char* filename);
  BoneId            BoneFromString (char* string);
  
};

