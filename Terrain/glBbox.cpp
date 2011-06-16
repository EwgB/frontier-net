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

  pmin.x = min (pmin.x, point.x);
  pmin.y = min (pmin.y, point.y);
  pmin.z = min (pmin.z, point.z);
  pmax.x = max (pmax.x, point.x);
  pmax.y = max (pmax.y, point.y);
  pmax.z = max (pmax.z, point.z);


}

void GLbbox::Clear (void)
{

  pmax = glVector (-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
  pmin = glVector ( MAX_VALUE,  MAX_VALUE,  MAX_VALUE);

}

void GLbbox::Render ()
{
  //Bottom of box (Assuming z = up)
  glBegin (GL_LINE_STRIP);
  glVertex3f (pmin.x, pmin.y, pmin.z);
  glVertex3f (pmax.x, pmin.y, pmin.z);
  glVertex3f (pmax.x, pmax.y, pmin.z);
  glVertex3f (pmin.x, pmax.y, pmin.z);
  glVertex3f (pmin.x, pmin.y, pmin.z);
  glEnd ();
  //Top of box
  glBegin (GL_LINE_STRIP);
  glVertex3f (pmin.x, pmin.y, pmax.z);
  glVertex3f (pmax.x, pmin.y, pmax.z);
  glVertex3f (pmax.x, pmax.y, pmax.z);
  glVertex3f (pmin.x, pmax.y, pmax.z);
  glVertex3f (pmin.x, pmin.y, pmax.z);
  glEnd ();
  //Sides
  glBegin (GL_LINES);
  glVertex3f (pmin.x, pmin.y, pmin.z);
  glVertex3f (pmin.x, pmin.y, pmax.z);

  glVertex3f (pmax.x, pmin.y, pmin.z);
  glVertex3f (pmax.x, pmin.y, pmax.z);

  glVertex3f (pmax.x, pmax.y, pmin.z);
  glVertex3f (pmax.x, pmax.y, pmax.z);

  glVertex3f (pmin.x, pmax.y, pmin.z);
  glVertex3f (pmin.x, pmax.y, pmax.z);
  glEnd ();

}

/*-----------------------------------------------------------------------------
Does the given point fall within the given Bbox?
-----------------------------------------------------------------------------*/

bool glBboxTestPoint (GLbbox box, GLvector point)
{

  if (point.x > box.pmax.x || point.x < box.pmin.x)
    return false;
  if (point.y > box.pmax.y || point.y < box.pmin.y)
    return false;
  if (point.z > box.pmax.z || point.z < box.pmin.z)
    return false;
  return true;

}

/*-----------------------------------------------------------------------------
Expand Bbox (if needed) to contain given point
-----------------------------------------------------------------------------*/

GLbbox glBboxContainPoint (GLbbox box, GLvector point)
{

  box.pmin.x = min (box.pmin.x, point.x);
  box.pmin.y = min (box.pmin.y, point.y);
  box.pmin.z = min (box.pmin.z, point.z);
  box.pmax.x = max (box.pmax.x, point.x);
  box.pmax.y = max (box.pmax.y, point.y);
  box.pmax.z = max (box.pmax.z, point.z);
  return box;
  
}

/*-----------------------------------------------------------------------------
This will invalidate the bbox. 
-----------------------------------------------------------------------------*/

GLbbox glBboxClear (void)
{

  GLbbox      result;

  result.pmax = glVector (-MAX_VALUE, -MAX_VALUE, -MAX_VALUE);
  result.pmin = glVector ( MAX_VALUE,  MAX_VALUE,  MAX_VALUE);
  return result;

}

