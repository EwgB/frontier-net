/*-----------------------------------------------------------------------------
  FileImage.cpp
-------------------------------------------------------------------------------
  This module uses devIL (http://openil.sourceforge.net) to open various
  image files.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include "il\il.h"

#define DEFAULT_SIZE      8

static unsigned     default_counter;

static char* do_default_image (GLcoord* size_in)
{
  GLrgba          color;
  unsigned char   bcolor[4];
  unsigned char   white[4];
  int             x, y;
  unsigned char*  buffer;
  unsigned char*  current;

  color = glRgbaUnique (default_counter++);
  bcolor[0] = (unsigned char)(color.red * 255.0f);
  bcolor[1] = (unsigned char)(color.green * 255.0f);
  bcolor[2] = (unsigned char)(color.blue * 255.0f);
  bcolor[3] = 255;
  memset (white, 255, 4);
  buffer = new unsigned char[DEFAULT_SIZE * DEFAULT_SIZE * 4];
  if (size_in) {
    size_in->x = DEFAULT_SIZE;
    size_in->y = DEFAULT_SIZE;
  }
  for (x = 0; x < DEFAULT_SIZE; x++) {
    for (y = 0; y < DEFAULT_SIZE; y++) {
      current = buffer + (x + y * DEFAULT_SIZE) * 4;
      if ((x + y) % 2) 
        memcpy (current, white, 4);
      else
        memcpy (current, bcolor, 4);
    }
  }
  return (char*)buffer;
}

char* FileImageLoad (char* filename, GLcoord* size_in)
{
  GLcoord size;
  int     ok;
  char*   buffer;

  ilEnable (IL_ORIGIN_SET);
  //if (strstr (filename, ".png"))
    ilOriginFunc(IL_ORIGIN_LOWER_LEFT);
  //else
    //ilOriginFunc(IL_ORIGIN_UPPER_LEFT);
  ok = ilLoadImage (filename);
  if (!ok)
    return do_default_image (size_in);
  size.x = ilGetInteger (IL_IMAGE_WIDTH);
  size.y = ilGetInteger (IL_IMAGE_HEIGHT);
  if (size_in) 
    *size_in = size; 
  buffer = new char[size.x * size.y * 4];
  ilCopyPixels (0, 0, 0, size.x, size.y, 1, IL_RGBA, IL_UNSIGNED_BYTE, buffer);
  return buffer;
}
*/