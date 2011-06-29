/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cfigure.h"

/////////////////////////
#include "avatar.h"
#include "input.h"
/////////////////////////

#define UP  glVector (0.0f, 0.0f, 1.0f)



static Figure     fig;

void FigureInit ()
{

  unsigned    i;
  GLmesh*     skin;

  for (i = 0; i < sizeof (bl) / sizeof (BoneList); i++) 
    fig.PushBone (bl[i].id, bl[i].id_parent, bl[i].pos);
  skin = fig.Skin ();
  skin->PushVertex (glVector (0.2f,-0.1f, 0.5f), UP, glVector (0.0f, 0.0f));
  skin->PushVertex (glVector (0.2f, 0.1f, 0.5f), UP, glVector (0.0f, 0.0f));
  skin->PushVertex (glVector (0.2f, 0.1f, 0.1f), UP, glVector (0.0f, 0.0f));
  skin->PushVertex (glVector (0.2f,-0.1f, 0.1f), UP, glVector (0.0f, 0.0f));
  skin->PushQuad (0, 1, 2, 3);
  skin->PushQuad (3, 2, 1, 0);
  fig.PushWeight (BONE_RKNEE, 0, 1.0f);
  fig.PushWeight (BONE_RKNEE, 1, 1.0f);
  fig.PushWeight (BONE_RKNEE, 2, 1.0f);
  fig.PushWeight (BONE_RKNEE, 3, 1.0f);

}

void FigureRender ()
{

  static float nn;

  nn += 0.05f;
  //fig.RotateBone (BONE_SPINE1, glVector (0.0f, 0.0f, sin (nn * 3) * 25.0f));
  //fig.RotateBone (BONE_RSHOULDER, glVector (0.0f, abs (sin (nn * 3)) * -80.0f, 0.0f));
  //fig.RotateBone (BONE_LSHOULDER, glVector (0.0f, abs (sin (nn * 3)) * 80.0f, 0.0f));
  fig.RotateBone (BONE_RHIP, glVector (sin (nn) * 25.0f, 0.0f,  0.0f));
  fig.RotateBone (BONE_RKNEE, glVector (-abs (cos (nn * 2) * 45.0f), 0.0f,  0.0f));
  fig.Update ();
  if (InputKeyPressed (SDLK_f))
    fig.PositionSet (AvatarPosition () + glVector (2.0f, 0.0f, 0.0f));
  glBindTexture (GL_TEXTURE_2D, 0);
  glDisable (GL_LIGHTING);
  fig.Render ();
  

}
