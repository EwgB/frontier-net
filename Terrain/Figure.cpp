/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "canim.h"
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
  0.0f, 0.0f, 0.0f,     BONE_ROOT,      BONE_ROOT,
  0.0f, 0.0f, 1.1f,     BONE_PELVIS,    BONE_ROOT,
  0.1f, 0.0f, 1.0f,     BONE_RHIP,      BONE_PELVIS,
  0.1f, 0.0f, 0.5f,     BONE_RKNEE,     BONE_RHIP,
  0.1f, 0.0f, 0.0f,     BONE_RANKLE,    BONE_RKNEE,
  0.1f,-0.1f, 0.0f,     BONE_RTOE,      BONE_RANKLE,

 -0.1f, 0.0f, 1.0f,     BONE_LHIP,      BONE_PELVIS,
 -0.1f, 0.0f, 0.5f,     BONE_LKNEE,     BONE_LHIP,
 -0.1f, 0.0f, 0.0f,     BONE_LANKLE,    BONE_LKNEE,
 -0.1f,-0.1f, 0.0f,     BONE_LTOE,      BONE_LANKLE,
  
  0.0f, 0.0f, 1.55f,    BONE_SPINE1,    BONE_PELVIS,

  0.1f, 0.0f, 1.5f,     BONE_RSHOULDER, BONE_SPINE1,
  0.2f, 0.0f, 1.5f,     BONE_RARM,      BONE_RSHOULDER,
  0.4f, 0.0f, 1.5f,     BONE_RELBOW,    BONE_RARM, 
  0.8f, 0.0f, 1.5f,     BONE_RWRIST,    BONE_RELBOW,

 -0.1f, 0.0f, 1.5f,     BONE_LSHOULDER, BONE_SPINE1,
 -0.2f, 0.0f, 1.5f,     BONE_LARM,      BONE_LSHOULDER,
 -0.4f, 0.0f, 1.5f,     BONE_LELBOW,    BONE_LARM, 
 -0.8f, 0.0f, 1.5f,     BONE_LWRIST,    BONE_LELBOW,

  0.0f, 0.0f, 1.6f,     BONE_NECK,      BONE_SPINE1,
  0.0f, 0.0f, 1.65f,    BONE_HEAD,      BONE_NECK,    
  0.0f,-0.2f, 1.65f,    BONE_FACE,      BONE_HEAD,
  0.0f, 0.0f, 1.8f,     BONE_CROWN,     BONE_FACE,

};

#define UP  glVector (0.0f, 0.0f, 1.0f)




static CFigure      fig;
static CFigure      fig2;
static CAnim        anim;
static CAnim        anim_stand;

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

  add_hull (&fig, glVector ( 0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BONE_RSHOULDER);
  add_hull (&fig, glVector ( 0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BONE_RELBOW);

  add_hull (&fig, glVector (-0.2f, 0.05f, 1.5f), -0.1f, -0.1f, BONE_LSHOULDER);
  add_hull (&fig, glVector (-0.5f, 0.05f, 1.5f), -0.1f, -0.1f, BONE_LELBOW);

  fig.Prepare ();
  anim.LoadBvh ("Anims//run.bvh");
  anim_stand.LoadBvh ("Anims//stand.bvh");

  fig2.LoadX ("models//male.x");
//  fig2.BoneInflate (BONE_HEAD, 0.01f);
  /*
  {

    FILE*             file;
    file = fopen ("stand.bvh", "w+b");
    if (!file) 
      return;
    for (i = 0; i < fig._bone.size (); i++) {
      fprintf (file, "Joint %s\n", CAnim::NameFromBone (fig._bone[i]._id));
      fprintf (file, "CHANNELS 3\n");
    }
    fprintf (file, "Motion\n");
    fprintf (file, "Frames: 1\n");
    fprintf (file, "Frame Time: 1.0\n");
    for (i = 0; i < fig._bone.size (); i++) 
      fprintf (file, "0.0 0.0 0.0 ");
    fprintf (file, "\n");
    fclose (file);
  }
  */



}

static unsigned   frame;
static bool       moveit = true;
static bool       stand;

void FigureRender ()
{

  static float nn;

  if (moveit)
    nn += 0.03f;
  
  fig2.RotateBone (BONE_SPINE1, glVector (0.0f, 0.0f, sin (nn * 3) * 25.0f));
  fig2.RotateBone (BONE_RFINGERS1, glVector (0.0f, -abs (sin (nn * 1)) * -80.0f, 0.0f));
  //fig2.RotateBone (BONE_RELBOW, glVector (abs (cos (nn * 1)) * 45.0f, 0.0f, 0.0f));
  //fig2.RotateBone (BONE_LSHOULDER, glVector (0.0f, abs (sin (nn * 3)) * 80.0f, 0.0f));
  
  //fig2.RotateBone (BONE_LELBOW, glVector (0.0f, 0.0f, abs (cos (nn * 2)) * 90.0f));
  fig2.RotateBone (BONE_LWRIST, glVector (0.0f, abs (cos (nn * 2)) * 90.0f, 0.0f));
  //fig2.RotateBone (BONE_RHIP, glVector (sin (nn) * 25.0f, 0.0f,  0.0f));
  //fig2.RotateBone (BONE_RKNEE, glVector (-abs (cos (nn * 2) * 45.0f), 0.0f,  0.0f));
  
  
  /*
  for (unsigned i = 0; i < anim._frame[frame].joint.size (); i++) {
    //if (anim._frame[frame].joint[i].id > BONE_PELVIS)
      fig.RotateBone (anim._frame[frame].joint[i].id, anim._frame[frame].joint[i].rotation);
  }
  */
  if (stand) {
    //fig.Animate (&anim_stand, nn);
    //fig2.Animate (&anim_stand, nn);
  } else {
    //fig.Animate (&anim, nn);
    //fig2.Animate (&anim, nn);
  }
  frame++;
  //frame %= anim._frame.size ();
  //fig.Update ();
  //fig2.Update ();
  if (InputKeyPressed (SDLK_f)) {
    fig.PositionSet (AvatarPosition () + glVector (0.0f, -2.0f, 0.0f));
    fig2.PositionSet (AvatarPosition () + glVector (0.0f, 2.0f, 0.0f));
  }
  if (InputKeyPressed (SDLK_g))
    moveit = !moveit;
  if (InputKeyPressed (SDLK_h))
    stand = !stand;

  glBindTexture (GL_TEXTURE_2D, 0);
  //glDisable (GL_LIGHTING);
  //fig.Render ();
  //fig2.Render ();
  //glEnable (GL_LIGHTING);
  

}
