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

#include "entropy.h"
#include "math.h"
#include "region.h"
#include "random.h"

#define LARGE_SCALE       9
//#define SMALL_STRENGTH    (REGION_SIZE / 6)
#define SMALL_STRENGTH    1
#define LARGE_STRENGTH    (REGION_SIZE * 1)
#define BLEND_DISTANCE    (REGION_SIZE / 4)
#define DITHER_SIZE       (REGION_SIZE / 2)
#define OCEAN_BUFFER      15 //The number of regions around the edge which must be ocean
#define FLOWER_PALETTE    (sizeof (flower_palette) / sizeof (GLrgba))
#define FREQUENCY         3 //Higher numbers make the overall map repeatmore often

#define NNORTH             "Northern"
#define NSOUTH             "Southern"
#define NEAST              "Eastern"
#define NWEST              "Western"

enum
{
  NORTH,
  SOUTH,
  EAST,
  WEST
};

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

static Region       continent[REGION_GRID][REGION_GRID];
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
  
  esmall *= r.geo_detail;
  elarge *= r.geo_large;
  //Apply the values!
  val = esmall * SMALL_STRENGTH + elarge * LARGE_STRENGTH;
  val += bias;
  if (r.climate == CLIMATE_SWAMP) 
    val -= r.geo_detail / 2.0f;
  //Modify the final value.
  if (r.flags_shape & REGION_FLAG_MESAS) {
    float    x = abs (offset.x - 0.5f) / 5;
    float    y = abs (offset.y - 0.5f) / 5;
    if ((esmall + 0.01f) < (x + y)) {
      val += 5;
    }
  }
  if (r.flags_shape & REGION_FLAG_CANYON_NS) {
    esmall = min (esmall, 0.35f);
    if (offset.x > (0.15f + esmall) && offset.x < (0.85f - esmall))
      val -= r.threshold;
    else 
      val += r.threshold;
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
  if (x < 0 || y < 0 || x >= REGION_GRID || y >= REGION_GRID)
    return continent[0][0];
  return continent[x][y];

}

/*-----------------------------------------------------------------------------
The following functions are used when building a new world.
-----------------------------------------------------------------------------*/

//check the regions around the given one, see if they are unused
static bool is_free (int x, int y, int radius)
{

  int       xx, yy;
  Region    r;

  for (xx = -radius; xx <= radius; xx++) {
    for (yy = -radius; yy <= radius; yy++) {
      r = region (x + xx, y + yy);
      if (r.climate != CLIMATE_INVALID)
        return false;
    }
  }
  return true;

}

//In general, what part of the map is this coordinate in?
static char* find_direction_name (int x, int y)
{

  GLcoord   from_center;

  from_center.x = abs (x - REGION_CENTER);
  from_center.y = abs (y - REGION_CENTER);
  if (from_center.x < from_center.y) {
    if (y < REGION_CENTER)
      return NNORTH;
    else
      return NSOUTH;
  } 
  if (x < REGION_CENTER)
    return NWEST;
  return NEAST;

}


//In general, what part of the map is this coordinate in?
static GLcoord find_direction (int x, int y)
{

  GLcoord   from_center;

  from_center.x = abs (x - REGION_CENTER);
  from_center.y = abs (y - REGION_CENTER);
  if (from_center.x < from_center.y) {
    if (y < REGION_CENTER)
      return direction[NORTH];
    else
      return direction[SOUTH];
  } 
  if (x < REGION_CENTER)
    return direction[WEST];
  return direction[EAST];

}

static bool try_river (int start_x, int start_y, int id)
{

  Region            r;
  Region*           neighbor;
  vector<GLcoord>   path;
  GLcoord           selected;
  GLcoord           last_move;
  GLcoord           to_coast;
  int               x, y;
  int               xx, yy;
  unsigned          d;
  float             lowest;
  float             water_level;
  float             water_strength;

  x = start_x;
  y = start_y;
  while (1) {
    r = continent[x][y];
    //If we run into the ocean, then we're done.
    if (r.climate == CLIMATE_OCEAN) 
      break;
    if (r.climate == CLIMATE_MOUNTAIN) 
      return false;
    //If we run into a river, we've become a tributary.
    if (r.climate == CLIMATE_RIVER) {
      //don't become a tributary at the start of a river. Looks odd.
      if (r.river_segment < 7)
        return false;
      break;
    }
    lowest = r.geo_bias;
    to_coast = find_direction (x, y);
    //lowest = 999.9f;
    selected.Clear ();
    for (d = 0; d < 4; d++) {
      neighbor = &continent[x + direction[d].x][y + direction[d].y];
      //Don't reverse course into ourselves
      if (last_move == (direction[d] * -1))
        continue;
      //Don't head directly AWAY from the coast
      if (direction[d] == to_coast * -1)
        continue;
      if (neighbor->geo_bias <= lowest) {
        selected = direction[d];
        lowest = neighbor->geo_bias;
      }
    }
    //If everthing around us is above us, we can't flow downhill
    if (!selected.x && !selected.y) //Let's just head for the edge of the map
      selected = to_coast;
    last_move = selected;
    x += selected.x;
    y += selected.y;
    path.push_back (selected);
  }
  //If the river is too short, ditch it.
  if (path.size () < (REGION_GRID / 4))
    return false;
  //The river is good. Place it.
  x = start_x;
  y = start_y;
  water_strength = 0.03f;
  water_level = continent[x][y].geo_bias;
  for (d = 0; d < path.size (); d++) {
    r = continent[x][y];
    if (!d)
      sprintf (r.title, "River%d-Source", id);
    else if (d == path.size () - 1) 
      sprintf (r.title, "River%d-Mouth", id);
    else
      sprintf (r.title, "River%d-%d", id, d);
    //A river should attain full strength after crossing 1/4 of the map
    water_strength += (1.0f / ((float)REGION_GRID / 4.0f));
    water_strength = min (water_strength, 1);
    r.flags_shape |= REGION_FLAG_NOBLEND;
    r.river_id = id;
    r.moisture = max (r.moisture, 0.5f);
    r.river_segment = d;
    //r.geo_detail = 8.0f + water_strength * 10.0f;
    r.geo_detail = 28.0f - water_strength * 10.0f;
    r.river_width = min (water_strength, 1);
    r.climate = CLIMATE_RIVER;
    water_level = min (r.geo_bias, water_level);
    //We need to flatten out this space, as well as all of its neighbors.
    r.geo_bias = water_level;
    for (xx = x - 2; xx <= x + 2; xx++) {
      for (yy = y - 2; yy <= y + 2; yy++) {
        if (continent[xx][yy].climate != CLIMATE_INVALID) 
          continue;
        if (!xx && !yy)
          continue;
        continent[xx][yy].geo_bias = min (continent[xx][yy].geo_bias, water_level);
        continent[xx][yy].geo_large = r.geo_large;
        continent[xx][yy].geo_detail = r.geo_detail;
        continent[xx][yy].climate = CLIMATE_RIVER_BANK;
        continent[xx][yy].flags_shape |= REGION_FLAG_NOBLEND;
        sprintf (continent[xx][yy].title, "River%d-Banks", id);
      }
    }
    selected = path[d];
    neighbor = &continent[x + selected.x][y + selected.y];
    if (selected.y == -1) {//we're moving north
      neighbor->flags_shape |= REGION_FLAG_RIVERS;
      r.flags_shape |= REGION_FLAG_RIVERN;
    }
    if (selected.y == 1) {//we're moving south
      neighbor->flags_shape |= REGION_FLAG_RIVERN;
      r.flags_shape |= REGION_FLAG_RIVERS;
    }
    if (selected.x == -1) {//we're moving west
      neighbor->flags_shape |= REGION_FLAG_RIVERE;
      r.flags_shape |= REGION_FLAG_RIVERW;
    }
    if (selected.x == 1) {//we're moving east
      neighbor->flags_shape |= REGION_FLAG_RIVERW;
      r.flags_shape |= REGION_FLAG_RIVERE;
    }
    continent[x][y] = r;
    x += selected.x;
    y += selected.y;
  }
  return true;

}

//Drop a point in the middle of the terrain and attempt to
//place a river. 
static void do_rivers (int count)
{

  int         rivers;
  int         cycles;
  int         x, y;

  rivers = 0;
  cycles = 0;
  while (rivers < count && cycles < 100) {
    x = REGION_CENTER + (RandomVal () % 30) - 15;
    y = REGION_CENTER + (RandomVal () % 30) - 15;
    if (try_river (x, y, rivers)) 
      rivers++;
    cycles++;
  }
  cycles = 0;

}

//look around the map and find an unused area of the desired size
static bool find_plot (int radius, GLcoord* result)
{

  int       cycles;
  GLcoord   test;
  
  cycles = 0;
  while (cycles < 20) {
    cycles++;
    test.x = RandomVal () % REGION_GRID;
    test.y = RandomVal () % REGION_GRID;
    if (is_free (test.x, test.y, radius)) {
      *result = test;
      return true;
    }
  }
  //couldn't find a spot. Map is full, or just bad dice rolls. 
  return false; 

}

//Randomly scatter some mountains around
static void do_mountains (int count)
{


  //now place a few mountains 
  int     mtn_size;
  int     step;
  float   height;
  int     i;
  int     x, y;
  GLcoord plot;
  Region  r;

  for (i = 0; i < count; i++) {
    mtn_size = 3;
    if (!find_plot (mtn_size, &plot))
      continue;
    for (x = -mtn_size; x <= mtn_size; x++) {
      for (y = -mtn_size; y <= mtn_size; y++) {
        r = continent[plot.x + x][plot.y + y];
        step = (max (abs (x), abs (y)));
        if (step == 0) {
          sprintf (r.title, "Mountain Summit");
        } else if (step == mtn_size) 
          sprintf (r.title, "Mountain Foothills");
        else {
          sprintf (r.title, "Mountain");
        }
        r.mountain_height = mtn_size - step;
        //Lose 20 degrees for every step up
        height = 1.0f - (float)step / (float)mtn_size;
        r.geo_large += 0.2f + height;
        r.geo_detail += 0.3f;
        r.geo_bias += (height + RandomFloat ()) * REGION_HALF;
        r.flags_shape = REGION_FLAG_NOBLEND;
        r.climate = CLIMATE_MOUNTAIN;
        continent[plot.x + x][plot.y + y] = r;
      }
    }
  }

}

//Blur the region attributes by averaging each region with its
//neighbors.  This prevents overly harsh transitions.
static void do_blur ()
{

  int   x, y, xx, yy, count;
  int   radius;
  float (*temp)[REGION_GRID];
  float (*moist)[REGION_GRID];
  float (*elev)[REGION_GRID];
  float (*sm)[REGION_GRID];
  float (*lg)[REGION_GRID];

  temp = new float[REGION_GRID][REGION_GRID];
  moist = new float[REGION_GRID][REGION_GRID];
  elev = new float[REGION_GRID][REGION_GRID];
  sm = new float[REGION_GRID][REGION_GRID];
  lg = new float[REGION_GRID][REGION_GRID];

  //Blur some of the attributes
  for (int passes = 0; passes < 5; passes++) {

    radius = 3;
    for (x = radius; x < REGION_GRID - radius; x++) {
      for (y = radius; y < REGION_GRID - radius; y++) {
        temp[x][y] = 0;
        moist[x][y] = 0;
        elev[x][y] = 0;
        sm[x][y] = 0;
        lg[x][y] = 0;
        count = 0;
        for (xx = -radius; xx <= radius; xx++) {
          for (yy = -radius; yy <= radius; yy++) {
            temp[x][y] += continent[x + xx][y + yy].temperature;
            moist[x][y] += continent[x + xx][y + yy].moisture;
            elev[x][y] += continent[x + xx][y + yy].geo_bias;
            sm[x][y] += continent[x + xx][y + yy].geo_detail;
            lg[x][y] += continent[x + xx][y + yy].geo_large;
            count++;
          }
        }
        temp[x][y] /= (float)count;
        moist[x][y] /= (float)count;
        elev[x][y] /= (float)count;
        sm[x][y] /= (float)count;
        lg[x][y] /= (float)count;
      }
    }
    //Put the blurred values back into our table
    for (x = radius; x < REGION_GRID - radius; x++) {
      for (y = radius; y < REGION_GRID - radius; y++) {
        continent[x][y].temperature = temp[x][y];
        //Rivers can get wetter through this process, but not drier.
        if (continent[x][y].climate == CLIMATE_RIVER) 
          continent[x][y].moisture = max (continent[x][y].moisture, moist[x][y]);
        else if (continent[x][y].climate != CLIMATE_OCEAN) 
          continent[x][y].moisture = moist[x][y];//No matter how arid it is, the OCEANS STAY WET!
        if (!(continent[x][y].flags_shape & REGION_FLAG_NOBLEND)) {
          //continent[x][y].geo_bias = elev[x][y];
          continent[x][y].geo_detail = sm[x][y];
          continent[x][y].geo_large = lg[x][y];
        }
      }
    }
  }
  delete []temp;
  delete []moist;
  delete []elev;
  delete []sm;
  delete []lg;
  
}

//Test the given area and see if it contains the given climate.
static bool climate_present (int x, int y, int radius, Climate c) 
{

  GLcoord   start, end;
  int       xx, yy;

  start.x = max (x - radius, 0);
  start.y = max (y - radius, 0);
  end.x = min (x + radius, REGION_GRID - 1);
  end.y = min (y + radius, REGION_GRID - 1);
  for (xx = start.x; xx <= end.x; xx++) {
    for (yy = start.y; yy <= end.y; yy++) {
      if (continent[xx][yy].climate == c)
        return true;
    }
  }
  return false;

}

//Test the given area and see if it ONLY contains the given climate.
static bool climate_exclusive (int x, int y, int radius, Climate c) 
{

  GLcoord   start, end;
  int       xx, yy;

  start.x = max (x - radius, 0);
  start.y = max (y - radius, 0);
  end.x = min (x + radius, REGION_GRID - 1);
  end.y = min (y + radius, REGION_GRID - 1);
  for (xx = start.x; xx <= end.x; xx++) {
    for (yy = start.y; yy <= end.y; yy++) {
      if (continent[xx][yy].climate != c)
        return false;
    }
  }
  return true;

}


//pass over the map, calculate the temp & moisture
static void do_climate () 
{

  int     x, y;  
  float   moist, temp;
  Region  r;

  for (y = 0; y < REGION_GRID; y++) {
    moist = 1.0f;
    for (x = 0; x < REGION_GRID; x++) {
      r = continent[x][y];
      moist -= 1.0f / REGION_CENTER;
      //Mountains block rainfall
      if (r.climate == CLIMATE_MOUNTAIN) {
        moist -= 0.1f * r.mountain_height;
      }       
      moist = max (moist, 0);
      r.moisture = moist;
      //Rivers always give some moisture
      if (r.climate == CLIMATE_RIVER) {
        r.moisture = max (r.moisture, 0.75f);
        moist += 0.2f;
        moist = min (moist, 1);
      }
      //The north 25% is max cold.  The south 25% is all tropical
      temp = ((float)y - (REGION_GRID / 4)) / REGION_CENTER;
      if (r.mountain_height) {
        temp -= (float)r.mountain_height * 0.2f;
      }
      temp = clamp (temp, MIN_TEMP, MAX_TEMP);
      //oceans have a moderating effect
      if (r.climate == CLIMATE_OCEAN) {
        if (temp > 0.99f)
          temp = temp;
        temp = (temp + 0.5f) / 2.0f;
        r.moisture = 1.0f;
        moist = 1.0f;
      }
      r.temperature = temp;
      continent[x][y] = r;
    }
  }

}

static void do_oceans ()
{

  int     x, y;
  Region  r;
  bool    is_ocean;
  
  //define the oceans at the edge of the world
  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
      r = continent[x][y];
      is_ocean = false;
      if (r.geo_scale <= 0.0f) 
        is_ocean = true;
      if (x == 0 || y == 0 || x == REGION_GRID - 1 || y == REGION_GRID - 1) 
        is_ocean = true;
      if (is_ocean) {
        //depth = (-1.0f - r.elevation);
        //r.color_map = glRgba (0.0f, 0.7f + r.elevation * 4.0f, 1.0f + r.elevation / 4.0f);
        r.color_map.Clamp ();
        //depth = max (depth, 0.0f);
        //depth = abs (r.elevation) * 3;
        //depth = min (depth, 1);
        r.geo_large = 0.0f;
        r.geo_detail = 0.3f;
        r.moisture = 1.0f;
        r.geo_bias = -10.0f;
        r.flags_shape = REGION_FLAG_NOBLEND;
        r.climate = CLIMATE_OCEAN;
        sprintf (r.title, "%s Ocean", find_direction_name (x, y));
        //Ocean rock is always white (Earthtones look bad)
        r.color_rock = glRgba (1.0f);
        continent[x][y] = r;
      }        
    }
  }

}

static void do_coast ()
{

  int             x, y;
  Region          r;
  int             pass;
  unsigned        i;
  bool            is_coast;
  vector<GLcoord> queue;
  GLcoord         current;

  //now define the coast 
  for (pass = 0; pass < 2; pass++) {
    queue.clear ();
    for (x = 0; x < REGION_GRID; x++) {
      for (y = 0; y < REGION_GRID; y++) {
        r = continent[x][y];
        //Skip already assigned places
        if (r.climate != CLIMATE_INVALID)
          continue;
        is_coast = false;
        //On the first pass, we add beach adjoining the sea
        if (!pass && climate_present (x, y, 1, CLIMATE_OCEAN)) 
          is_coast = true;
        //One the second pass, we add beach adjoining the beach we added on the previous step
        if (pass && climate_present (x, y, 1, CLIMATE_COAST)) 
          is_coast = true;
        if (is_coast) {
          current.x = x;
          current.y = y;
          queue.push_back (current);
        }
      }
    }
    //Now we're done scanning the map.  Run through our list and make the new regions.
    for (i = 0; i < queue.size (); i++) {
      current = queue[i];
      r = continent[current.x][current.y];
      if (!pass) 
        sprintf (r.title, "%s beach", find_direction_name (x, y));
      else
        sprintf (r.title, "%s coast", find_direction_name (x, y));
      r.geo_large = 0.0f;
      //beaches are low and partially submerged
      if (!pass) {
        r.geo_bias = -2.0f;
        r.geo_detail = 4.0f;
      } else {
        r.geo_bias = 0.5f;
        r.geo_detail = 14.5f;
      }
      r.moisture = 1.0f;
      r.flags_shape |= REGION_FLAG_NOBLEND | REGION_FLAG_BEACH;
      r.beach_threshold = 3.0f + RandomFloat () * 5.0f;
      r.climate = CLIMATE_COAST;
      continent[current.x][current.y] = r;
    }
  }

}

//This will fill in all reviously un-assigned regions.
static void do_landmass ()
{

  int       x, y;
  Region    r;
  unsigned  rand;

  //now define the interior 
  for (x = 1; x < REGION_GRID - 1; x++) {
    for (y = 1; y < REGION_GRID - 1; y++) {
      r = continent[x][y];
      //See if this is already ocean
      if (r.climate != CLIMATE_INVALID)
        continue;
      //noise = Entropy (x, y);
      sprintf (r.title, "???");
      r.geo_bias = r.geo_scale * 10.0f;
      r.geo_detail = 20.0f;
      //Have them trend more hilly in dry areas
      //r.geo_detail += (1.0f - r.moisture) * RandomFloat () * 0.5f;
      //r.geo_bias += RandomFloat () * 6;
      
      rand = RandomVal () % 8;
      if (r.moisture > 0.3f && r.temperature > 0.5f) {
        GLrgba    c;
        int       shape;
        
        r.has_flowers = RandomVal () % 4 == 0;
        shape = RandomVal ();
        c = flower_palette[RandomVal () % FLOWER_PALETTE];
        for (int i = 0; i < FLOWERS; i++) {
          r.color_flowers[i] = c;
          r.flower_shape[i] = shape;
          if ((RandomVal () % 15) == 0) {
            shape = RandomVal ();
            c = flower_palette[RandomVal () % FLOWER_PALETTE];
          }
        }
      }      
      if (rand == 0) {
        r.flags_shape |= REGION_FLAG_MESAS;
        sprintf (r.title, "Mesas");
      } else if (rand == 1) {
        sprintf (r.title, "Craters");
        r.flags_shape |= REGION_FLAG_CRATER;
      } else if (rand == 2) {
        sprintf (r.title, "TEST");
        r.flags_shape |= REGION_FLAG_TEST;
      } else if (rand == 3) {
        sprintf (r.title, "Sinkhole");
        r.flags_shape |= REGION_FLAG_SINKHOLE;
      } else if (rand == 4) {
        sprintf (r.title, "Crack");
        r.flags_shape |= REGION_FLAG_CRACK;
      } else if (rand == 5) {
        sprintf (r.title, "Tiered");
        r.flags_shape |= REGION_FLAG_TIERED;
      } else if (rand == 6) {
        sprintf (r.title, "Wasteland");
      } else if (rand == 7) {
        sprintf (r.title, "Swamp");
        r.climate = CLIMATE_SWAMP;
        r.color_atmosphere = glRgba (0.0f, 0.5f, 0.0f);
        r.moisture = 1.0f;
        r.geo_detail = 3.0f;
        r.has_flowers = false;
        r.flags_shape |= REGION_FLAG_NOBLEND;
      } else {
        sprintf (r.title, "Grasslands");
        //r.geo_detail /= 3;
        //r.geo_large /= 3;
      }  
      

      continent[x][y] = r;
    }
  }

}

static void do_canyons (int count)
{

  Region    r;
  GLcoord   plot;
  float     depth;
  int       x, y;
  int       can_size;

  for (x = 0; x < count; x++) {
    can_size = 2;
    if (!find_plot (can_size, &plot))
      continue;
    depth = 0.5f + RandomFloat () * 3.0f;
    for (y = -can_size; y <= can_size; y++) {
      r = continent[plot.x][plot.y + y];
      sprintf (r.title, "Canyon");
      r.geo_large += 0.2f;
      r.geo_detail = 0.5f;
      r.threshold = 0.5f + depth * (float)(can_size - abs (y));
      r.flags_shape |= REGION_FLAG_CANYON_NS;
      continent[plot.x][plot.y + y] = r;
    }
  }

}


//This finds and identifies the ocean regions distant from the shore.
//(Ocean points which have ONLY ocean around them.  It hammers them
//flat. No sense in wasting detail out there.
static void do_oceans_deep ()
{

  int       x, y;
  Region    r;

  for (x = 0; x < REGION_GRID - 1; x++) {
    for (y = 0; y < REGION_GRID - 1; y++) {
      r = continent[x][y];
      //See if this is already ocean
      if (r.climate != CLIMATE_OCEAN)
        continue;
      if (climate_exclusive (x, y, 2, CLIMATE_OCEAN)) {
        sprintf (r.title, "Deep Ocean");
        r.geo_bias = -10;
        r.geo_large = 0.0f;
        r.geo_detail = 0.0f;
        r.moisture = 1.0f;
        r.flags_shape = REGION_FLAG_NOBLEND;
        r.climate = CLIMATE_OCEAN;
        continent[x][y] = r;
      }
    }
  }

}

static void do_colors ()
{

  int       x, y;
  Region    r;
  float     fade;
  GLrgba    warm_grass, cold_grass, wet_grass, dry_grass, dead_grass;
  GLrgba    cold_dirt, warm_dirt, dry_dirt, wet_dirt;
  GLrgba    humid_air, dry_air, cold_air, warm_air;
  GLrgba    warm_rock, cold_rock;

  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
      r = continent[x][y];
      //Devise a grass color

      //wet grass is deep greens
      wet_grass.red = RandomFloat () * 0.3f;
      wet_grass.green = 0.4f + RandomFloat () * 0.6f;
      wet_grass.blue = RandomFloat () * 0.3f;
      //Dry grass is mostly reds and oranges
      dry_grass.red = 0.7f + RandomFloat () * 0.3f;
      dry_grass.green = 0.5f + RandomFloat () * 0.5f;
      dry_grass.blue = 0.0f + RandomFloat () * 0.3f;
      //Dead grass is pale beige
      dead_grass = glRgba (0.7f, 0.6f, 0.5f);
      dead_grass *= 0.7f + RandomFloat () * 0.3f;
      if (r.moisture < 0.5f) {
        fade = r.moisture * 2.0f;
        warm_grass = glRgbaInterpolate (dead_grass, dry_grass, fade);
      } else {
        fade = (r.moisture - 0.5f) * 2.0f;
        warm_grass = glRgbaInterpolate (dry_grass, wet_grass, fade);
      }
      //cold grass is pale and a little blue
      cold_grass.red = 0.5f + RandomFloat () * 0.2f;
      cold_grass.green = 0.8f + RandomFloat () * 0.2f;
      cold_grass.blue = 0.7f + RandomFloat () * 0.2f;
      if (r.temperature < COLD)
        r.color_grass = glRgbaInterpolate (cold_grass, warm_grass, r.temperature / COLD);
      else
        r.color_grass = warm_grass;
      //Devise a random but plausible dirt color
      //Dry dirts are mostly reds, oranges, and browns
      dry_dirt.red = 0.4f + RandomFloat () * 0.6f;
      dry_dirt.green = 0.4f + RandomFloat () * 0.6f;
      dry_dirt.green = min (dry_dirt.green, dry_dirt.red);
      dry_dirt.green = 0.1f + RandomFloat () * 0.5f;
      dry_dirt.blue = 0.2f + RandomFloat () * 0.4f;
      dry_dirt.blue = min (dry_dirt.blue, dry_dirt.green);
      //wet dirt is various browns
      fade = RandomFloat () * 0.6f;
      wet_dirt.red = 0.2f + fade;
      wet_dirt.green = 0.1f + fade;
      wet_dirt.blue = 0.0f +  fade / 2.0f;
      wet_dirt.green += RandomFloat () * 0.1f;
      //cold dirt is pale
      cold_dirt = glRgbaInterpolate (wet_dirt, glRgba (0.7f), 0.5f);
      //warm dirt us a fade from wet to dry
      warm_dirt = glRgbaInterpolate (dry_dirt, wet_dirt, r.moisture);
      fade = MathScalar (r.temperature, FREEZING, 1.0f);
      r.color_dirt = glRgbaInterpolate (cold_dirt, warm_dirt, fade);

      //"atmosphere" is the overall color of the lighting & fog. 
      humid_air = glRgba (1.0f, 1.0f, 0.3f);
      dry_air = glRgba (1.0f, 0.7f, 0.3f);
      warm_air = glRgbaInterpolate (dry_air, humid_air, r.moisture);
      cold_air = glRgba (0.3f, 0.7f, 1.0f);
      r.color_atmosphere = glRgbaInterpolate (cold_air, warm_air, r.temperature);

      //Devise a rock color
      fade = MathScalar (r.temperature, FREEZING, 1.0f);
      //Warm rock is red
      warm_rock.red = 1.0f;
      warm_rock.green = 1.0f - RandomFloat () * 0.6f;
      warm_rock.blue = 1.0f - RandomFloat () * 0.6f;
      //Cold rock is white or blue
      cold_rock.blue = 1.0f;
      cold_rock.green = 1.0f - RandomFloat () * 0.4f;
      cold_rock.red = cold_rock.green;
      r.color_rock = glRgbaInterpolate (cold_rock, warm_rock, fade);
      
      //Color the map
      switch (r.climate) {
      case CLIMATE_MOUNTAIN:
        r.color_map = glRgba (0.5f, 0.5f, 0.5f);break;
      case CLIMATE_COAST:
        r.color_map = glRgba (0.9f, 0.7f, 0.4f);break;
      case CLIMATE_OCEAN:
        break;//We set this when we made the ocean
      case CLIMATE_RIVER:
        r.color_map = glRgba (0.0f, 0.0f, 0.6f);
        break;
      case CLIMATE_RIVER_BANK:
        r.color_map = r.color_dirt;
        break;
      case CLIMATE_SWAMP:
        r.color_grass *= 0.5f;
        r.color_map = r.color_grass * 0.5f;
        break;
      default:
        r.color_map = r.color_grass;
        break;
      }
      if (r.geo_scale >= 0.0f)
        r.color_map *= (r.geo_scale * 0.5f + 0.5f);
      continent[x][y] = r;
    }
  }
  
}


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

  buffer = new unsigned char[REGION_GRID * REGION_GRID * 3];

  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
      //Flip it vertically, because the OpenGL texture coord system is retarded.
      yy = (REGION_GRID - 1) - y;
      r = continent[x][yy];
      ptr = &buffer[(x + y * REGION_GRID) * 3];
      ptr[0] = (unsigned char)(r.color_map.red * 255.0f);
      ptr[1] = (unsigned char)(r.color_map.green * 255.0f);
      ptr[2] = (unsigned char)(r.color_map.blue * 255.0f);
    }
  }
  glTexImage2D (GL_TEXTURE_2D, 0, GL_RGB, REGION_GRID, REGION_GRID, 0, GL_RGB, GL_UNSIGNED_BYTE, &buffer[0]);
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
  if (x < 0 || y < 0 || x >= REGION_GRID || y >= REGION_GRID)
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
  if (x < 0 || y < 0 || x >= REGION_GRID || y >= REGION_GRID)
    return continent[0][0];
  return continent[x][y];

}


void RegionSet (int x, int y, Region val)
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
  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
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
  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
      memset (&r, 0, sizeof (Region));
      sprintf (r.title, "NOTHING");
      r.geo_large = r.geo_detail = 0;
      r.mountain_height = 0;
      r.grid_pos.x = x;
      r.grid_pos.y = y;
      from_center.x = abs (x - REGION_CENTER);
      from_center.y = abs (y - REGION_CENTER);
      //Geo scale is a number from -1 to 1. -1 is lowest ovean. 0 is sea level. 
      //+1 is highest elevation on the island. This is used to guide other derived numbers.
      r.geo_scale = glVectorLength (glVector ((float)from_center.x, (float)from_center.y));
      r.geo_scale /= (REGION_CENTER - OCEAN_BUFFER);
      r.geo_scale = 1.0f - r.geo_scale;
      //Create a steep drop around the edge of the world
      if (r.geo_scale < -0.5f)
        r.geo_scale *= 2.0f;
      r.geo_scale += (Entropy ((x + offset.x) * FREQUENCY, (y + offset.y) * FREQUENCY) - 0.2f) / 3;
      r.geo_scale = clamp (r.geo_scale, -1, 1);
      if (r.geo_scale > 0.0f)
        r.geo_bias = 1.0f + r.geo_scale;
      r.geo_large = 0.3f;
      r.geo_large = 0.0f;
      r.geo_detail = 0.0f;
      r.color_map = glRgba (0.0f);
      r.climate = CLIMATE_INVALID;
      continent[x][y] = r;
    }
  }

  do_oceans ();
  do_coast ();
  //do_oceans_deep ();
  //do_mountains (3);
  //do_canyons (3);
  do_climate ();
  do_rivers (4);
  do_climate ();//Do climate a second time now that rivers are in
  do_landmass ();
  for (x = 0; x < REGION_GRID; x++) {
    for (y = 0; y < REGION_GRID; y++) {
      r = continent[x][y];
      //r.geo_detail = 0.0f;
      continent[x][y] = r;
    }
  }

  do_blur ();
  do_colors ();
  do_map ();
  
}

GLrgba RegionColorGet (int world_x, int world_y, SurfaceColor c)
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
  //return r0.color_map;////////////////////////////////////////////////////   
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
  /*
  GLrgba    c0, c1, c2, c3, result;
  Region    r0, r1, r2, r3;

  r0 = region (origin.x, origin.y);
  r1 = region (origin.x + 1, origin.y);
  r2 = region (origin.x, origin.y + 1);
  r3 = region (origin.x + 1, origin.y + 1);
  c0 = r0.color_atmosphere;
  c1 = r1.color_atmosphere;
  c2 = r2.color_atmosphere;
  c3 = r3.color_atmosphere;
  result.red   = MathInterpolateQuad (c0.red, c1.red, c2.red, c3.red, offset);
  result.green = MathInterpolateQuad (c0.green, c1.green, c2.green, c3.green, offset);
  result.blue  = MathInterpolateQuad (c0.blue, c1.blue, c2.blue, c3.blue, offset);
  return result;
  */
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
  origin.x = clamp (origin.x, 0, REGION_GRID - 1);
  origin.y = clamp (origin.y, 0, REGION_GRID - 1);
  offset.x = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
  offset.y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
  rul = region (origin.x, origin.y);
  rur = region (origin.x + 1, origin.y);
  rbl = region (origin.x, origin.y + 1);
  rbr = region (origin.x + 1, origin.y + 1);
  return MathInterpolateQuad (rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset, ((origin.x + origin.y) %2) == 0);

}

Cell RegionCell (int world_x, int world_y)
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
  origin.x = clamp (origin.x, 0, REGION_GRID - 1);
  origin.y = clamp (origin.y, 0, REGION_GRID - 1);
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
