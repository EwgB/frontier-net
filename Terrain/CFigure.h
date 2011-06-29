
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

struct BoneList
{
  GLvector    pos;
  BoneId      id;
  BoneId      id_parent;
};

static BoneList  bl[] =
{
  0.0f, 0.0f, 1.1f,     BONE_PELVIS,    BONE_ORIGIN,
  0.1f, 0.0f, 1.0f,     BONE_RHIP,      BONE_PELVIS,
  0.1f, 0.0f, 0.5f,     BONE_RKNEE,     BONE_RHIP,
  0.1f, 0.0f, 0.0f,     BONE_RANKLE,    BONE_RKNEE,
  0.1f, 0.1f, 0.0f,     BONE_RTOE,      BONE_RANKLE,

 -0.1f, 0.0f, 1.0f,     BONE_LHIP,      BONE_PELVIS,
 -0.1f, 0.0f, 0.5f,     BONE_LKNEE,     BONE_LHIP,
 -0.1f, 0.0f, 0.0f,     BONE_LANKLE,    BONE_LKNEE,
 -0.1f, 0.1f, 0.0f,     BONE_LTOE,      BONE_LANKLE,
  
  0.0f, 0.0f, 1.55f,    BONE_SPINE1,    BONE_PELVIS,

  0.2f, 0.0f, 1.5f,     BONE_RSHOULDER, BONE_SPINE1,
  0.2f, 0.0f, 1.2f,     BONE_RELBOW,    BONE_RSHOULDER, 
  0.2f, 0.0f, 0.9f,     BONE_RWRIST,    BONE_RELBOW,

 -0.2f, 0.0f, 1.5f,     BONE_LSHOULDER, BONE_SPINE1,
 -0.2f, 0.0f, 1.2f,     BONE_LELBOW,    BONE_LSHOULDER, 
 -0.2f, 0.0f, 0.9f,     BONE_LWRIST,    BONE_LELBOW,

  0.0f, 0.0f, 1.6f,     BONE_NECK,      BONE_SPINE1,
  0.0f, 0.0f, 1.65f,    BONE_HEAD,      BONE_NECK,    
  0.0f, 0.2f, 1.65f,    BONE_FACE,      BONE_HEAD,
  0.0f, 0.0f, 1.8f,     BONE_CROWN,     BONE_FACE,

};

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

class Figure
{
  GLvector          _position;
  vector<Bone>      _bone;
  unsigned          _bone_index[BONE_COUNT];
  
  GLmesh            _skin;
  GLmesh            _skin_static;
  void              RotateHierarchy (unsigned id, GLvector offset, GLmatrix m);
  void              RotatePoints (unsigned id, GLvector offset, GLmatrix m);
public:
  

  Figure ();
  void              PositionSet (GLvector pos) { _position = pos; };
  void              RotateBone (unsigned id, GLvector angle);
  void              PushBone (unsigned id, unsigned parent, GLvector pos);
  void              PushWeight (unsigned id, unsigned index, float weight);
  void              Render ();
  GLmesh*           Skin () { return &_skin_static; };
  void              Update ();
};
