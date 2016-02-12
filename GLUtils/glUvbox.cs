/*-----------------------------------------------------------------------------
  glUvbox.cpp
  2011 Shamus Young
-------------------------------------------------------------------------------
  This class is used for storing and and manipulating UV texture coords.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"

struct GLuvbox
{
  GLvector2 ul;
  GLvector2 lr;
  void      Set (GLvector2 ul, GLvector2 lr);
  void      Set (int x, int y, int columns, int rows);
  void      Set (float repeats);
  GLvector2 Corner (unsigned index);
  GLvector2 Center ();

};

void GLuvbox::Set (float repeats)
{
  ul = glVector (0.0f, 0.0f);
  lr = glVector (repeats, repeats);
}

void GLuvbox::Set (int x, int y, int columns, int rows)
{
  GLvector2   frame_size;

  frame_size.x = 1.0f / (float)columns;
  frame_size.y = 1.0f / (float)rows;

  ul = glVector ((float)x * frame_size.x, (float)y * frame_size.y);
  lr = glVector ((float)(x + 1) * frame_size.x, (float)(y + 1) * frame_size.y);
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
*/