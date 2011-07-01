/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Loads animations and applies them to models.  (CFigures)

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "canim.h"
#include "cfigure.h"
#include "file.h"
#include "log.h"

#define NEWLINE     "\n"


char* CAnim::NameFromBone (BoneId id)
{
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
  case BONE_INVALID:
    return "Bone Invalid";
  }
  return "Unknown";

}


BoneId CAnim::BoneFromString (char* name)
{

  char*   test;

  if (strstr (name, "ROOT")) 
    return BONE_ROOT;
  if (strstr (name, "PELVIS")) 
    return BONE_PELVIS;
  if (strstr (name, "HIP")) {
    if (strchr (name, 'L'))
      return BONE_LHIP;
    if (strchr (name, 'R'))
      return BONE_RHIP;
    //Not left or right.  Probably actually the pelvis.
    return BONE_PELVIS;
  }
  if (strstr (name, "THIGH")) {
    if (strchr (name, 'L'))
      return BONE_LHIP;
    if (strchr (name, 'R'))
      return BONE_RHIP;
    //Not left or right.  
    return BONE_INVALID;
  }
  if (strstr (name, "SHIN")) {
    if (strchr (name + 4, 'L'))
      return BONE_LKNEE;
    if (strchr (name + 4, 'R'))
      return BONE_RKNEE;
    //Not left or right.  
    return BONE_INVALID;
  }
  /*
  if (strstr (name, "KNEE")) {
    if (strchr (name + 4, 'L'))
      return BONE_LKNEE;
    if (strchr (name + 4, 'R'))
      return BONE_RKNEE;
    //Not left or right.  
    return BONE_INVALID;
  }
  */
  
  if (strstr (name, "FOOT")) {
    if (strchr (name + 4, 'L'))
      return BONE_LANKLE;
    if (strchr (name + 4, 'R'))
      return BONE_RANKLE;
    //Not left or right.  
    return BONE_INVALID;
  }
  
  /*
  if (strstr (name, "ANKLE")) {
    if (strchr (name + 5, 'L'))
      return BONE_LANKLE;
    if (strchr (name + 5, 'R'))
      return BONE_RANKLE;
    //Not left or right.  
    return BONE_INVALID;
  }
  */
  if (strstr (name, "BACK")) 
    return BONE_SPINE1;
  if (strstr (name, "SPINE")) 
    return BONE_SPINE1;
  if (strstr (name, "NECK")) 
    return BONE_NECK;
  if (strstr (name, "HEAD")) 
    return BONE_HEAD;
  if (strstr (name, "SHOULDER")) {
    if (strchr (name + 8, 'L'))
      return BONE_LSHOULDER;
    if (strchr (name + 8, 'R'))
      return BONE_RSHOULDER;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "FOREARM")) {
    if (strchr (name + 7, 'L'))
      return BONE_LELBOW;
    if (strchr (name + 7, 'R'))
      return BONE_RELBOW;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (test = strstr (name, "UPPERARM")) {
    if (strchr (test + 8, 'L'))
      return BONE_LARM;
    if (strchr (test + 8, 'R'))
      return BONE_RARM;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  /*
  if (strstr (name, "ELBOW")) {
    if (strchr (name + 7, 'L'))
      return BONE_LELBOW;
    if (strchr (name + 7, 'R'))
      return BONE_RELBOW;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  */
  if (strstr (name, "TOE")) {
    if (strchr (name, 'L'))
      return BONE_LTOE;
    if (strchr (name, 'R'))
      return BONE_RTOE;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "HAND")) {
    if (strchr (name, 'L'))
      return BONE_LWRIST;
    if (strchr (name, 'R'))
      return BONE_RWRIST;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "FINGERS1")) {
    if (strchr (name + 7, 'L'))
      return BONE_LFINGERS1;
    if (strchr (name + 7, 'R'))
      return BONE_RFINGERS1;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "FINGERS2")) {
    if (strchr (name + 7, 'L'))
      return BONE_LFINGERS2;
    if (strchr (name + 7, 'R'))
      return BONE_RFINGERS2;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "THUMB1")) {
    if (strchr (name, 'L'))
      return BONE_LTHUMB1;
    if (strchr (name, 'R'))
      return BONE_RTHUMB1;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  if (strstr (name, "THUMB2")) {
    if (strchr (name, 'L'))
      return BONE_LTHUMB2;
    if (strchr (name, 'R'))
      return BONE_RTHUMB2;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  return BONE_INVALID;

}

bool CAnim::LoadBvh (char* filename)
{

  bool            done;
  long            size;
  char*           buffer;
  char*           token;
  char*           find;
  vector<BoneId>  dem_bones;
  int             channels;
  unsigned        frames;
  unsigned        frame;
  unsigned        bone;
  BoneId          current_id;
  AnimJoint       joint;
  string          path;

  path.assign ("anims//");
  path.append (filename);
  if (!strchr (filename, '.'))
    path.append (".bvh");
  buffer = FileLoad ((char*)path.c_str (), &size);
  if (!buffer)
    return false;
  _strupr (buffer);
  done = false;
  channels = 3;
  current_id = BONE_INVALID;
  token = strtok (buffer, NEWLINE);
  while (!done) {
    if (find = strstr (token, "CHANNEL")) {
      channels = atoi (find + 8);
      //Six channels means first 3 are position.  Ignore
      if (channels == 6)
        dem_bones.push_back (BONE_INVALID);
      dem_bones.push_back (current_id);
    }
    if (find = strstr (token, "JOINT")) {
      find += 5; //skip the word joint
      current_id = BoneFromString (find);
    }
    if (find = strstr (token, "MOTION")) {//we've reached the final section of the file
      for (unsigned i = 0; i < dem_bones.size (); i++) 
        Log ("%s", NameFromBone (dem_bones[i]));

      token = strtok (NULL, NEWLINE);
      frames = 0;
      if (find = strstr (token, "FRAMES"))
        frames = atoi (find + 7);
      _frame.clear ();
      _frame.resize (frames);
      token = strtok (NULL, NEWLINE);//throw away "frame time" line.
      for (frame = 0; frame < frames; frame++) {
        Log ("Frame #%d", frame); 
        token = strtok (NULL, NEWLINE);
        find = token;
        for (bone = 0; bone < dem_bones.size (); bone++) {
          joint.id = dem_bones[bone];
          joint.rotation.x = (float)atof (find);
          find = strchr (find, 32) + 1;
          joint.rotation.y = -(float)atof (find);
          find = strchr (find, 32) + 1;
          joint.rotation.z = -(float)atof (find);
          find = strchr (find, 32) + 1;
          if (joint.id != BONE_INVALID) {
            _frame[frame].joint.push_back (joint);
            //Log ("%s: %1.1f %1.1f %1.1f", CFigure::BoneName (joint.id), joint.rotation.x, joint.rotation.y, joint.rotation.z); 
          }
        }
      }
      done = true;
    }
    token = strtok (NULL, NEWLINE);
  }
  free (buffer);
  return true;

}



