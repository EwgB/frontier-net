namespace FrontierSharp.Common.Animation {
    public interface IAnimation {
    }
}

/* From CAnim.h
 #define CANIM_H

enum BoneId
{
  BONE_ROOT,
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
  BONE_RARM,
  BONE_LARM,
  BONE_RSHOULDER,
  BONE_LSHOULDER,
  BONE_RELBOW,
  BONE_LELBOW,
  BONE_RWRIST,
  BONE_LWRIST,
  BONE_RFINGERS1,
  BONE_LFINGERS1,
  BONE_RFINGERS2,
  BONE_LFINGERS2,
  BONE_RTHUMB1,
  BONE_LTHUMB1,
  BONE_RTHUMB2,
  BONE_LTHUMB2,
  BONE_NECK,
  BONE_HEAD,
  BONE_FACE,
  BONE_CROWN,
  BONE_UNKNOWN0,
  BONE_UNKNOWN1,
  BONE_UNKNOWN2,
  BONE_UNKNOWN3,
  BONE_UNKNOWN4,
  BONE_UNKNOWN5,
  BONE_UNKNOWN6,
  BONE_UNKNOWN7,
  BONE_UNKNOWN8,
  BONE_UNKNOWN9,
  BONE_UNKNOWN10,
  BONE_UNKNOWN11,
  BONE_UNKNOWN12,
  BONE_UNKNOWN13,
  BONE_UNKNOWN14,
  BONE_UNKNOWN15,
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
  AnimFrame         _current;
  AnimJoint*        GetFrame (float frame);  
  void              SetDefaultAnimation ();
  unsigned          Frames () { return _frame.size (); };
  unsigned          Joints () { return _frame[0].joint.size (); };
  unsigned          Id (unsigned frame, unsigned index) { return _frame[frame].joint[index].id; };
  GLvector          Rotation (unsigned frame, unsigned index) { return _frame[frame].joint[index].rotation; };
  bool              LoadBvh (char* filename);
  static BoneId     BoneFromString (char* string);
  static char*      NameFromBone (BoneId id);
  
};

*/
