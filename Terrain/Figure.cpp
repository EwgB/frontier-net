/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cfigure.h"

/////////////////////////
#include "avatar.h"
#include "file.h"
#include "input.h"
#include "log.h"
/////////////////////////


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
  0.1f,-0.1f, 0.0f,     BONE_RTOE,      BONE_RANKLE,

 -0.1f, 0.0f, 1.0f,     BONE_LHIP,      BONE_PELVIS,
 -0.1f, 0.0f, 0.5f,     BONE_LKNEE,     BONE_LHIP,
 -0.1f, 0.0f, 0.0f,     BONE_LANKLE,    BONE_LKNEE,
 -0.1f,-0.1f, 0.0f,     BONE_LTOE,      BONE_LANKLE,
  
  0.0f, 0.0f, 1.55f,    BONE_SPINE1,    BONE_PELVIS,

  0.2f, 0.0f, 1.5f,     BONE_RSHOULDER, BONE_SPINE1,
  0.2f, 0.0f, 1.2f,     BONE_RELBOW,    BONE_RSHOULDER, 
  0.2f, 0.0f, 0.9f,     BONE_RWRIST,    BONE_RELBOW,

 -0.2f, 0.0f, 1.5f,     BONE_LSHOULDER, BONE_SPINE1,
 -0.2f, 0.0f, 1.2f,     BONE_LELBOW,    BONE_LSHOULDER, 
 -0.2f, 0.0f, 0.9f,     BONE_LWRIST,    BONE_LELBOW,

  0.0f, 0.0f, 1.6f,     BONE_NECK,      BONE_SPINE1,
  0.0f, 0.0f, 1.65f,    BONE_HEAD,      BONE_NECK,    
  0.0f,-0.2f, 1.65f,    BONE_FACE,      BONE_HEAD,
  0.0f, 0.0f, 1.8f,     BONE_CROWN,     BONE_FACE,

};

#define UP  glVector (0.0f, 0.0f, 1.0f)

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




static CFigure      fig;
static CAnim        anim;

void add_hull (CFigure* f, GLvector p, float d, float h, BoneId id)
{

  unsigned    base;
  GLmesh*     m;

  m = f->Skin ();
  base = m->Vertices ();
  m->PushVertex (glVector (p.x, p.y, p.z), UP, glVector (0.0f, 0.0f));
  m->PushVertex (glVector (p.x, p.y + d, p.z), UP, glVector (0.0f, 0.0f));
  m->PushVertex (glVector (p.x, p.y + d, p.z + h), UP, glVector (0.0f, 0.0f));
  m->PushVertex (glVector (p.x, p.y, p.z + h), UP, glVector (0.0f, 0.0f));
  m->PushQuad (base + 0, base + 1, base + 2, base + 3);
  m->PushQuad (base + 3, base + 2, base + 1, base + 0);
  f->PushWeight (id, base, 1.0f);
  f->PushWeight (id, base + 1, 1.0f);
  f->PushWeight (id, base + 2, 1.0f);
  f->PushWeight (id, base + 3, 1.0f);

}

void FigureInit ()
{

  unsigned    i;
  GLmesh*     skin;

  for (i = 0; i < sizeof (bl) / sizeof (BoneList); i++) 
    fig.PushBone (bl[i].id, bl[i].id_parent, bl[i].pos);
  skin = fig.Skin ();

  add_hull (&fig, glVector ( 0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BONE_RKNEE);
  add_hull (&fig, glVector (-0.1f, 0.05f, 0.5f), -0.1f, -0.4f, BONE_LKNEE);
  
  add_hull (&fig, glVector ( 0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BONE_RHIP);
  add_hull (&fig, glVector (-0.1f, 0.05f, 1.0f), -0.1f, -0.5f, BONE_LHIP);

  add_hull (&fig, glVector ( 0.2f, 0.05f, 1.5f), -0.1f, -0.3f, BONE_RSHOULDER);
  add_hull (&fig, glVector ( 0.2f, 0.05f, 1.2f), -0.1f, -0.3f, BONE_RELBOW);

  add_hull (&fig, glVector (-0.2f, 0.05f, 1.5f), -0.1f, -0.3f, BONE_LSHOULDER);
  add_hull (&fig, glVector (-0.2f, 0.05f, 1.2f), -0.1f, -0.3f, BONE_LELBOW);


  anim.LoadBvh ("Anims//run.bvh");

}

static unsigned   frame;


void FigureRender ()
{

  static float nn;

  nn += 0.05f;
  /*
  fig.RotateBone (BONE_SPINE1, glVector (0.0f, 0.0f, sin (nn * 3) * 25.0f));
  fig.RotateBone (BONE_RSHOULDER, glVector (0.0f, abs (sin (nn * 1)) * -80.0f, 0.0f));
  fig.RotateBone (BONE_RELBOW, glVector (abs (cos (nn * 1)) * 45.0f, 0.0f, 0.0f));
  fig.RotateBone (BONE_LSHOULDER, glVector (0.0f, abs (sin (nn * 3)) * 80.0f, 0.0f));
  fig.RotateBone (BONE_LELBOW, glVector (abs (cos (nn * 2)) * 90.0f, 0.0f, 0.0f));
  fig.RotateBone (BONE_RHIP, glVector (sin (nn) * 25.0f, 0.0f,  0.0f));
  fig.RotateBone (BONE_RKNEE, glVector (-abs (cos (nn * 2) * 45.0f), 0.0f,  0.0f));
  */
  
  for (unsigned i = 0; i < anim._frame[frame].joint.size (); i++) {
    //if (anim._frame[frame].joint[i].id > BONE_PELVIS)
      fig.RotateBone (anim._frame[frame].joint[i].id, anim._frame[frame].joint[i].rotation);
  }
  frame++;
  frame %= anim._frame.size ();
  
  
  fig.Update ();
  if (InputKeyPressed (SDLK_f))
    fig.PositionSet (AvatarPosition () + glVector (0.0f, -2.0f, 0.0f));
  glBindTexture (GL_TEXTURE_2D, 0);
  glDisable (GL_LIGHTING);
  fig.Render ();
  

}
