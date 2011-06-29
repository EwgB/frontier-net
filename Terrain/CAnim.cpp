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

BoneId CAnim::BoneFromString (char* name)
{

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
  if (strstr (name, "FOOT")) {
    if (strchr (name + 4, 'L'))
      return BONE_LANKLE;
    if (strchr (name + 4, 'R'))
      return BONE_RANKLE;
    //Not left or right.  
    return BONE_INVALID;
  }
  if (strstr (name, "BACK")) 
    return BONE_SPINE1;
  if (strstr (name, "NECK")) 
    return BONE_NECK;
  if (strstr (name, "HEAD")) 
    return BONE_HEAD;
  /*
  if (strstr (name, "SHOULDER")) {
    if (strchr (name + 8, 'L'))
      return BONE_LSHOULDER;
    if (strchr (name + 8, 'R'))
      return BONE_RSHOULDER;
    //Not left or right? That can't be right.
    return BONE_INVALID;
  }
  */
  if (strstr (name, "UPPERARM")) {
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


  buffer = FileLoad (filename, &size);
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
        Log ("%s", CFigure::BoneName (dem_bones[i]));

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
          joint.rotation.z = -(float)atof (find);
          find = strchr (find, 32) + 1;
          joint.rotation.y = (float)atof (find);
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



