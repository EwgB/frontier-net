/*-----------------------------------------------------------------------------

  glUvbox.cpp

  2011 Shamus Young

-------------------------------------------------------------------------------
  
  This class is used for storing and and manipulating UV texture coords.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

void GLuvbox::Set (float repeats)
{
  ul = glVector (0.0f, 0.0f);
  lr = glVector (repeats, repeats);
}


void GLuvbox::Set (GLvector2 ul_in, GLvector2 lr_in)
{
  ul = ul_in;
  lr = lr_in;
}

GLvector2 GLuvbox::Corner (unsigned index)
{

  switch (index) {
  case GLUV_TOP_LEFT:
    return ul;
  case GLUV_TOP_RIGHT:
    return glVector (lr.x, ul.y);
  case GLUV_BOTTOM_RIGHT:
    return lr;
  case GLUV_BOTTOM_LEFT:
    return glVector (ul.x, lr.y);
  case GLUV_LEFT_EDGE:
    return glVector (ul.x, (ul.y + lr.y) / 2);
  case GLUV_RIGHT_EDGE:
    return glVector (lr.x, (ul.y + lr.y) / 2);
  case GLUV_TOP_EDGE:
    return glVector ((ul.x + lr.x) / 2, ul.y);
  case GLUV_BOTTOM_EDGE:
    return glVector ((ul.x + lr.x) / 2, lr.y);
  }
  return glVector (0.0f, 0.0f);

}

GLvector2 GLuvbox::Center ()
{

  return (ul + lr) / 2;

}
