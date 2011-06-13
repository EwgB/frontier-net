/*-----------------------------------------------------------------------------

  Water.cpp


-------------------------------------------------------------------------------

 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "random.h"
#include "region.h"
#include "texture.h"
#include "vbo.h"

#define WATER_TILE      7

static VBO        water;
static VBO        map;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void WaterBuild ()
{

  vector<GLvector>  vert;
  vector<GLvector>  normal;
  vector<GLvector2> uv;
  vector<GLvector2> uv_map;
  vector<unsigned>  index;
  int               x, y;
  float             elev;

  for (y = 0; y < REGION_GRID_EDGE; y++) {
    for (x = 0; x < REGION_GRID_EDGE; x++) {
      elev = RegionWaterLevel (x * REGION_SIZE, y * REGION_SIZE);
      elev = max (elev, 0.0f);
      uv_map.push_back (glVector ((float)x / REGION_GRID , (float)(REGION_GRID - y) / REGION_GRID));
      uv.push_back (glVector ((float)x * WATER_TILE , (float)y * WATER_TILE));
      normal.push_back (glVector (0.0f, 0.0f, 1.0f));
      vert.push_back (glVector ((float)x * REGION_SIZE, (float)y * REGION_SIZE, elev));
    }
  }
  for (y = 0; y < REGION_GRID; y++) {
    for (x = 0; x < REGION_GRID; x++) {
      index.push_back ((x + 0) + (y + 0) * REGION_GRID_EDGE);
      index.push_back ((x + 1) + (y + 0) * REGION_GRID_EDGE);
      index.push_back ((x + 1) + (y + 1) * REGION_GRID_EDGE);
      index.push_back ((x + 0) + (y + 1) * REGION_GRID_EDGE);
    }
  }

  water.Create (GL_QUADS, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);
  map.Create (GL_QUADS, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv_map[0]);

}


void WaterRender ()
{

  glDisable (GL_BLEND);
  glDepthMask (false);
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("water4.bmp"));
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  water.Render ();  
  glDepthMask (true);
  glBindTexture (GL_TEXTURE_2D, RegionMap ());
  //glDisable (GL_FOG);

  if (0) {
    glDisable (GL_BLEND);
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  } else {
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
    glEnable (GL_BLEND);
    glBlendFunc (GL_ONE, GL_ONE);
  }
  map.Render ();

}
