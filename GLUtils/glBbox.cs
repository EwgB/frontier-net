/*-----------------------------------------------------------------------------
  glBbox.cpp
  2006 Shamus Young
-------------------------------------------------------------------------------
  This module has a few functions useful for manipulating the bounding-box structs.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include <math.h>

struct GLbbox
{
  GLvector3   pmin;
  GLvector3   pmax;

  GLvector    Center ();
  void        ContainPoint (GLvector point);
  void        Clear ();
  void        Render ();
  GLvector    Size ();
};

#define MAX_VALUE               999999999999999.9f

GLvector GLbbox::Center ()
{
  return (pmin + pmax) / 2.0f;
}

GLvector GLbbox::Size ()
{
  return pmax - pmin;
}

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

void GLbbox::Render() {
	//Bottom of box (Assuming z = up)
	glBegin(GL_LINE_STRIP);
	glVertex3f(pmin.x, pmin.y, pmin.z);
	glVertex3f(pmax.x, pmin.y, pmin.z);
	glVertex3f(pmax.x, pmax.y, pmin.z);
	glVertex3f(pmin.x, pmax.y, pmin.z);
	glVertex3f(pmin.x, pmin.y, pmin.z);
	glEnd();
	//Top of box
	glBegin(GL_LINE_STRIP);
	glVertex3f(pmin.x, pmin.y, pmax.z);
	glVertex3f(pmax.x, pmin.y, pmax.z);
	glVertex3f(pmax.x, pmax.y, pmax.z);
	glVertex3f(pmin.x, pmax.y, pmax.z);
	glVertex3f(pmin.x, pmin.y, pmax.z);
	glEnd();
	//Sides
	glBegin(GL_LINES);
	glVertex3f(pmin.x, pmin.y, pmin.z);
	glVertex3f(pmin.x, pmin.y, pmax.z);

	glVertex3f(pmax.x, pmin.y, pmin.z);
	glVertex3f(pmax.x, pmin.y, pmax.z);

	glVertex3f(pmax.x, pmax.y, pmin.z);
	glVertex3f(pmax.x, pmax.y, pmax.z);

	glVertex3f(pmin.x, pmax.y, pmin.z);
	glVertex3f(pmin.x, pmax.y, pmax.z);
	glEnd();
}
*/