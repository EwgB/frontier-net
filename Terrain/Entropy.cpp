/*-----------------------------------------------------------------------------

  Entropy.cpp

-------------------------------------------------------------------------------

  This provides a map of erosion-simulated terrain data.  This map is kept
  at non-powers-of-2 sizes in order to avoid tiling as much as possible.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include <stdio.h>
#include "bmpfile.h"
#include "file.h"
#include "texture.h"

#define ENTROPY_FILE      "entropy.raw"
#define BLUR_RADIUS       2
#define INDEX(x,y)        ((x % size.x) + (y % size.y) * size.x)

static bool       loaded;
static GLcoord    size;
static float*     map;

/*-----------------------------------------------------------------------------
-----------------------------------------------------------------------------*/

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
  memcpy (buffer, map, sizeof (float) * size.x * size.y);
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
              if (map[index] >= high) {
                high = map[index];
                high_index = n;
              }
              if (map[index] <= low) {
                low = map[index];
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
    memcpy (map, buffer, sizeof (float) * size.x * size.y);
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
      map[index] = (map[index] + val) / 2.0f;
      map[index] = val;
    }
  }
  delete[] buffer;
  //re-normalize the map
  high = 0;
  low = 999999;
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      index = entropy_index (x, y);
      high = max (map[index], high);
      low = min (map[index], low);
    }
  }
  high = high - low;
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      index = entropy_index (x, y);
      map[index] -= low;
      map[index] /= high;
    }
  }

}

static void entropy_create (char* filename)
{

  FILE*     file;
  BMPFile*  b;
  int       x, y;
  int       elements;
  byte      red;
  GLvector2 offset;
  GLcoord   scan;
  
  if (!filename) 
		return;
  file = fopen (filename,"r");					
  if (!file)	
    return;
	fclose (file);
	b = ImageLoad (filename);		
  size.x = b->sizeX - 4;
  size.y = b->sizeY - 4;
  elements = size.x * size.y;
  map = new float [elements];
  for (y = 0; y < size.y; y++) {
    for (x = 0; x < size.x; x++) {
      offset.x = (float)x / (float)size.x;
      offset.y = (float)y / (float)size.y;
      scan.x = (int)(offset.x * (float)b->sizeX);
      scan.y = (int)(offset.y * (float)b->sizeY);
      red = b->data[(scan.x + scan.y * b->sizeX) * 3];
      map[x + y * size.x] = (float)red / 255;
    }
  }
  entropy_erode ();
  file = fopen (ENTROPY_FILE, "wb");
  if (file) {
    fwrite (&size.x, sizeof (size.x), 1, file);
    fwrite (&size.y, sizeof (size.y), 1, file);
    fwrite (map, sizeof (float), size.x * size.y, file);
    fclose (file);
  }
  delete b;
  loaded = true;

}

/*-----------------------------------------------------------------------------
-----------------------------------------------------------------------------*/

static void entropy_load ()
{

  FILE*     file;

  //entropy_create ("textures/noise256.bmp");
  file = fopen (ENTROPY_FILE, "rb");
  if (file) {
    fread (&size.x, sizeof (size.x), 1, file);
    fread (&size.y, sizeof (size.y), 1, file);
    map = new float [size.x * size.y];
    fread (map, sizeof (float), size.x * size.y, file);
    fclose (file);
    loaded = true;
  } else
    entropy_create ("textures/noise256.bmp");

}


float Entropy (int x, int y)
{

  if (!loaded) 
    entropy_load ();
  if (!map || x < 0 || y < 0)
    return 0;
  return map[(x % size.x) + (y % size.y) * size.x];

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