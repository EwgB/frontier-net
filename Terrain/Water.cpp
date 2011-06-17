/*-----------------------------------------------------------------------------

  Water.cpp


-------------------------------------------------------------------------------

 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "random.h"
#include "texture.h"
#include "vbo.h"
#include "world.h"

#define WATER_TILE      7

static VBO        water;
static VBO        map;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void WaterBuild ()
{
  /*
  vector<GLvector>  vert;
  vector<GLvector>  normal;
  vector<GLvector2> uv;
  vector<GLvector2> uv_map;
  vector<unsigned>  index;
  float             x, y;
  int               xx, yy;
  float             elev;
  int               width, height;

  width = height =0;
  for (y = 0; y < WORLD_GRID_EDGE; y += 0.25f) {
    width = 0;
    for (x = 0; x < WORLD_GRID_EDGE; x += 0.25f) {
      elev = RegionWaterLevel ((int)(x * REGION_SIZE), (int)(y * REGION_SIZE));
      elev = max (elev, 0.0f);
      uv_map.push_back (glVector ((float)x / WORLD_GRID , (float)(WORLD_GRID - y) / WORLD_GRID));
      uv.push_back (glVector ((float)x * WATER_TILE , (float)y * WATER_TILE));
      normal.push_back (glVector (0.0f, 0.0f, 1.0f));
      vert.push_back (glVector ((float)x * REGION_SIZE, (float)y * REGION_SIZE, elev));
      width++;
    }
    height++;
  }
  for (yy = 0; yy < height - 1; yy++) {
    for (xx = 0; xx < width - 1; xx++) {
      index.push_back ((xx + 0) + (yy + 0) * width);
      index.push_back ((xx + 1) + (yy + 0) * width);
      index.push_back ((xx + 1) + (yy + 1) * width);
      index.push_back ((xx + 0) + (yy + 1) * width);
    }
  }

  water.Create (GL_QUADS, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);
  map.Create (GL_QUADS, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv_map[0]);

  */
  vector<GLvector>  vert;
  vector<GLvector>  normal;
  vector<GLvector2> uv;
  vector<GLvector2> uv_map;
  vector<unsigned>  index;
  int               x, y;
  float             elev;

  for (y = 0; y < WORLD_GRID_EDGE; y++) {
    for (x = 0; x < WORLD_GRID_EDGE; x++) {
      elev = WorldWaterLevel (x * REGION_SIZE, y * REGION_SIZE);
      elev = max (elev, 0.0f);
      uv_map.push_back (glVector ((float)x / WORLD_GRID, (float)(WORLD_GRID - y) / WORLD_GRID));
      uv.push_back (glVector ((float)x * WATER_TILE , (float)y * WATER_TILE));
      normal.push_back (glVector (0.0f, 0.0f, 1.0f));
      vert.push_back (glVector ((float)x * REGION_SIZE, (float)y * REGION_SIZE, elev));
    }
  }
  for (y = 0; y < WORLD_GRID; y++) {
    for (x = 0; x < WORLD_GRID; x++) {
      index.push_back ((x + 0) + (y + 0) * WORLD_GRID_EDGE);
      index.push_back ((x + 1) + (y + 0) * WORLD_GRID_EDGE);
      index.push_back ((x + 1) + (y + 1) * WORLD_GRID_EDGE);
      index.push_back ((x + 0) + (y + 1) * WORLD_GRID_EDGE);
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
  glBindTexture (GL_TEXTURE_2D, WorldMap ());
  //glDisable (GL_FOG);

  if (0) {
    glDisable (GL_BLEND);
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  } else {
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
    glEnable (GL_BLEND);
    glBlendFunc (GL_ONE, GL_ONE);
  }
  //map.Render ();

}
