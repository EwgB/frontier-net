/*-----------------------------------------------------------------------------

  World.cpp


-------------------------------------------------------------------------------

  This holds the region grid, which is the main table of information from 
  which ALL OTHER GEOGRAPHICAL DATA is generated or derived.  Note that
  the resulting data is not STORED here. Regions are sets of rules and 
  properties. You crank numbers through them, and it creates the world. 

  This output data is stored and managed elsewhere. (See CPage.cpp)

  This also holds tables of random numbers.  Basically, everything needed to
  re-create the world should be stored here.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ctree.h"
#include "entropy.h"
#include "file.h"
#include "math.h"
#include "random.h"
#include "terraform.h"
#include "world.h"

#define LARGE_STRENGTH    1 //Not used. Considering removing.
#define LARGE_SCALE       9 //Not used. Considering removing.
//The dither map scatters surface data so that grass colorings end up in adjacent regions.
#define DITHER_SIZE       (REGION_SIZE / 2)
//How much space in a region is spent interpolating between itself and its neighbors.
#define BLEND_DISTANCE    (REGION_SIZE / 4)


static GLcoord      dithermap[DITHER_SIZE][DITHER_SIZE];
static unsigned     map_id;
static World        planet;//THE WHOLE THING!
static CTree        tree[TREE_TYPES][TREE_TYPES];
static unsigned     canopy;

/*-----------------------------------------------------------------------------
The following functions are used when generating elevation data
-----------------------------------------------------------------------------*/

//This modifies the passed elevation value AFTER region cross-fading is complete,
//For things that should not be mimicked by neighbors. (Like rivers.)
static float do_height_noblend (float val, Region r, GLvector2 offset, float water)
{

  //return val;
  if (r.flags_shape & REGION_FLAG_RIVER_ANY) {
    GLvector2   cen;
    float       strength;
    float       delta;
    GLvector2   new_off;

    //if this river is strictly north / south
    if (r.flags_shape & REGION_FLAG_RIVERNS && !(r.flags_shape & REGION_FLAG_RIVEREW)) {
      //This makes the river bend side-to-side
      switch ((r.grid_pos.x + r.grid_pos.y) % 6) {
      case 0:
        offset.x += abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 1:
        offset.x -= abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.25f;break;
      case 2:
        offset.x += abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f;break;
      case 3:
        offset.x -= abs (sin (offset.y * 180.0f * DEGREES_TO_RADIANS)) * 0.1f;break;
      case 4:
        offset.x += sin (offset.y * 360.0f * DEGREES_TO_RADIANS) * 0.1f;break;
      case 5:
        offset.x += sin (offset.y * 360.0f * DEGREES_TO_RADIANS) * 0.1f;break;
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
    //if this river curves around a bend
    if (r.flags_shape & REGION_FLAG_RIVERNW && !(r.flags_shape & REGION_FLAG_RIVERSE)) 
      offset.x = offset.y = offset.Length ();
    if (r.flags_shape & REGION_FLAG_RIVERSE && !(r.flags_shape & REGION_FLAG_RIVERNW)) {
      new_off.x = 1.0f - offset.x;
      new_off.y = 1.0f - offset.y;
      new_off.x = new_off.y = new_off.Length ();
      offset = new_off;
    }    
    if (r.flags_shape & REGION_FLAG_RIVERNE && !(r.flags_shape & REGION_FLAG_RIVERSW)) {
      new_off.x = 1.0f - offset.x;
      new_off.y = offset.y;
      new_off.x = new_off.y = new_off.Length ();
      offset = new_off;
    }    
    if (r.flags_shape & REGION_FLAG_RIVERSW && !(r.flags_shape & REGION_FLAG_RIVERNE)) {
      new_off.x = offset.x;
      new_off.y = 1.0f - offset.y;
      new_off.x = new_off.y = new_off.Length ();
      offset = new_off;
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
      delta = (val - water) + 4.0f * r.river_width;
      val -= (delta) * (1.0f - strength);
    }
  }
  return val;

}

//This takes the given properties and generates a single unit of elevation data,
//according to the local region rules.
// Water is the water level.  Detail is the height of the rolling hills. Bias
//is a direct height added on to these.
static float do_height (Region r, GLvector2 offset, float water, float detail, float bias)
{

  float     val;

  //Modify the detail values before they are applied
  if (r.flags_shape & REGION_FLAG_CRATER) {
    if (detail > 0.5f)
      detail = 0.5f;
  }
  if (r.flags_shape & REGION_FLAG_TIERED) {
    if (detail < 0.2f)
      detail += 0.2f;
    else
    if (detail < 0.5f)
      detail -= 0.2f;
  }
  if (r.flags_shape & REGION_FLAG_CRACK) {
    if (detail > 0.2f && detail < 0.3f)
      detail = 0.0f;
  }
  if (r.flags_shape & REGION_FLAG_SINKHOLE) {
    float    x = abs (offset.x - 0.5f);
    float    y = abs (offset.y - 0.5f);
    if (detail > max (x, y))
      detail /= 4.0f;
  }
  
  //Soften up the banks of a river 
  if (r.flags_shape & REGION_FLAG_RIVER_ANY) {
    GLvector2   cen;
    float       strength;

    cen.x = abs ((offset.x - 0.5f) * 2.0f);
    cen.y = abs ((offset.y - 0.5f) * 2.0f);
    strength = min (cen.x, cen.y);
    strength = max (strength, 0.1f);
    detail *= strength;
  }
  



  //Apply the values!
  val = water + detail * r.geo_detail + bias * LARGE_STRENGTH;
  if (r.climate == CLIMATE_SWAMP) {
    val -= r.geo_detail / 2.0f;
    val = max (val, r.geo_water - 0.5f);
  }
  //Modify the final value.
  if (r.flags_shape & REGION_FLAG_MESAS) {
    float    x = abs (offset.x - 0.5f) / 5;
    float    y = abs (offset.y - 0.5f) / 5;
    if ((detail + 0.01f) < (x + y)) {
      val += 5;
    }
  }
  if (r.flags_shape & REGION_FLAG_CANYON_NS) {
    float    x = abs (offset.x - 0.5f) * 2.0f;;
    if (x + detail < 0.5f)
      val -= min (r.geo_detail, 10.0f);
  }
  if ((r.flags_shape & REGION_FLAG_BEACH) && val < r.cliff_threshold && val > 0.0f) {
    val /= r.cliff_threshold;
    val *= val;
    val *= r.cliff_threshold;
    val += 0.2f;
  }
  if ((r.flags_shape & REGION_FLAG_BEACH_CLIFF) && val < r.cliff_threshold && val > -0.1f) {
    val -= min (r.cliff_threshold, 10.0f);
  }
  //if a point dips below the water table, make sure it's not too close to the water,
  //to avoid ugly z-fighting
  //if (val < bias)
    //val = min (val, bias - 2.5f);
  return val;

}


static void build_map_texture ()
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
      r = planet.map[x][yy];
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

-----------------------------------------------------------------------------*/

float WorldWaterLevel (int world_x, int world_y)
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
  rul = WorldRegionGet (origin.x, origin.y);
  rur = WorldRegionGet (origin.x + 1, origin.y);
  rbl = WorldRegionGet (origin.x, origin.y + 1);
  rbr = WorldRegionGet (origin.x + 1, origin.y + 1);
  return MathInterpolateQuad (rul.geo_water, rur.geo_water, rbl.geo_water, rbr.geo_water, offset, ((origin.x + origin.y) %2) == 0);

}

float WorldBiasLevel (int world_x, int world_y)
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
  rul = WorldRegionGet (origin.x, origin.y);
  rur = WorldRegionGet (origin.x + 1, origin.y);
  rbl = WorldRegionGet (origin.x, origin.y + 1);
  rbr = WorldRegionGet (origin.x + 1, origin.y + 1);
  return MathInterpolateQuad (rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset, ((origin.x + origin.y) %2) == 0);

}

Cell WorldCell (int world_x, int world_y)
{

  float     detail;
  float     bias;
  Region    rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.
  float     eul, eur, ebl, ebr;
  float     water;
  GLvector2 offset;
  GLcoord   origin;
  GLcoord   ul, br; //Upper left and bottom-right corners
  GLvector2 blend;
  bool      left;
  Cell      result;

  detail = Entropy (world_x, world_y);
  bias = WorldBiasLevel (world_x, world_y);
  water = WorldWaterLevel (world_x, world_y);
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
  result.detail = detail;
  result.water_level = water;

  ul.x = origin.x;
  ul.y = origin.y;
  br.x = (world_x + BLEND_DISTANCE) / REGION_SIZE;
  br.y = (world_y + BLEND_DISTANCE) / REGION_SIZE;

  if (ul == br) {
    rul = WorldRegionGet (ul.x, ul.y);
    result.elevation = do_height (rul, offset, water, detail, bias);
    result.elevation = do_height_noblend (result.elevation, rul, offset, water);
    return result;
  }
  rul = WorldRegionGet (ul.x, ul.y);
  rur = WorldRegionGet (br.x, ul.y);
  rbl = WorldRegionGet (ul.x, br.y);
  rbr = WorldRegionGet (br.x, br.y);

  eul = do_height (rul, offset, water, detail, bias);
  eur = do_height (rur, offset, water, detail, bias);
  ebl = do_height (rbl, offset, water, detail, bias);
  ebr = do_height (rbr, offset, water, detail, bias);
  result.elevation = MathInterpolateQuad (eul, eur, ebl,ebr, blend, left);
  result.elevation = do_height_noblend (result.elevation, rul, offset, water);
  return result;

}

unsigned WorldTreeType (float moisture, float temperature)
{

  int   m, t;

  m = (int)(moisture * TREE_TYPES);
  t = (int)(temperature * TREE_TYPES);
  m = clamp (m, 0, TREE_TYPES - 1);
  t = clamp (t, 0, TREE_TYPES - 1);
  return m + t * TREE_TYPES;

}

CTree* WorldTree (unsigned id)
{

  unsigned    m, t;

  m = id % TREE_TYPES;
  t = (id - m) / TREE_TYPES;
  return &tree[m][t];

}

char* WorldLocationName (int world_x, int world_y)
{

  static char   result[20];
  char          lat[20];
  char          lng[20];

  world_x /= REGION_SIZE;
  world_y /= REGION_SIZE;
  world_x -= WORLD_GRID_CENTER;
  world_y -= WORLD_GRID_CENTER;
  if (!world_x && !world_y)
    return "Equatorial meridian";
  if (world_x == 0)
    strcpy (lng, "meridian");
  else if (world_x < 0)
    sprintf (lng, "%d west", abs (world_x));
  else
    sprintf (lng, "%d east", world_x);
  if (world_y == 0)
    strcpy (lat, "Equator");
  else if (world_y < 0)
    sprintf (lat, "%d north", abs (world_y));
  else
    sprintf (lat, "%d south", world_y);
  sprintf (result, "%s, %s", lat, lng);
  return result;

}

void    WorldInit ()
{

  int         x, y;

  //Fill in the dither table - a table of random offsets
  for (y = 0; y < DITHER_SIZE; y++) {
    for (x = 0; x < DITHER_SIZE; x++) {
      dithermap[x][y].x = RandomVal () % DITHER_SIZE + RandomVal () % DITHER_SIZE;
      dithermap[x][y].y = RandomVal () % DITHER_SIZE + RandomVal () % DITHER_SIZE;
    }
  }

}

float WorldNoisef (int index)
{

  index = abs (index % NOISE_BUFFER);
  return planet.noisef[index];

}

unsigned WorldNoisei (int index)
{

  index = abs (index % NOISE_BUFFER);
  return planet.noisei[index];

}

void    WorldGenerate (unsigned seed_in)
{

  int         x;
  unsigned    m, t;
  bool        is_canopy;
  int         rotator;

  RandomInit (seed_in);
  planet.seed = seed_in;
  FileMakeDirectory (WorldDirectory ());
  for (x = 0; x < NOISE_BUFFER; x++) {
    planet.noisei[x] = RandomVal ();
    planet.noisef[x] = RandomFloat ();
  }
  rotator = 0;
  for (m = 0; m < TREE_TYPES; m++) {
    for (t = 0; t < TREE_TYPES; t++) {
      if ((m == TREE_TYPES / 2) && (t == TREE_TYPES / 2)) {
        is_canopy = true;
        canopy = m + t * TREE_TYPES;
      } else
        is_canopy = false;
      tree[m][t].Create (is_canopy, (float)m / TREE_TYPES, (float)t / TREE_TYPES, rotator++);
    }
  }
  planet.wind_from_west = (RandomVal () % 2) ? true : false;
  planet.northern_hemisphere = (RandomVal () % 2) ? true : false;
  planet.river_count = 5 + RandomVal () % 4;
  planet.lake_count = 5 + RandomVal () % 4;
  TerraformPrepare ();
  TerraformOceans ();
  TerraformCoast ();
  TerraformClimate ();
  TerraformRivers (planet.river_count);
  TerraformLakes (planet.lake_count);
  TerraformClimate ();//Do climate a second time now that rivers are in
  TerraformZones ();
  TerraformClimate ();//Now again, since we have added climate-modifying features (Mountains, etc.)
  TerraformFill ();
  TerraformAverage ();
  TerraformFlora ();
  TerraformColors ();
  build_map_texture ();
  
}

Region WorldRegionGet (int index_x, int index_y)
{

  return planet.map[index_x][index_y];

}

void WorldRegionSet (int index_x, int index_y, Region val)
{

  planet.map[index_x][index_y] = val;

}

Region WorldRegionFromPosition (int world_x, int world_y)
{
  
  world_x = max (world_x, 0);
  world_y = max (world_y, 0);
  world_x += dithermap[world_x % DITHER_SIZE][world_y % DITHER_SIZE].x;
  world_y += dithermap[world_x % DITHER_SIZE][world_y % DITHER_SIZE].y;
  world_x /= REGION_SIZE;
  world_y /= REGION_SIZE;
  if (world_x >= WORLD_GRID || world_y >= WORLD_GRID)
    return planet.map[0][0];
  return planet.map[world_x][world_y];

}

Region WorldRegionFromPosition (float world_x, float world_y)
{
  
  return WorldRegionFromPosition ((int)world_x, (int)world_y);

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
  r0 = WorldRegionGet (origin.x, origin.y);
  r1 = WorldRegionGet (origin.x + 1, origin.y);
  r2 = WorldRegionGet (origin.x, origin.y + 1);
  r3 = WorldRegionGet (origin.x + 1, origin.y + 1);
  switch (c) {
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
  case SURFACE_COLOR_SAND:
    return glRgba (0.98f, 0.82f, 0.42f);
  default:
  case SURFACE_COLOR_GRASS:
    c0 = r0.color_grass;
    c1 = r1.color_grass;
    c2 = r2.color_grass;
    c3 = r3.color_grass;
    break;
  }
  result.red   = MathInterpolateQuad (c0.red, c1.red, c2.red, c3.red, offset);
  result.green = MathInterpolateQuad (c0.green, c1.green, c2.green, c3.green, offset);
  result.blue  = MathInterpolateQuad (c0.blue, c1.blue, c2.blue, c3.blue, offset);
  return result;
  
}

unsigned WorldCanopyTree ()
{

  return canopy;

}

unsigned WorldMap ()
{

  return map_id;

}

World* WorldPtr ()
{

  return &planet;

}

void          WorldTexturePurge ()
{

  unsigned    m, t;

  for (m = 0; m < TREE_TYPES; m++) {
    for (t = 0; t < TREE_TYPES; t++) {
      tree[m][t].TexturePurge ();
    }
  }
  build_map_texture ();

}

char* WorldDirectionFromAngle (float angle)
{

  char*   direction;

  direction = "North";
  if (angle < 22.5f)
    direction = "North";
  else if (angle < 67.5f)
    direction = "Northwest";
  else if (angle < 112.5f)
    direction = "West";
  else if (angle < 157.5f)
    direction = "Southwest";
  else if (angle < 202.5f)
    direction = "South";
  else if (angle < 247.5f)
    direction = "Southeast";
  else if (angle < 292.5f)
    direction = "East";
  else if (angle < 337.5f)
    direction = "Northeast";
  return direction;

}

char* WorldDirectory ()
{

  static char     dir[32];

  sprintf (dir, "saves//seed%d//", planet.seed);
  return dir;

}