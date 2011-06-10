/*-----------------------------------------------------------------------------

  glBbox.cpp

  2006 Shamus Young

-------------------------------------------------------------------------------
  
  This module has a few functions useful for manipulating the bounding-box 
  structs.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include <math.h>

#define MAX_VALUE               999999999999999.9f

void GLbbox::ContainPoint (GLvector point)
{

  min.x = MIN (min.x, point.x);
  min.y = MIN (min.y, point.y);
  min.z = MIN (min.z, point.z);
  max.x = MAX (max.x, point.x);
  max.y = MAX (max.y, point.y);
  max.z = MAX (max.z, point.z);


}

void GLbbox::Clear (void)
{

  max = glVector (-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
  min = glVector ( MAX_VALUE,  MAX_VALUE,  MAX_VALUE);

}

void GLbbox::Render ()
{
  //Bottom of box (Assuming z = up)
  glBegin (GL_LINE_STRIP);
  glVertex3f (min.x, min.y, min.z);
  glVertex3f (max.x, min.y, min.z);
  glVertex3f (max.x, max.y, min.z);
  glVertex3f (min.x, max.y, min.z);
  glVertex3f (min.x, min.y, min.z);
  glEnd ();
  //Top of box
  glBegin (GL_LINE_STRIP);
  glVertex3f (min.x, min.y, max.z);
  glVertex3f (max.x, min.y, max.z);
  glVertex3f (max.x, max.y, max.z);
  glVertex3f (min.x, max.y, max.z);
  glVertex3f (min.x, min.y, max.z);
  glEnd ();
  //Sides
  glBegin (GL_LINES);
  glVertex3f (min.x, min.y, min.z);
  glVertex3f (min.x, min.y, max.z);

  glVertex3f (max.x, min.y, min.z);
  glVertex3f (max.x, min.y, max.z);

  glVertex3f (max.x, max.y, min.z);
  glVertex3f (max.x, max.y, max.z);

  glVertex3f (min.x, max.y, min.z);
  glVertex3f (min.x, max.y, max.z);
  glEnd ();

}

/*-----------------------------------------------------------------------------
Does the given point fall within the given Bbox?
-----------------------------------------------------------------------------*/

bool glBboxTestPoint (GLbbox box, GLvector point)
{

  if (point.x > box.max.x || point.x < box.min.x)
    return false;
  if (point.y > box.max.y || point.y < box.min.y)
    return false;
  if (point.z > box.max.z || point.z < box.min.z)
    return false;
  return true;

}

/*-----------------------------------------------------------------------------
Expand Bbox (if needed) to contain given point
-----------------------------------------------------------------------------*/

GLbbox glBboxContainPoint (GLbbox box, GLvector point)
{

  box.min.x = MIN (box.min.x, point.x);
  box.min.y = MIN (box.min.y, point.y);
  box.min.z = MIN (box.min.z, point.z);
  box.max.x = MAX (box.max.x, point.x);
  box.max.y = MAX (box.max.y, point.y);
  box.max.z = MAX (box.max.z, point.z);
  return box;
  
}

/*-----------------------------------------------------------------------------
This will invalidate the bbox. 
-----------------------------------------------------------------------------*/

GLbbox glBboxClear (void)
{

  GLbbox      result;

  result.max = glVector (-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
  result.min = glVector ( MAX_VALUE,  MAX_VALUE,  MAX_VALUE);
  return result;

}

