namespace FrontierSharp.Common.Animation {
    /// <summary>Loads animations and applies them to models.</summary>
    public interface IAnimation {
    }
}

/* From CAnim.h
 #define CANIM_H

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
