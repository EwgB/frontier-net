/*-----------------------------------------------------------------------------

  glCoord.cpp

  2011 Shamus Young

-------------------------------------------------------------------------------

  Coord is a struct for manipulating a pair of ints. Good for grid-walking.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

bool GLcoord::operator== (const GLcoord& c)
{
  if (x == c.x && y == c.y)
    return true;
  return false;

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