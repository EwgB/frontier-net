/*-----------------------------------------------------------------------------
  Entropy.cpp
-------------------------------------------------------------------------------
  This provides a map of erosion-simulated terrain data.  This map is kept
  at non-powers-of-2 sizes in order to avoid tiling as much as possible.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include <stdio.h>
#include "file.h"
#include "texture.h"

float Entropy (float x, float y);
float Entropy (int x, int y);

#define ENTROPY_FILE      "entropy.raw"
#define BLUR_RADIUS       3
#define INDEX(x,y)        ((x % size.x) + (y % size.y) * size.x)

static bool       loaded;
static GLcoord    size;
static float*     emap;

static int entropy_index (GLcoord n)
{
  if (n.x < 0)
    n.x = abs (n.x);
  if (n.y < 0)
    n.y = abs (n.y);
  n.x %= size.x;
  n.y %= size.y;
  return n.x + n.y * size.x;
}

static int entropy_index (int x, int y)
{
  GLcoord   n;

  n.x = x;
  n.y = y;
  return entropy_index (n);
}

static void entropy_erode ()
{
  float*  buffer;
  int     x, y;
  float   low, high, val;
  GLcoord current;
  GLcoord low_index, high_index;
  GLcoord n;
  int     index;
  int     count;

  buffer = new float[size.x * size.y];
  memcpy (buffer, emap, sizeof (float) * size.x * size.y);
  //Pass over the entire map, dropping a "raindrop" on each point. Trace
  //a path downhill until the drop hits bottom. Subtract elevation
  //along the way.  Makes natural hells from handmade ones. Super effective.
  for (int pass = 0; pass < 3; pass++) {
    for (y = 0; y < size.y; y++) {
      for (x = 0; x < size.x; x++) {
        low = high = buffer[x + y * size.x];
        current.x = x;
        current.y = y;
        low_index = high_index = current;
        while (1) {
          //look for neighbors lower than this point
          for (n.x = current.x - 1; n.x <= current.x + 1; n.x++) {
            for (n.y = current.y - 1; n.y <= current.y + 1; n.y++) {
              index = entropy_index (n);
              if (emap[index] >= high) {
                high = emap[index];
                high_index = n;
              }
              if (emap[index] <= low) {
                low = emap[index];
                low_index = n;
              }
            }
          }
          //Search done.  
          //Sanity checks
          if (low_index.x < 0)
            low_index.x += size.x;
          if (low_index.y < 0)
            low_index.y += size.y;
          low_index.x %= size.x;
          low_index.y %= size.y;
          //If we didn't move, then we're at the lowest point
          if (low_index == current)
            break;
          index = entropy_index (current);
          //If we're at the highest point around, we're on a spike.
          //File that sucker down.
          if (high_index == current)
            buffer[index] *= 0.95f;
          //Erode this point a tiny bit, and move down.
          buffer[index] *= 0.97f;
          current = low_index;
        }
      }
    }
    memcpy (emap, buffer, sizeof (float) * size.x * size.y);
  }

  //Blur the elevations a bit to round off little spikes and divots.
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      val = 0.0f;
      count = 0;
      for (n.x = -BLUR_RADIUS; n.x <= BLUR_RADIUS; n.x++) {
        for (n.y = -BLUR_RADIUS; n.y <= BLUR_RADIUS; n.y++) {
          current.x = ((x + n.x) + size.x) % size.x;
          current.y = ((y + n.y) + size.y) % size.y;
          index = entropy_index (current);
          val += buffer[index];
          count++;
        }
      }
      val /= (float)count;
      emap[index] = (emap[index] + val) / 2.0f;
      emap[index] = val;
    }
  }
  delete[] buffer;
  //re-normalize the map
  high = 0;
  low = 999999;
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      index = entropy_index (x, y);
      high = max (emap[index], high);
      low = min (emap[index], low);
    }
  }
  high = high - low;
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      index = entropy_index (x, y);
      emap[index] -= low;
      emap[index] /= high;
    }
  }
}

static void entropy_create (char* filename)
{
  FILE*           file;
  char*           buffer;
  int             x, y;
  int             elements;
  byte            red;
  GLvector2       offset;
  GLcoord         scan;
  
  if (!filename) 
		return;
  file = fopen (filename,"r");					
  if (!file)	
    return;
	fclose (file);
  buffer = FileImageLoad (filename, &size);
  elements = size.x * size.y;
  emap = new float [elements];
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      offset.x = (float)x / (float)size.x;
      offset.y = (float)y / (float)size.y;
      scan.x = (int)(offset.x * (float)size.x);
      scan.y = (int)(offset.y * (float)size.y);
      red = buffer[(scan.x + scan.y * size.x) * 4];
      emap[x + y * size.x] = (float)red / 255;
    }
  }
  entropy_erode ();
  file = fopen (ENTROPY_FILE, "wb");
  if (file) {
    fwrite (&size.x, sizeof (size.x), 1, file);
    fwrite (&size.y, sizeof (size.y), 1, file);
    fwrite (emap, sizeof (float), size.x * size.y, file);
    fclose (file);
  }
  delete buffer;
  loaded = true;
}

static void entropy_load ()
{
  FILE*     file;

  //entropy_create ("textures/noise256.bmp");
  file = fopen (ENTROPY_FILE, "rb");
  if (file) {
    fread (&size.x, sizeof (size.x), 1, file);
    fread (&size.y, sizeof (size.y), 1, file);
    emap = new float [size.x * size.y];
    fread (emap, sizeof (float), size.x * size.y, file);
    fclose (file);
    loaded = true;
  } else
    entropy_create ("textures/noise256.bmp");
}

float Entropy (int x, int y)
{
  if (!loaded) 
    entropy_load ();
  if (!emap || x < 0 || y < 0)
    return 0;
  return emap[(x % size.x) + (y % size.y) * size.x];
}

float Entropy (float x, float y)
{
  int     cell_x;
  int     cell_y;
  float   a;
  float   b;
  float   c;
  float   y0, y1, y2, y3;
  float   dx;
  float   dy;

  cell_x = (int)x;
  cell_y = (int)y;
  dx = (x - (float)cell_x);
  dy = (y - (float)cell_y);
  y0 = Entropy (cell_x, cell_y);
  y1 = Entropy (cell_x + 1, cell_y);
  y2 = Entropy (cell_x, cell_y + 1);
  y3 = Entropy (cell_x + 1, cell_y + 1);
  if (dx < dy) {
    c = y2 - y0; 
    b = y3 - y2; 
    a = y0;
  } else {
    c = y3 - y1; 
    b = y1 - y0; 
    a = y0;
  }
  return (a + b * dx + c * dy);
}
*/