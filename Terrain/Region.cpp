/*-----------------------------------------------------------------------------

  Region.cpp


-------------------------------------------------------------------------------

  This holds the region grid, which is the main table of information from 
  which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
  the resulting data is not STORED here. Regions are sets of rules and 
  properties. You crank numbers through them, and it creates the world. 

  This output data is stored and managed elsewhere. (See CPage.cpp)
 
-----------------------------------------------------------------------------*/


#include "stdafx.h"
#if 0
#include "entropy.h"
#include "math.h"
#include "region.h"
#include "random.h"
#include "terraform.h"

#define LARGE_SCALE       9
#define SMALL_STRENGTH    1
#define LARGE_STRENGTH    (REGION_SIZE * 1)
#define BLEND_DISTANCE    (REGION_SIZE / 4)
#define DITHER_SIZE       (REGION_SIZE / 2)
#define OCEAN_BUFFER      20 //The number of regions around the edge which must be ocean
#define FLOWER_PALETTE    (sizeof (flower_palette) / sizeof (GLrgba))
#define FREQUENCY         3 //Higher numbers make the overall map repeat more often

#define NNORTH             "Northern"
#define NSOUTH             "Southern"
#define NEAST              "Eastern"
#define NWEST              "Western"

static GLcoord      direction[] = {
  0, -1, // North
  0, 1,  // South
  1, 0,  // East
 -1, 0   // West
};

static GLrgba       flower_palette[] = {
  {1.0f, 1.0f, 1.0f, 1.0f}, {1.0f, 1.0f, 1.0f, 1.0f}, {1.0f, 1.0f, 1.0f, 1.0f}, //white
  {1.0f, 0.3f, 0.3f, 1.0f}, {1.0f, 0.3f, 0.3f, 1.0f}, //red
  {1.0f, 1.0f, 0.0f, 1.0f}, {1.0f, 1.0f, 0.0f, 1.0f}, //Yellow
  {0.7f, 0.3f, 1.0f, 1.0f}, // Violet
  {1.0f, 0.5f, 1.0f, 1.0f}, // Pink #1
  {1.0f, 0.5f, 0.8f, 1.0f}, // Pink #2
  {1.0f, 0.0f, 0.5f, 1.0f}, //Maroon
};

static Region       continent[WORLD_GRID][WORLD_GRID];
static GLcoord      dithermap[DITHER_SIZE][DITHER_SIZE];
static unsigned     map_id;

/*-----------------------------------------------------------------------------
The following functions are used when generating elevation data
-----------------------------------------------------------------------------*/

//This modifies the passed elevation value AFTER region cross-fading is complete,
//For things that should not be mimicked by neighbors. (Like rivers.)
static float do_height_noblend (float val, Region r, GLvector2 offset, float bias)
{

  //return val;
  if (r.flags_shape & REGION_FLAG_RIVER_ANY) {
    GLvector2   cen;
    float       strength;
    float       delta;

    //if this river is strictly north / south
    if (r.flags_shape & REGION_FLAG_RIVERNS && !(r.flags_shape & REGION_FLAG_RIVEREW)) {
      //This makes the river bend side-to-side
      switch ((r.grid_pos.x + r.grid_pos.y) % 4) {
      case 0:
        offset.x += abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 1:
        offset.x -= abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 2:
        offset.x += abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f;break;
      case 3:
        offset.x -= abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f;break;
      }
    }
    //if this river is strictly east / west
    if (r.flags_shape & REGION_FLAG_RIVEREW && !(r.flags_shape & REGION_FLAG_RIVERNS)) {
      //This makes the river bend side-to-side
      switch ((r.grid_pos.x + r.grid_pos.y) % 4) {
      case 0:
        offset.y -= abs (sin (offset.x * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 1:
        offset.y += abs (sin (offset.x * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 2:
        offset.y -= abs (sin (offset.x * 180.0f * DEGREES_TO_RADIANS)) * 0.10f;break;
      case 3:
        offset.y += abs (sin (offset.x * 180.0f * DEGREES_TO_RADIANS)) * 0.10f;break;
      }
    }
    
    cen.x = abs ((offset.x - 0.5f) * 2.0f);
    cen.y = abs ((offset.y - 0.5f) * 2.0f);
    strength = glVectorLength (cen);
    if (r.flags_shape & REGION_FLAG_RIVERN && offset.y < 0.5f)
      strength = min (strength, cen.x);
    if (r.flags_shape & REGION_FLAG_RIVERS && offset.y >= 0.5f)
      strength = min (strength, cen.x);
    if (r.flags_shape & REGION_FLAG_RIVERW && offset.x < 0.5f) 
      strength = min (strength, cen.y);
    if (r.flags_shape & REGION_FLAG_RIVERE && offset.x >= 0.5f) 
      strength = min (strength, cen.y);
    if (strength < (r.river_width / 2)) {
      strength *= 1.0f / (r.river_width / 2);
      delta = (val - bias) + 4.0f * r.river_width;
      val -= (delta) * (1.0f - strength);
    }
  }
  return val;

}

//This takes the given properties and generates a single unit of elevation data,
//according to the local region rules.
static float do_height (Region r, GLvector2 offset, float bias, float esmall, float elarge)
{

  float     val;

  //Modify the detail values before they are applied
  if (r.flags_shape & REGION_FLAG_CRATER) {
    if (esmall > 0.5f)
      esmall = 0.5f;
  }
  if (r.flags_shape & REGION_FLAG_TIERED) {
    if (esmall < 0.2f)
      esmall += 0.2f;
    else
    if (esmall < 0.5f)
      esmall -= 0.2f;
  }
  if (r.flags_shape & REGION_FLAG_CRACK) {
    if (esmall > 0.2f && esmall < 0.3f)
      esmall = 0.0f;
  }
  if (r.flags_shape & REGION_FLAG_SINKHOLE) {
    float    x = abs (offset.x - 0.5f);
    float    y = abs (offset.y - 0.5f);
    if (esmall > max (x, y))
      esmall /= 4.0f;
  }
  //Soften up the banks of a river 
  if (r.flags_shape & REGION_FLAG_RIVER_ANY) {
    GLvector2   cen;
    float       strength;

    cen.x = abs ((offset.x - 0.5f) * 2.0f);
    cen.y = abs ((offset.y - 0.5f) * 2.0f);
    strength = min (cen.x, cen.y);
    strength = max (strength, 0.2f);
    esmall *= strength;
  }
  
  elarge *= r.geo_large;
  //Apply the values!
  val = esmall * r.geo_detail + elarge * LARGE_STRENGTH;
  val += bias;
  if (r.climate == CLIMATE_SWAMP) {
    val -= r.geo_detail / 2.0f;
    val = max (val, r.geo_water - 0.5f);
  }
  //Modify the final value.
  if (r.flags_shape & REGION_FLAG_MESAS) {
    float    x = abs (offset.x - 0.5f) / 5;
    float    y = abs (offset.y - 0.5f) / 5;
    if ((esmall + 0.01f) < (x + y)) {
      val += 5;
    }
  }
  if (r.flags_shape & REGION_FLAG_CANYON_NS) {
    float    x = abs (offset.x - 0.5f) * 2.0f;;

    if (x + esmall < 0.5f)
      val = bias + (val - bias) / 2.0f;
    else 
      val += r.geo_water;
  }
  if ((r.flags_shape & REGION_FLAG_BEACH) && val < r.beach_threshold && val > 0.0f) {
    val /= r.beach_threshold;
    val = 1 - val;
    val *= val * val;
    val = 1 - val;
    val *= r.beach_threshold;
    val -= 0.2f;
  }
  if ((r.flags_shape & REGION_FLAG_BEACH_CLIFF) && val < r.beach_threshold && val > -0.1f) {
    val -= r.beach_threshold;
  }
  return val;

}

static Region region (int x, int y)
{
  if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
    return continent[0][0];
  return continent[x][y];

}

/*-----------------------------------------------------------------------------
The following functions are used when building a new world.
-----------------------------------------------------------------------------*/

static void do_map ()
{

  int       x, y, yy;
  Region    r;

  if (!map_id) 
    glGenTextures (1, &map_id); 
  glBindTexture(GL_TEXTURE_2D, map_id);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  unsigned char* buffer; 
  unsigned char*  ptr;

  buffer = new unsigned char[WORLD_GRID * WORLD_GRID * 3];

  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      //Flip it vertically, because the OpenGL texture coord system is retarded.
      yy = (WORLD_GRID - 1) - y;
      r = continent[x][yy];
      ptr = &buffer[(x + y * WORLD_GRID) * 3];
      ptr[0] = (unsigned char)(r.color_map.red * 255.0f);
      ptr[1] = (unsigned char)(r.color_map.green * 255.0f);
      ptr[2] = (unsigned char)(r.color_map.blue * 255.0f);
    }
  }
  glTexImage2D (GL_TEXTURE_2D, 0, GL_RGB, WORLD_GRID, WORLD_GRID, 0, GL_RGB, GL_UNSIGNED_BYTE, &buffer[0]);
  delete buffer;

}


/*-----------------------------------------------------------------------------
Module functions
-----------------------------------------------------------------------------*/

unsigned RegionMap ()
{

  return map_id;

}

Region RegionGet (float x, float y)
{
  
  x /= REGION_SIZE;
  y /= REGION_SIZE;
  if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
    return continent[0][0];
  return continent[(int)x][(int)y];

}

Region RegionGet (int x, int y)
{
  
  x = max (x, 0);
  y = max (y, 0);
  x += dithermap[x % DITHER_SIZE][y% DITHER_SIZE].x;
  y += dithermap[x % DITHER_SIZE][y% DITHER_SIZE].y;
  x /= REGION_SIZE;
  y /= REGION_SIZE;
  if (x < 0 || y < 0 || x >= WORLD_GRID || y >= WORLD_GRID)
    return continent[0][0];
  return continent[x][y];

}

Region WorldRegionGet (int x, int y)
{

  return continent[x][y];

}

void WorldRegionSet (int x, int y, Region val)
{

  continent[x][y] = val;

}


void    RegionInit ()
{

  int         x, y;
  Region      r;

  //Fill in the dither table - a table of random offsets
  for (x = 0; x < DITHER_SIZE; x++) {
    for (y = 0; y < DITHER_SIZE; y++) {
      dithermap[x][y].x = RandomVal () % DITHER_SIZE + RandomVal () % DITHER_SIZE;
      dithermap[x][y].y = RandomVal () % DITHER_SIZE + RandomVal () % DITHER_SIZE;
    }
  }
  //Set some defaults
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      memset (&r, 0, sizeof (Region));
      continent[x][y] = r;
    }
  }

}
  
void    RegionGenerate ()
{

  int         x, y;
  Region      r;
  GLcoord     from_center;
  GLcoord     offset;

  int z = sizeof (Region);
  //Set some defaults
  offset.x = RandomVal () % 1024;
  offset.y = RandomVal () % 1024;
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      memset (&r, 0, sizeof (Region));
      sprintf (r.title, "NOTHING");
      r.geo_large = r.geo_detail = 0;
      r.mountain_height = 0;
      r.grid_pos.x = x;
      r.grid_pos.y = y;
      from_center.x = abs (x - WORLD_GRID_CENTER);
      from_center.y = abs (y - WORLD_GRID_CENTER);
      //Geo scale is a number from -1 to 1. -1 is lowest ovean. 0 is sea level. 
      //+1 is highest elevation on the island. This is used to guide other derived numbers.
      r.geo_scale = glVectorLength (glVector ((float)from_center.x, (float)from_center.y));
      r.geo_scale /= (WORLD_GRID_CENTER - OCEAN_BUFFER);
      //Create a steep drop around the edge of the world
      if (r.geo_scale > 1.25f)
        r.geo_scale = 1.25f + (r.geo_scale - 1.25f) * 2.0f;
      r.geo_scale = 1.0f - r.geo_scale;
      r.geo_scale += (Entropy ((x + offset.x) * FREQUENCY, (y + offset.y) * FREQUENCY) - 0.2f) / 1;
      r.geo_scale = clamp (r.geo_scale, -1.0f, 1.0f);
      if (r.geo_scale > 0.0f)
        r.geo_water = 1.0f + r.geo_scale;
      r.geo_large = 0.3f;
      r.geo_large = 0.0f;
      r.geo_detail = 0.0f;
      r.color_map = glRgba (0.0f);
      r.climate = CLIMATE_INVALID;
      continent[x][y] = r;
    }
  }
  TerraformOceans ();
  TerraformCoast ();
  TerraformClimate ();
  TerraformRivers (4);
  TerraformClimate ();//Do climate a second time now that rivers are in
  TerraformZones ();
  TerraformFill ();
  TerraformAverage ();
  TerraformColors ();
  do_map ();
  
}

GLrgba WorldColorGet (int world_x, int world_y, SurfaceColor c)
{

  GLcoord   origin;
  int       x, y;
  GLvector2 offset;
  GLrgba    c0, c1, c2, c3, result;
  Region    r0, r1, r2, r3;

  x = max (world_x % DITHER_SIZE, 0);
  y = max (world_y % DITHER_SIZE, 0);
  world_x += dithermap[x][y].x;
  world_y += dithermap[x][y].y;
  offset.x = (float)(world_x % REGION_SIZE) / REGION_SIZE;
  offset.y = (float)(world_y % REGION_SIZE) / REGION_SIZE;
  origin.x = world_x / REGION_SIZE;
  origin.y = world_y / REGION_SIZE;
  r0 = region (origin.x, origin.y);
  r1 = region (origin.x + 1, origin.y);
  r2 = region (origin.x, origin.y + 1);
  r3 = region (origin.x + 1, origin.y + 1);
  //return r0.color_grass;
  switch (c) {
  case SURFACE_COLOR_GRASS:
    c0 = r0.color_grass;
    c1 = r1.color_grass;
    c2 = r2.color_grass;
    c3 = r3.color_grass;
    break;
  case SURFACE_COLOR_DIRT:
    c0 = r0.color_dirt;
    c1 = r1.color_dirt;
    c2 = r2.color_dirt;
    c3 = r3.color_dirt;
    break;
  case SURFACE_COLOR_ROCK:
    c0 = r0.color_rock;
    c1 = r1.color_rock;
    c2 = r2.color_rock;
    c3 = r3.color_rock;
    break;
  }
  result.red   = MathInterpolateQuad (c0.red, c1.red, c2.red, c3.red, offset);
  result.green = MathInterpolateQuad (c0.green, c1.green, c2.green, c3.green, offset);
  result.blue  = MathInterpolateQuad (c0.blue, c1.blue, c2.blue, c3.blue, offset);
  return result;
  
}


GLrgba RegionAtmosphere (int world_x, int world_y)
{

  GLcoord   origin;
  GLvector2 offset;

  offset.x = (float)(world_x % REGION_SIZE) / REGION_SIZE;
  offset.y = (float)(world_y % REGION_SIZE) / REGION_SIZE;
  origin.x = world_x / REGION_SIZE;
  origin.y = world_y / REGION_SIZE;
  return region (origin.x, origin.y).color_atmosphere;

}

float RegionWaterLevel (int world_x, int world_y)
{

  GLcoord   origin;
  GLvector2 offset;
  Region    rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.

  world_x += REGION_HALF;
  world_y += REGION_HALF;
  origin.x = world_x / REGION_SIZE;
  origin.y = world_y / REGION_SIZE;
  origin.x = clamp (origin.x, 0, WORLD_GRID - 1);
  origin.y = clamp (origin.y, 0, WORLD_GRID - 1);
  offset.x = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
  offset.y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
  rul = region (origin.x, origin.y);
  rur = region (origin.x + 1, origin.y);
  rbl = region (origin.x, origin.y + 1);
  rbr = region (origin.x + 1, origin.y + 1);
  return MathInterpolateQuad (rul.geo_water, rur.geo_water, rbl.geo_water, rbr.geo_water, offset, ((origin.x + origin.y) %2) == 0);

}

Cell WorldCell (int world_x, int world_y)
{

  float     esmall, elarge;
  Region    rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.
  float     eul, eur, ebl, ebr;
  float     bias;
  GLvector2 offset;
  GLcoord   origin;
  GLcoord   ul, br; //Upper left and bottom-right corners
  GLvector2 blend;
  bool      left;
  Cell      result;

  esmall = Entropy (world_x, world_y);
  elarge = Entropy ((float)world_x / LARGE_SCALE, (float)world_y / LARGE_SCALE);
  bias = RegionWaterLevel (world_x, world_y);
  origin.x = world_x / REGION_SIZE;
  origin.y = world_y / REGION_SIZE;
  origin.x = clamp (origin.x, 0, WORLD_GRID - 1);
  origin.y = clamp (origin.y, 0, WORLD_GRID - 1);
  //Get our offset from the region origin as a pair of scalars.
  blend.x = (float)(world_x % BLEND_DISTANCE) / BLEND_DISTANCE;
  blend.y = (float)(world_y % BLEND_DISTANCE) / BLEND_DISTANCE;
  left = ((origin.x + origin.y) %2) == 0;
  offset.x = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
  offset.y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
  result.detail = esmall;
  result.water_level = bias;

  ul.x = origin.x;
  ul.y = origin.y;
  br.x = (world_x + BLEND_DISTANCE) / REGION_SIZE;
  br.y = (world_y + BLEND_DISTANCE) / REGION_SIZE;

  if (ul == br) {
    rul = region (ul.x, ul.y);
     result.elevation = do_height (rul, offset, bias, esmall, elarge);
     result.elevation = do_height_noblend (result.elevation, rul, offset, bias);
     return result;
  }
  rul = region (ul.x, ul.y);
  rur = region (br.x, ul.y);
  rbl = region (ul.x, br.y);
  rbr = region (br.x, br.y);

  eul = do_height (rul, offset, bias, esmall, elarge);
  eur = do_height (rur, offset, bias, esmall, elarge);
  ebl = do_height (rbl, offset, bias, esmall, elarge);
  ebr = do_height (rbr, offset, bias, esmall, elarge);
  result.elevation = MathInterpolateQuad (eul, eur, ebl,ebr, blend, left);
  result.elevation = do_height_noblend (result.elevation, rul, offset, bias);
  return result;

}

#endif