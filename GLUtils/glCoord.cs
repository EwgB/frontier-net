/*-----------------------------------------------------------------------------
  glCoord.cpp
  2011 Shamus Young
-------------------------------------------------------------------------------
  Coord is a struct for manipulating a pair of ints. Good for grid-walking.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"

struct GLcoord
{
  int         x;
  int         y;
  void        Clear ();
  bool        Walk (int x_width, int y_width);
  bool        Walk (int size);
  
  bool        operator== (const GLcoord& c);
  bool        operator!= (const GLcoord& c);
  
  GLcoord     operator+  (const int& c);
  GLcoord     operator+  (const GLcoord& c);
  void        operator+= (const float& c) { x += (int)c; y += (int)c; };
  void        operator+= (const int& c) { x += c; y += c; };
  void        operator+= (const GLcoord& c) { x += c.x; y += c.y; };

  GLcoord     operator-  (const int& c);
  GLcoord     operator-  (const GLcoord& c);
  void        operator-= (const float& c) { x -= (int)c; y -= (int)c; };
  void        operator-= (const int& c) { x -= c; y -= c; };
  void        operator-= (const GLcoord& c) { x -= c.x; y -= c.y; };

  GLcoord     operator*  (const int& c);
  GLcoord     operator*  (const GLcoord& c);
  void        operator*= (const int& c) { x *= c; y *= c; };
  void        operator*= (const GLcoord& c) { x *= c.x; y *= c.y; };
};

bool GLcoord::operator== (const GLcoord& c)
{
  if (x == c.x && y == c.y)
    return true;
  return false;
}

bool GLcoord::operator!= (const GLcoord& c)
{
  if (x == c.x && y == c.y)
    return false;
  return true;
}

GLcoord GLcoord::operator- (const GLcoord& c)
{
  GLcoord result;
  result.x = x - c.x;
  result.y = y - c.y;
  return result;
}

GLcoord GLcoord::operator- (const int& c)
{
  GLcoord result;
  result.x = x - c;
  result.y = y - c;
  return result;
}

GLcoord GLcoord::operator* (const GLcoord& c)
{
  GLcoord result;
  result.x = x * c.x;
  result.y = y * c.y;
  return result;
}

GLcoord GLcoord::operator* (const int& c)
{
  GLcoord result;
  result.x = x * c;
  result.y = y * c;
  return result;
}

GLcoord GLcoord::operator+ (const GLcoord& c)
{
  GLcoord result;
  result.x = x + c.x;
  result.y = y + c.y;
  return result;
}

GLcoord GLcoord::operator+ (const int& c)
{
  GLcoord result;
  result.x = x + c;
  result.y = y + c;
  return result;
}

bool GLcoord::Walk (int x_size, int y_size)
{
  x++;
  if (x >= x_size) {
    y++;
    x = 0;
    if (y >= y_size) {
      y = 0; 
      return true;
    }
  }
  return false;
}

bool GLcoord::Walk (int size)
{
  return Walk (size, size);
}

void GLcoord::Clear ()
{
  x = y = 0;
}
*/