namespace FrontierSharp.Animation {
    using System;
    using System.Collections.Generic;

    using Common.Animation;

    public class Anim : IAnimation {
        private IList<AnimFrame> frames;

        public IList<AnimJoint> GetFrame(float delta) {
            throw new NotImplementedException();
        }

        public int JointCount => frames[0].joint.size();

        public BoneId BoneFromString(string name) {
            if (name.Contains("ROOT"))
                return BoneId.Root;
            if (name.Contains("PELVIS"))
                return BoneId.Pelvis;
            if (name.Contains("HIP")) {
                // If not left or right, then probably actually the pelvis.
                return name.Contains("L") ? BoneId.LeftHip : (name.Contains("R") ? BoneId.RightHip : BoneId.Pelvis);
            }

            if (name.Contains("THIGH")) {
                return name.Contains("L") ? BoneId.LeftHip : (name.Contains("R") ? BoneId.RightHip : BoneId.Invalid);
            }

            if (name.Contains("SHIN")) {
                return name.Substring(4).Contains("L")
                    ? BoneId.LeftKnee
                    : (name.Substring(4).Contains("R") ? BoneId.RightKnee : BoneId.Invalid);
            }
            //if (strstr (name, "KNEE")) {
            //  if (strchr (name + 4, 'L'))
            //    return BoneId.LeftKnee;
            //  if (strchr (name + 4, 'R'))
            //    return BoneId.RightKnee;
            //  //Not left or right.  
            //  return BoneId.Invalid;
            //}

            if (name.Contains("FOOT")) {
                return name.Substring(4).Contains("L")
                    ? BoneId.LeftAnkle
                    : (name.Substring(4).Contains("R") ? BoneId.RightAnkle : BoneId.Invalid);
            }

            //if (strstr (name, "ANKLE")) {
            //  if (strchr (name + 5, 'L'))
            //    return BoneId.LeftAnkle;
            //  if (strchr (name + 5, 'R'))
            //    return BoneId.RightAnkle;
            //  //Not left or right.  
            //  return BoneId.Invalid;
            //}

            if (name.Contains("BACK") || name.Contains("SPINE"))
                return BoneId.Spine1;
            if (name.Contains("NECK"))
                return BoneId.Neck;
            if (name.Contains("HEAD"))
                return BoneId.Head;
            if (name.Contains("SHOULDER")) {
                return name.Substring(8).Contains("L")
                    ? BoneId.LeftShoulder
                    : (name.Substring(8).Contains("R") ? BoneId.RightShoulder : BoneId.Invalid);
            }

            if (name.Contains("FOREARM")) {
                return name.Substring(7).Contains("L")
                    ? BoneId.LeftElbow
                    : (name.Substring(7).Contains("R") ? BoneId.RightElbow : BoneId.Invalid);
            }

            int pos;
            if ((pos = name.IndexOf("UPPERARM", StringComparison.Ordinal)) > 0) {
                var test = name.Substring(pos + 8);
                return test.Contains("L") ? BoneId.LeftArm : (test.Contains("R") ? BoneId.RightArm : BoneId.Invalid);
            }

            //if (strstr (name, "ELBOW")) {
            //  if (strchr (name + 7, 'L'))
            //    return BoneId.LeftELBOW;
            //  if (strchr (name + 7, 'R'))
            //    return BoneId.RightELBOW;
            //  //Not left or right? That can't be right.
            //  return BoneId.Invalid;
            //}

            if (name.Contains("TOE")) {
                return name.Contains("L") ? BoneId.LeftToe : (name.Contains("R") ? BoneId.RightToe : BoneId.Invalid);
            }

            if (name.Contains("HAND")) {
                return name.Contains("L")
                    ? BoneId.LeftWrist
                    : (name.Contains("R") ? BoneId.RightWrist : BoneId.Invalid);
            }

            if (name.Contains("FINGERS1")) {
                return name.Substring(7).Contains("L")
                    ? BoneId.LeftFingers1
                    : (name.Substring(7).Contains("R") ? BoneId.RightFingers1 : BoneId.Invalid);
            }

            if (name.Contains("FINGERS2")) {
                return name.Substring(7).Contains("L")
                    ? BoneId.LeftFingers2
                    : (name.Substring(7).Contains("R") ? BoneId.RightFingers2 : BoneId.Invalid);
            }

            if (name.Contains("THUMB1")) {
                return name.Contains("L")
                    ? BoneId.LeftThumb1
                    : (name.Contains("R") ? BoneId.RightThumb1 : BoneId.Invalid);
            }

            if (name.Contains("THUMB2")) {
                return name.Contains("L")
                    ? BoneId.LeftThumb2
                    : (name.Contains("R") ? BoneId.RightThumb2 : BoneId.Invalid);
            }

            return BoneId.Invalid;

        }
    }
}

/*From CAnim.h
 * #define CANIM_H

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
  AnimFrame         _current;
  void              SetDefaultAnimation ();
  unsigned          Frames () { return _frame.size (); };
  unsigned          Id (unsigned frame, unsigned index) { return _frame[frame].joint[index].id; };
  GLvector          Rotation (unsigned frame, unsigned index) { return _frame[frame].joint[index].rotation; };
  bool              LoadBvh (string filename);
  static string      NameFromBone (BoneId id);
  
};
 */

/*From CAnim.cpp
 *

  Loads animations and applies them to models.  (CFigures)
#include "stdafx.h"
#include "canim.h"
#include "console.h"
#include "cfigure.h"
#include "file.h"
#include "math.h"

#define NEWLINE     "\n"


string NameFromBone(BoneId id) {
    switch (id) {
        case BONE_ROOT:
            return "Root";
        case BONE_PELVIS:
            return "Pelvis";
        case BONE_RHIP:
            return "Hip Right";
        case BONE_LHIP:
            return "Hip Left";
        case BONE_RKNEE:
            return "Knee Right";
        case BONE_LKNEE:
            return "Knee Left";
        case BONE_RANKLE:
            return "Ankle Right";
        case BONE_LANKLE:
            return "Ankle Left";
        case BONE_RTOE:
            return "Toe Right";
        case BONE_LTOE:
            return "Toe Left";
        case BONE_SPINE1:
        case BONE_SPINE2:
        case BONE_SPINE3:
            return "Spine";
        case BONE_RSHOULDER:
            return "Shoulder Right";
        case BONE_LSHOULDER:
            return "Shoulder Left";
        case BONE_RARM:
            return "Arm Right";
        case BONE_LARM:
            return "Arm Left";
        case BONE_RELBOW:
            return "Elbow Right";
        case BONE_LELBOW:
            return "Elbow Left";
        case BONE_RWRIST:
            return "Wrist Right";
        case BONE_LWRIST:
            return "Wrist Left";
        case BONE_NECK:
            return "Neck";
        case BONE_HEAD:
            return "Head";
        case BONE_FACE:
        case BONE_CROWN:
        case BONE_Invalid:
            return "Bone Invalid";
    }
    return "Unknown";

}



//This makes a one-frame do-nothing animating, so we don't crash when an animating if missing.
void SetDefaultAnimation() {

    AnimJoint joint;

    _frame.clear();
    _frame.resize(1);
    joint.id = BONE_PELVIS;
    joint.rotation = glVector(0.0f, 0.0f, 0.0f);
    _frame[0].joint.push_back(joint);

}

bool LoadBvh(string filename) {

    bool done;
    long size;
    string buffer;
    string token;
    string find;
    vector<BoneId> dem_bones;
    int channels;
    unsigned frames;
    unsigned frame;
    unsigned bone;
    BoneId current_id;
    AnimJoint joint;
    string path;

    path.assign("anims//");
    path.append(filename);
    if (!strchr(filename, '.'))
        path.append(".bvh");
    buffer = FileLoad((string)path.c_str(), &size);
    if (!buffer) {
        ConsoleLog("LoadBvh: Can't find %s", (string)path.c_str());
        SetDefaultAnimation();
        return false;
    }
    _strupr(buffer);
    done = false;
    channels = 3;
    current_id = BONE_Invalid;
    token = strtok(buffer, NEWLINE);
    while (!done) {
        if (find = strstr(token, "CHANNEL")) {
            channels = atoi(find + 8);
            //Six channels means first 3 are position.  Ignore.
            if (channels == 6)
                dem_bones.push_back(BONE_Invalid);
            dem_bones.push_back(current_id);
        }
        if (find = strstr(token, "JOINT")) {
            find += 5; //skip the word joint
            current_id = BoneFromString(find);
        }
        if (find = strstr(token, "MOTION")) {//we've reached the final section of the file
            token = strtok(NULL, NEWLINE);
            frames = 0;
            if (find = strstr(token, "FRAMES"))
                frames = atoi(find + 7);
            _frame.clear();
            _frame.resize(frames);
            token = strtok(NULL, NEWLINE);//throw away "frame time" line.
            for (frame = 0; frame < frames; frame++) {
                token = strtok(NULL, NEWLINE);
                find = token;
                for (bone = 0; bone < dem_bones.size(); bone++) {
                    joint.id = dem_bones[bone];
                    joint.rotation.x = (float)atof(find);
                    find = strchr(find, 32) + 1;
                    joint.rotation.y = -(float)atof(find);
                    find = strchr(find, 32) + 1;
                    joint.rotation.z = -(float)atof(find);
                    find = strchr(find, 32) + 1;
                    if (joint.id != BONE_Invalid) {
                        _frame[frame].joint.push_back(joint);
                    }
                }
            }
            done = true;
        }
        token = strtok(NULL, NEWLINE);
    }
    free(buffer);
    return true;

}

AnimJoint* GetFrame(float delta) {

    unsigned i;
    unsigned frame;
    unsigned next_frame;
    AnimJoint aj;

    delta *= (float)Frames();
    frame = (unsigned)delta % Frames();
    next_frame = (frame + 1) % Frames();
    delta -= (float)frame;
    _current.joint.clear();
    for (i = 0; i < Joints(); i++) {
        aj.id = _frame[frame].joint[i].id;
        aj.rotation = glVectorInterpolate(_frame[frame].joint[i].rotation, _frame[next_frame].joint[i].rotation, delta);
        _current.joint.push_back(aj);
    }
    return &_current.joint[0];

}
 */
