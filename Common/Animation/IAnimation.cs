namespace FrontierSharp.Common.Animation {
    using System.Collections.Generic;

    /// <summary>Loads animations and applies them to models.</summary>
    public interface IAnimation {
        IList<AnimJoint> GetFrame(float delta);
        int Joints();
        BoneId BoneFromString(string name);
    }
}

/* From CAnim.h

struct AnimFrame
{
  vector<AnimJoint> joint;
};

class CAnim
{

public:
  vector<AnimFrame> _frame;
  AnimFrame         _current;
  void              SetDefaultAnimation ();
  unsigned          Frames () { return _frame.size (); };
  unsigned          Joints () { return _frame[0].joint.size (); };
  unsigned          Id (unsigned frame, unsigned index) { return _frame[frame].joint[index].id; };
  GLvector          Rotation (unsigned frame, unsigned index) { return _frame[frame].joint[index].rotation; };
  bool              LoadBvh (char* filename);
  static char*      NameFromBone (BoneId id);
  
};

*/
