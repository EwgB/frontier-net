/*-----------------------------------------------------------------------------

  Terraform.cpp


-------------------------------------------------------------------------------

  This module is a set of worker functions for World.cpp.  Really, everything
  here could go in World.cpp, except that region.cpp would be just too 
  damn big and unmanageable. 

  Still, this system isn't connected to anything else and it's only used
  when World.cpp is generating region data.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "entropy.h"
#include "math.h"
#include "random.h"
#include "world.h"

//The number of regions around the edge which should be ocean.
#define OCEAN_BUFFER      (WORLD_GRID / 6) 
//This affects the mapping of the coastline.  Higher = busier, more repetitive coast.
#define FREQUENCY         3 
//How many different colors of flowers are available
#define FLOWER_PALETTE    (sizeof (flower_palette) / sizeof (GLrgba))

static char*        direction_name[] = 
{
  "Northern",
  "Southern",
  "Eastern",
  "Western"
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

/*-----------------------------------------------------------------------------
Helper functions
-----------------------------------------------------------------------------*/

//In general, what part of the map is this coordinate in?
static char* get_direction_name (int x, int y)
{

  GLcoord   from_center;

  from_center.x = abs (x - WORLD_GRID_CENTER);
  from_center.y = abs (y - WORLD_GRID_CENTER);
  if (from_center.x < from_center.y) {
    if (y < WORLD_GRID_CENTER)
      return direction_name[NORTH];
    else
      return direction_name[SOUTH];
  } 
  if (x < WORLD_GRID_CENTER)
    return direction_name[WEST];
  return direction_name[EAST];

}

//In general, what part of the map is this coordinate in?
GLcoord get_map_side (int x, int y)
{

  GLcoord   from_center;

  from_center.x = abs (x - WORLD_GRID_CENTER);
  from_center.y = abs (y - WORLD_GRID_CENTER);
  if (from_center.x < from_center.y) {
    if (y < WORLD_GRID_CENTER)
      return direction[NORTH];
    else
      return direction[SOUTH];
  } 
  if (x < WORLD_GRID_CENTER)
    return direction[WEST];
  return direction[EAST];

}

//Test the given area and see if it contains the given climate.
static bool is_climate_present (int x, int y, int radius, Climate c) 
{

  GLcoord   start, end;
  int       xx, yy;
  Region    r;

  start.x = max (x - radius, 0);
  start.y = max (y - radius, 0);
  end.x = min (x + radius, WORLD_GRID - 1);
  end.y = min (y + radius, WORLD_GRID - 1);
  for (xx = start.x; xx <= end.x; xx++) {
    for (yy = start.y; yy <= end.y; yy++) {
      r = WorldRegionGet (xx, yy);
      if (r.climate == c)
        return true;
    }
  }
  return false;

}

//check the regions around the given one, see if they are unused
static bool is_free (int x, int y, int radius)
{

  int       xx, yy;
  Region    r;

  for (xx = -radius; xx <= radius; xx++) {
    for (yy = -radius; yy <= radius; yy++) {
      if (x + xx < 0 || x + xx >= WORLD_GRID)
        return false;
      if (y + yy < 0 || y + yy >= WORLD_GRID)
        return false;
      r = WorldRegionGet (x + xx, y + yy);
      if (r.climate != CLIMATE_INVALID)
        return false;
    }
  }
  return true;

}


//look around the map and find an unused area of the desired size
static bool find_plot (int radius, GLcoord* result)
{

  int       cycles;
  GLcoord   test;
  
  cycles = 0;
  while (cycles < 20) {
    cycles++;
    test.x = RandomVal () % WORLD_GRID;
    test.y = RandomVal () % WORLD_GRID;
    if (is_free (test.x, test.y, radius)) {
      *result = test;
      return true;
    }
  }
  //couldn't find a spot. Map is full, or just bad dice rolls. 
  return false; 

}

//Gives a 1 in 'odds' chance of adding flowers to the given region
void add_flowers (Region* r, unsigned odds)
{

  GLrgba    c;
  int       shape;

  r->has_flowers = RandomVal () % odds == 0;
  shape = RandomVal ();
  c = flower_palette[RandomVal () % FLOWER_PALETTE];
  for (int i = 0; i < FLOWERS; i++) {
    r->color_flowers[i] = c;
    r->flower_shape[i] = shape;
    if ((RandomVal () % 15) == 0) {
      shape = RandomVal ();
      c = flower_palette[RandomVal () % FLOWER_PALETTE];
    }
  }


}

/*-----------------------------------------------------------------------------
Functions to place individual climates
-----------------------------------------------------------------------------*/



//Place one mountain
static void do_mountain (int x, int y, int mtn_size)
{

  int     step;
  Region  r;
  int     xx, yy;

  for (xx = -mtn_size; xx <= mtn_size; xx++) {
    for (yy = -mtn_size; yy <= mtn_size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      step = (max (abs (xx), abs (yy)));
      if (step == 0) {
        sprintf (r.title, "Mountain Summit");
      } else if (step == mtn_size) 
        sprintf (r.title, "Mountain Foothills");
      else {
        sprintf (r.title, "Mountain");
      }
      r.mountain_height = 1 + (mtn_size - step);
      r.geo_detail = 13.0f + r.mountain_height* 7.0f;
      r.geo_bias = (WorldNoisef (xx + yy) * 0.5f + (float)r.mountain_height) * REGION_SIZE / 2;
      r.flags_shape = REGION_FLAG_NOBLEND;
      r.climate = CLIMATE_MOUNTAIN;
      WorldRegionSet (xx + x, yy + y, r);
    }
  }

}

//Place a rocky wasteland
static void do_rocky (int x, int y, int size)
{

  Region  r;
  int     xx, yy;

  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Rocky Wasteland");
      r.geo_detail = 40.0f;
      //r.flags_shape = REGION_FLAG_NOBLEND;
      r.climate = CLIMATE_ROCKY;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}


//Place some plains
static void do_plains (int x, int y, int size)
{

  Region  r;
  int     xx, yy;
  float   water;

  r = WorldRegionGet (x, y);
  water = r.geo_water;
  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Plains");
      r.climate = CLIMATE_PLAINS;
      r.color_atmosphere = glRgba (0.0f, 0.5f, 0.0f);
      r.geo_water = water;
      r.geo_bias = 8.0f;
      r.moisture = 1.0f;
      r.geo_detail = 1.0f;
      add_flowers (&r, 8);
      r.flags_shape |= REGION_FLAG_NOBLEND;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}


//Place a swamp
static void do_swamp (int x, int y, int size)
{

  Region  r;
  int     xx, yy;
  float   water;

  r = WorldRegionGet (x, y);
  water = r.geo_water;
  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Swamp");
      r.climate = CLIMATE_SWAMP;
      r.color_atmosphere = glRgba (0.0f, 0.5f, 0.0f);
      r.geo_water = water;
      r.moisture = 1.0f;
      r.geo_detail = 8.0f;
      r.has_flowers = false;
      r.flags_shape |= REGION_FLAG_NOBLEND;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}

//Place a field of flowers
static void do_field (int x, int y, int size)
{

  Region    r;
  int       xx, yy;

  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Field");
      r.climate = CLIMATE_FIELD;
      add_flowers (&r, 4);
      /*
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
      */
      r.color_atmosphere = glRgba (0.7f, 0.6f, 0.4f);
      r.geo_detail = 8.0f;
      r.flags_shape |= REGION_FLAG_NOBLEND;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}


//Place a field of flowers
static void do_forest (int x, int y, int size)
{

  Region    r;
  int       xx, yy;

  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Forest");
      r.climate = CLIMATE_FOREST;
      r.color_atmosphere = glRgba (0.1f, 0.6f, 0.3f);
      r.geo_detail = 8.0f;
      r.tree_threshold = 0.75f;
      //r.flags_shape |= REGION_FLAG_NOBLEND;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}


//Place a desert
static void do_desert (int x, int y, int size)
{

  Region    r;
  int       xx, yy;

  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + x, yy + y);
      sprintf (r.title, "Desert");
      r.climate = CLIMATE_DESERT;
      r.color_atmosphere = glRgba (0.6f, 0.3f, 0.1f);
      r.geo_detail = 8.0f;
      r.geo_bias = 4.0f;
      r.tree_threshold = 0.0f;
      WorldRegionSet (x + xx, y + yy, r);
    }
  }

}



static void do_canyon (int x, int y, int radius)
{

  Region    r;
  int       yy;
  float     step;

  for (yy = -radius; yy <= radius; yy++) {
    r = WorldRegionGet (x, yy + y);
    step = (float)abs (yy) / (float)radius;
    step = 1.0f - step;
    sprintf (r.title, "Canyon");
    r.climate = CLIMATE_CANYON;
    r.geo_detail = 5 + step * 25.0f;
    //r.geo_detail = 1;
    r.flags_shape |= REGION_FLAG_CANYON_NS | REGION_FLAG_NOBLEND;
    WorldRegionSet (x, y + yy, r);
  }

}

static bool try_lake (int try_x, int try_y, int id)
{

  Region    r;
  int       xx, yy;
  int       size;
  float     depth;
  float     water_level;
  GLvector2 to_center;

  size = 4;
  //if (!is_free (try_x, try_y, size)) 
    //return false;
  //Find the lowest water level in our lake
  water_level = 9999.9f;
  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      r = WorldRegionGet (xx + try_x, yy + try_y);
      if (r.climate != CLIMATE_INVALID && r.climate != CLIMATE_RIVER && r.climate != CLIMATE_RIVER_BANK)
        return false;
      if (r.moisture < 0.5f)
        return false;
      water_level = min (water_level, r.geo_water);
    }
  }
  for (xx = -size; xx <= size; xx++) {
    for (yy = -size; yy <= size; yy++) {
      to_center = glVector ((float)xx, (float)yy);
      depth = to_center.Length ();
      if (depth >= (float)size)
        continue;
      depth = (float)size - depth;
      r = WorldRegionGet (xx + try_x, yy + try_y);
      sprintf (r.title, "Lake%d", id);
      r.geo_water = water_level;
      r.geo_detail = 2.0f;
      r.geo_bias = -4.0f * depth;
      r.climate = CLIMATE_LAKE;
      r.flags_shape |= REGION_FLAG_NOBLEND;
      WorldRegionSet (xx + try_x, yy + try_y, r);
    }
  }
  return true;

}

static bool try_river (int start_x, int start_y, int id)
{

  Region            r;
  Region            neighbor;
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
    r = WorldRegionGet (x, y);
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
    lowest = r.geo_water;
    to_coast = get_map_side (x, y);
    //lowest = 999.9f;
    selected.Clear ();
    for (d = 0; d < 4; d++) {
      neighbor = WorldRegionGet (x + direction[d].x, y + direction[d].y);
      //Don't reverse course into ourselves
      if (last_move == (direction[d] * -1))
        continue;
      //ALWAYS go for the ocean, if available
      if (neighbor.climate == CLIMATE_OCEAN) {
        selected = direction[d];
        lowest = neighbor.geo_water;
      } 
      //Don't head directly AWAY from the coast
      if (direction[d] == to_coast * -1)
        continue;
      //Go whichever way is lowest
      if (neighbor.geo_water < lowest) {
        selected = direction[d];
        lowest = neighbor.geo_water;
      }
      //WorldRegionSet (x + direction[d].x, y + direction[d].y, neighbor);
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
  if (path.size () < (WORLD_GRID / 4))
    return false;
  //The river is good. Place it.
  x = start_x;
  y = start_y;
  water_strength = 0.03f;
  water_level = WorldRegionGet (x, y).geo_water;
  for (d = 0; d < path.size (); d++) {
    r = WorldRegionGet (x, y);
    if (!d)
      sprintf (r.title, "River%d-Source", id);
    else if (d == path.size () - 1) 
      sprintf (r.title, "River%d-Mouth", id);
    else
      sprintf (r.title, "River%d-%d", id, d);
    //A river should attain full strength after crossing 1/4 of the map
    water_strength += (1.0f / ((float)WORLD_GRID / 4.0f));
    water_strength = min (water_strength, 1);
    r.flags_shape |= REGION_FLAG_NOBLEND;
    r.river_id = id;
    r.moisture = max (r.moisture, 0.5f);
    r.river_segment = d;
    //Rivers get flatter as they go, travel from rocky streams to wide river plains
    r.geo_detail = 28.0f - water_strength * 20.0f;
    r.river_width = min (water_strength, 1);
    r.climate = CLIMATE_RIVER;
    water_level = min (r.geo_water, water_level);
    //We need to flatten out this space, as well as all of its neighbors.
    r.geo_water = water_level;
    for (xx = x - 1; xx <= x + 1; xx++) {
      for (yy = y - 1; yy <= y + 1; yy++) {
        neighbor = WorldRegionGet (xx, yy);
        if (neighbor.climate != CLIMATE_INVALID) 
          continue;
        if (!xx && !yy)
          continue;
        neighbor.geo_water = min (neighbor.geo_water, water_level);
        neighbor.geo_bias = r.geo_bias;
        neighbor.geo_detail = r.geo_detail;
        neighbor.climate = CLIMATE_RIVER_BANK;
        neighbor.flags_shape |= REGION_FLAG_NOBLEND;
        sprintf (neighbor.title, "River%d-Banks", id);
        WorldRegionSet (xx, yy, neighbor);
      }
    }
    selected = path[d];
    //neighbor = &continent[x + selected.x][y + selected.y];
    neighbor = WorldRegionGet (x + selected.x, y + selected.y);
    if (selected.y == -1) {//we're moving north
      neighbor.flags_shape |= REGION_FLAG_RIVERS;
      r.flags_shape |= REGION_FLAG_RIVERN;
    }
    if (selected.y == 1) {//we're moving south
      neighbor.flags_shape |= REGION_FLAG_RIVERN;
      r.flags_shape |= REGION_FLAG_RIVERS;
    }
    if (selected.x == -1) {//we're moving west
      neighbor.flags_shape |= REGION_FLAG_RIVERE;
      r.flags_shape |= REGION_FLAG_RIVERW;
    }
    if (selected.x == 1) {//we're moving east
      neighbor.flags_shape |= REGION_FLAG_RIVERW;
      r.flags_shape |= REGION_FLAG_RIVERE;
    }
    WorldRegionSet (x, y, r);
    WorldRegionSet (x + selected.x, y + selected.y, neighbor);
    x += selected.x;
    y += selected.y;
  }
  return true;

}


/*-----------------------------------------------------------------------------
The following functions are used when building a new world.
-----------------------------------------------------------------------------*/

//pass over the map, calculate the temp & moisture
void TerraformClimate () 
{

  int       x, y;  
  float     rainfall, rain_loss, temp;
  Region    r;
  GLvector2 from_center;
  float     distance;
  GLcoord   walk;
  World*    w;

  rainfall = 1.0f;
  w = WorldPtr ();
  walk.Clear ();
  do {
    //Wind (and thus rainfall) come from west.
    if (w->wind_from_west) 
      x = walk.x; 
    else 
      x = (WORLD_GRID - 1) - walk.x;
    y = walk.y;
    r = WorldRegionGet (x, y);
    //************   TEMPERATURE *******************//
    //The north 25% is max cold.  The south 25% is all tropical
    //On a southern hemisphere map, this is reversed.
    if (w->northern_hemisphere)
      temp = ((float)y - (WORLD_GRID / 4)) / WORLD_GRID_CENTER;
    else 
      temp = ((float)(WORLD_GRID - y) - (WORLD_GRID / 4)) / WORLD_GRID_CENTER;
    //Mountains are cooler at the top
    if (r.mountain_height) 
      temp -= (float)r.mountain_height * 0.15f;
    //We add a slight bit of heat to the center of the map, to
    //round off climate boundaries.
    from_center = glVector ((float)(x - WORLD_GRID_CENTER), (float)(x - WORLD_GRID_CENTER));
    distance = from_center.Length () / WORLD_GRID_CENTER;
    temp += distance * 0.2f;
    temp = clamp (temp, MIN_TEMP, MAX_TEMP);
    //************  RAINFALL *******************//
    //Oceans are ALWAYS WET.
    if (r.climate == CLIMATE_OCEAN)
      rainfall = 1.0f;
    //We lose rainfall as we move inland.
    rain_loss = 1.5f / WORLD_GRID_CENTER;
    //We lose rainfall more slowly as it gets colder.
    if (temp < 0.5f)
      rain_loss *= temp * 2.0f;
    rainfall -= rain_loss;
    //Mountains block rainfall
    if (r.climate == CLIMATE_MOUNTAIN) 
      rainfall -= 0.1f * r.mountain_height;
    r.moisture = max (rainfall, 0);
    //Rivers always give some moisture
    if (r.climate == CLIMATE_RIVER || r.climate == CLIMATE_RIVER_BANK) {
      r.moisture = max (r.moisture, 0.75f);
      rainfall += 0.05f;
      rainfall = min (rainfall, 1);
    }
    //oceans have a moderating effect on climate
    if (r.climate == CLIMATE_OCEAN) 
      temp = (temp + 0.5f) / 2.0f;
    r.temperature = temp;
    //r.moisture = min (1, r.moisture + WorldNoisef (walk.x + walk.y * WORLD_GRID) * 0.1f);
    //r.temperature = min (1, r.temperature + WorldNoisef (walk.x + walk.y * WORLD_GRID) * 0.1f);
    WorldRegionSet (x, y, r);
  } while (!walk.Walk (WORLD_GRID));

}


//Figure out what plant life should grow here.
void TerraformFlora () 
{

  Region    r;
  GLcoord   walk;

  walk.Clear ();
  do {
    r = WorldRegionGet (walk.x, walk.y);
    r.tree_type =  WorldTreeType (r.moisture, r.temperature);
    if (r.climate == CLIMATE_FOREST)
      r.tree_type = WorldCanopyTree ();
    WorldRegionSet (walk.x, walk.y, r);
  } while (!walk.Walk (WORLD_GRID));

}

GLrgba TerraformColorGenerate (SurfaceColor c, float moisture, float temperature, int seed)
{

  float     fade;

  switch (c) {
  case SURFACE_COLOR_GRASS:
    GLrgba    warm_grass, cold_grass, wet_grass, dry_grass, dead_grass;

    wet_grass.red = WorldNoisef (seed++) * 0.3f;
    wet_grass.green = 0.4f + WorldNoisef (seed++) * 0.6f;
    wet_grass.blue = WorldNoisef (seed++) * 0.3f;
    //Dry grass is mostly reds and oranges
    dry_grass.red = 0.7f + WorldNoisef (seed++) * 0.3f;
    dry_grass.green = 0.5f + WorldNoisef (seed++) * 0.5f;
    dry_grass.blue = 0.0f + WorldNoisef (seed++) * 0.3f;
    //Dead grass is pale beige
    dead_grass = glRgba (0.7f, 0.6f, 0.5f);
    dead_grass *= 0.7f + WorldNoisef (seed++) * 0.3f;
    if (moisture < 0.5f) {
      fade = moisture * 2.0f;
      warm_grass = glRgbaInterpolate (dead_grass, dry_grass, fade);
    } else {
      fade = (moisture - 0.5f) * 2.0f;
      warm_grass = glRgbaInterpolate (dry_grass, wet_grass, fade);
    }
    //cold grass is pale and a little blue
    cold_grass.red = 0.5f + WorldNoisef (seed++) * 0.2f;
    cold_grass.green = 0.8f + WorldNoisef (seed++) * 0.2f;
    cold_grass.blue = 0.7f + WorldNoisef (seed++) * 0.2f;
    if (temperature < TEMP_COLD)
      return glRgbaInterpolate (cold_grass, warm_grass, temperature / TEMP_COLD);
    return warm_grass;
  case SURFACE_COLOR_DIRT:
    GLrgba    cold_dirt, warm_dirt, dry_dirt, wet_dirt;

    //Devise a random but plausible dirt color
    //Dry dirts are mostly reds, oranges, and browns
    dry_dirt.red = 0.4f + WorldNoisef (seed++) * 0.6f;
    dry_dirt.green = 0.4f + WorldNoisef (seed++) * 0.6f;
    dry_dirt.green = min (dry_dirt.green, dry_dirt.red);
    dry_dirt.green = 0.1f + WorldNoisef (seed++) * 0.5f;
    dry_dirt.blue = 0.2f + WorldNoisef (seed++) * 0.4f;
    dry_dirt.blue = min (dry_dirt.blue, dry_dirt.green);
    //wet dirt is various browns
    fade = WorldNoisef (seed++) * 0.6f;
    wet_dirt.red = 0.2f + fade;
    wet_dirt.green = 0.1f + fade;
    wet_dirt.blue = 0.0f +  fade / 2.0f;
    wet_dirt.green += WorldNoisef (seed++) * 0.1f;
    //cold dirt is pale
    cold_dirt = glRgbaInterpolate (wet_dirt, glRgba (0.7f), 0.5f);
    //warm dirt us a fade from wet to dry
    warm_dirt = glRgbaInterpolate (dry_dirt, wet_dirt, moisture);
    fade = MathScalar (temperature, FREEZING, 1.0f);
    return glRgbaInterpolate (cold_dirt, warm_dirt, fade);
  case SURFACE_COLOR_ROCK:
    GLrgba    warm_rock, cold_rock;

    //Devise a rock color
    fade = MathScalar (temperature, FREEZING, 1.0f);
    //Warm rock is red
    warm_rock.red = 1.0f;
    warm_rock.green = 1.0f - RandomFloat () * 0.6f;
    warm_rock.blue = 1.0f - RandomFloat () * 0.6f;
    //Cold rock is white or blue
    cold_rock.blue = 1.0f;
    cold_rock.green = 1.0f - RandomFloat () * 0.4f;
    cold_rock.red = cold_rock.green;
    return glRgbaInterpolate (cold_rock, warm_rock, fade);
  }
  //Shouldn't happen. Returns pink to flag the problem.
  return glRgba (1.0f, 0.0f, 1.0f);

}

//Determine the grass, dirt, rock, and other colors used by this region.
void TerraformColors ()
{

  int       x, y;
  Region    r;
  GLrgba    humid_air, dry_air, cold_air, warm_air;

  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      r = WorldRegionGet (x, y);
      r.color_grass = TerraformColorGenerate (SURFACE_COLOR_GRASS, r.moisture, r.temperature, r.grid_pos.x + r.grid_pos.y * WORLD_GRID);
      r.color_dirt = TerraformColorGenerate (SURFACE_COLOR_DIRT, r.moisture, r.temperature, r.grid_pos.x + r.grid_pos.y * WORLD_GRID);
      r.color_rock = TerraformColorGenerate (SURFACE_COLOR_ROCK, r.moisture, r.temperature, r.grid_pos.x + r.grid_pos.y * WORLD_GRID);
      //"atmosphere" is the overall color of the lighting & fog. 
      humid_air = glRgba (1.0f, 1.0f, 0.3f);
      dry_air = glRgba (1.0f, 0.7f, 0.3f);
      warm_air = glRgbaInterpolate (dry_air, humid_air, r.moisture);
      cold_air = glRgba (0.3f, 0.7f, 1.0f);
      r.color_atmosphere = glRgbaInterpolate (cold_air, warm_air, r.temperature);
      //Color the map
      switch (r.climate) {
      case CLIMATE_MOUNTAIN:
        r.color_map = glRgba (0.2f + (float)r.mountain_height / 4.0f);
        r.color_map.Normalize ();
        break;
      case CLIMATE_DESERT:
        r.color_map = glRgba (0.9f, 0.7f, 0.4f);
      case CLIMATE_COAST:
        if (r.flags_shape & REGION_FLAG_BEACH_CLIFF)
          r.color_map = glRgba (0.3f, 0.3f, 0.3f);
        else
          r.color_map = glRgba (0.9f, 0.7f, 0.4f);
        break;
      case CLIMATE_OCEAN:
        r.color_map = glRgba (0.0f, 1.0f + r.geo_scale * 2.0f, 1.0f + r.geo_scale);
        r.color_map.Clamp ();
        break;
      case CLIMATE_RIVER:
      case CLIMATE_LAKE:
        r.color_map = glRgba (0.0f, 0.0f, 0.6f);
        break;
      case CLIMATE_RIVER_BANK:
        r.color_map = r.color_dirt;
        break;
      case CLIMATE_FIELD:
        r.color_map = r.color_grass + glRgba (0.7f, 0.5f, 0.6f);
        r.color_map.Normalize ();
        break;
      case CLIMATE_PLAINS:
        r.color_map = r.color_grass + glRgba (0.5f, 0.5f, 0.5f);
        r.color_map.Normalize ();
        break;
      case CLIMATE_FOREST:
        r.color_map = r.color_grass + glRgba (0.0f, 0.3f, 0.0f);
        r.color_map *= 0.5f;
        break;
      case CLIMATE_SWAMP:
        r.color_grass *= 0.5f;
        r.color_map = r.color_grass * 0.5f;
        break;
      case CLIMATE_ROCKY:
        r.color_map = r.color_grass * 0.8f;
        r.color_map += r.color_rock * 0.2f;
        r.color_map.Normalize ();
        break;
      case CLIMATE_CANYON:
        r.color_map = r.color_rock * 0.3f;
        break;
      default:
        r.color_map = r.color_grass;
        break;
      }
      if (r.geo_scale >= 0.0f)
        r.color_map *= (r.geo_scale * 0.5f + 0.5f);
      //if (r.geo_scale >= 0.0f)
        //r.color_map = glRgbaUnique (r.tree_type);
      WorldRegionSet (x, y, r);
    }
  }
  
}

//Blur the region attributes by averaging each region with its
//neighbors.  This prevents overly harsh transitions.
void TerraformAverage ()
{

  int       x, y, xx, yy, count;
  int       radius;
  Region    r;
  float     (*temp)[WORLD_GRID];
  float     (*moist)[WORLD_GRID];
  float     (*elev)[WORLD_GRID];
  float     (*sm)[WORLD_GRID];
  float     (*bias)[WORLD_GRID];

  temp = new float[WORLD_GRID][WORLD_GRID];
  moist = new float[WORLD_GRID][WORLD_GRID];
  elev = new float[WORLD_GRID][WORLD_GRID];
  sm = new float[WORLD_GRID][WORLD_GRID];
  bias = new float[WORLD_GRID][WORLD_GRID];

  //Blur some of the attributes
  for (int passes = 0; passes < 2; passes++) {

    radius = 2;
    for (x = radius; x < WORLD_GRID - radius; x++) {
      for (y = radius; y < WORLD_GRID - radius; y++) {
        temp[x][y] = 0;
        moist[x][y] = 0;
        elev[x][y] = 0;
        sm[x][y] = 0;
        bias[x][y] = 0;
        count = 0;
        for (xx = -radius; xx <= radius; xx++) {
          for (yy = -radius; yy <= radius; yy++) {
            r = WorldRegionGet (x + xx, y + yy);
            temp[x][y] += r.temperature;
            moist[x][y] += r.moisture;
            elev[x][y] += r.geo_water;
            sm[x][y] += r.geo_detail;
            bias[x][y] += r.geo_bias;
            count++;
          }
        }
        temp[x][y] /= (float)count;
        moist[x][y] /= (float)count;
        elev[x][y] /= (float)count;
        sm[x][y] /= (float)count;
        bias[x][y] /= (float)count;
      }
    }
    //Put the blurred values back into our table
    for (x = radius; x < WORLD_GRID - radius; x++) {
      for (y = radius; y < WORLD_GRID - radius; y++) {
        r = WorldRegionGet (x, y);
        //Rivers can get wetter through this process, but not drier.
        if (r.climate == CLIMATE_RIVER) 
          r.moisture = max (r.moisture, moist[x][y]);
        else if (r.climate != CLIMATE_OCEAN) 
          r.moisture = moist[x][y];//No matter how arid it is, the OCEANS STAY WET!
        if (!(r.flags_shape & REGION_FLAG_NOBLEND)) {
          r.geo_detail = sm[x][y];
          r.geo_bias = bias[x][y];
        }
        WorldRegionSet (x, y, r);
      }
    }
  }
  delete []temp;
  delete []moist;
  delete []elev;
  delete []sm;
  delete []bias;
  
}

//Indentify regions where geo_scale is negative.  These will be ocean.
void TerraformOceans ()
{

  int     x, y;
  Region  r;
  bool    is_ocean;
  
  //define the oceans at the edge of the world
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      r = WorldRegionGet (x, y);
      is_ocean = false;
      if (r.geo_scale <= 0.0f) 
        is_ocean = true;
      if (x == 0 || y == 0 || x == WORLD_GRID - 1 || y == WORLD_GRID - 1) 
        is_ocean = true;
      if (is_ocean) {
        r.geo_bias = -10.0f;
        r.geo_detail = 0.3f;
        r.moisture = 1.0f;
        r.geo_water = 0.0f;
        r.flags_shape = REGION_FLAG_NOBLEND;
        r.climate = CLIMATE_OCEAN;
        sprintf (r.title, "%s Ocean", get_direction_name (x, y));
        WorldRegionSet (x, y, r);
      }        
    }
  }

}

//Find existing ocean regions and place costal regions beside them.
void TerraformCoast ()
{

  int             x, y;
  Region          r;
  int             pass;
  unsigned        i;
  unsigned        cliff_grid;
  bool            is_coast;
  bool            is_cliff;
  vector<GLcoord> queue;
  GLcoord         current;

  cliff_grid = WORLD_GRID / 8;
  //now define the coast 
  for (pass = 0; pass < 2; pass++) {
    queue.clear ();
    for (x = 0; x < WORLD_GRID; x++) {
      for (y = 0; y < WORLD_GRID; y++) {
        r = WorldRegionGet (x, y);
        //Skip already assigned places
        if (r.climate != CLIMATE_INVALID)
          continue;
        is_coast = false;
        //On the first pass, we add beach adjoining the sea
        if (!pass && is_climate_present (x, y, 1, CLIMATE_OCEAN)) 
          is_coast = true;
        //One the second pass, we add beach adjoining the beach we added on the previous step
        if (pass && is_climate_present (x, y, 1, CLIMATE_COAST)) 
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
      r = WorldRegionGet (current.x, current.y);
      is_cliff = (((current.x / cliff_grid) + (current.y / cliff_grid)) % 2) != 0;
      if (!pass) 
        sprintf (r.title, "%s beach", get_direction_name (current.x, current.y));
      else
        sprintf (r.title, "%s coast", get_direction_name (current.x, current.y));
      //beaches are low and partially submerged
      if (!pass) {
        r.geo_bias = -1.0f;
        if (is_cliff)
          r.flags_shape |= REGION_FLAG_BEACH_CLIFF;
        else
          r.flags_shape |= REGION_FLAG_BEACH;
      } else 
        r.geo_bias = 0.0f;
      r.geo_detail = 5.0f + Entropy (current.x, current.y) * 10.0f;
      r.cliff_threshold = r.geo_detail * 0.5f;
      r.moisture = 1.0f;
      r.geo_water = 0.0f;
      r.flags_shape |= REGION_FLAG_NOBLEND;
      r.climate = CLIMATE_COAST;
      WorldRegionSet (current.x, current.y, r);
    }
  }


}

//Drop a point in the middle of the terrain and attempt to
//place a river. 
void TerraformRivers (int count)
{

  int         rivers;
  int         cycles;
  int         x, y;
  int         range;

  rivers = 0;
  cycles = 0;
  range = WORLD_GRID_CENTER / 3;
  while (rivers < count && cycles < 100) {
    x = WORLD_GRID_CENTER + (RandomVal () % range) - range / 2;
    y = WORLD_GRID_CENTER + (RandomVal () % range) - range / 2;
    if (try_river (x, y, rivers)) 
      rivers++;
    cycles++;
  }

}

//Search around for places to put lakes
void TerraformLakes (int count)
{

  int         lakes;
  int         cycles;
  int         x, y;
  int         range;
  GLcoord     shove;

  lakes = 0;
  cycles = 0;
  range = WORLD_GRID_CENTER / 4;
  while (lakes < count && cycles < 100) {
    //Pick a random spot in the middle of the map
    x = WORLD_GRID_CENTER + (WorldNoisei (cycles) % range) - range / 2;
    y = WORLD_GRID_CENTER + (WorldNoisei (cycles * 2) % range) - range / 2;
    //Now push that point away from the middle
    shove = get_map_side (x, y);
    shove *= range;
    if (try_lake (x + shove.x, y + shove.y, lakes)) 
      lakes++;
    cycles++;
  }

}

//Create zones of different climates.
void TerraformZones ()
{

  int             x, y;
  vector<Climate> climates;
  Region          r;
  int             radius;
  Climate         c;
  GLcoord         walk;

  walk.Clear ();
  do {
    x = walk.x;
    y = walk.y;// + WorldNoisei (walk.x + walk.y * WORLD_GRID) % 4;
    radius = 2 + WorldNoisei (10 + walk.x + walk.y * WORLD_GRID) % 9;
    if (is_free (x, y, radius)) {
      r = WorldRegionGet (x, y);
      climates.clear ();
      //swamps only appear in wet areas that aren't cold.
      if (r.moisture > 0.9f && r.temperature > TEMP_TEMPERATE)
        climates.push_back (CLIMATE_SWAMP);
      //mountains only appear in the middle
      if (abs (x - WORLD_GRID_CENTER) < 10 && radius > 1)
        climates.push_back (CLIMATE_MOUNTAIN);
      //Deserts are HOT and DRY. Duh.
      if (r.temperature > TEMP_HOT && r.moisture < 0.05f && radius > 1)
        climates.push_back (CLIMATE_DESERT);
      //fields should be not too hot or cold.
      if (r.temperature > TEMP_TEMPERATE && r.temperature < TEMP_HOT && r.moisture > 0.5f && radius == 1)
        climates.push_back (CLIMATE_FIELD);
      if (r.temperature > TEMP_TEMPERATE && r.temperature < TEMP_HOT && r.moisture > 0.25f && radius > 1)
        climates.push_back (CLIMATE_PLAINS);
      //Rocky wastelands favor cold areas
      if (r.temperature < TEMP_TEMPERATE)
        climates.push_back (CLIMATE_ROCKY);
      if (radius > 1)
        climates.push_back (CLIMATE_CANYON);
      if (r.temperature > TEMP_TEMPERATE && r.temperature < TEMP_HOT && r.moisture > 0.5f)
        climates.push_back (CLIMATE_FOREST);
      if (climates.empty ()) {
        walk.Walk (WORLD_GRID);
        continue;
      }              
      c = climates[RandomVal () % climates.size ()];
      switch (c) {
      case CLIMATE_ROCKY:
        do_rocky (x, y, radius);
        break;
      case CLIMATE_MOUNTAIN:
        do_mountain (x, y, radius);
        break;
      case CLIMATE_CANYON:
        do_canyon (x, y, radius);
        break;
      case CLIMATE_SWAMP:
        do_swamp (x, y, radius);
        break;
      case CLIMATE_FIELD:
        do_field (x, y, radius);
        break;
      case CLIMATE_DESERT:
        do_desert (x, y, radius);
        break;
      case CLIMATE_PLAINS:
        do_plains (x, y, radius);
        break;
      case CLIMATE_FOREST:
        do_forest (x, y, radius);
        break;
      }
    }
  } while (!walk.Walk (WORLD_GRID));

}


//This will fill in all previously un-assigned regions.
void TerraformFill ()
{

  int       x, y;
  Region    r;
  unsigned  rand;

  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      r = WorldRegionGet (x, y);
      //See if this is already ocean
      if (r.climate != CLIMATE_INVALID)
        continue;
      sprintf (r.title, "???");
      r.geo_water = r.geo_scale * 10.0f;
      r.geo_detail = 20.0f;
      //Have them trend more hilly in dry areas
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
      } else {
        sprintf (r.title, "Grasslands");
        //r.geo_detail /= 3;
        //r.geo_large /= 3;
      }  
      WorldRegionSet (x, y, r);
    }
  }

}

void TerraformPrepare () 
{


  int         x, y;
  Region      r;
  GLcoord     from_center;
  GLcoord     offset;

  //Set some defaults
  offset.x = RandomVal () % 1024;
  offset.y = RandomVal () % 1024;
  for (x = 0; x < WORLD_GRID; x++) {
    for (y = 0; y < WORLD_GRID; y++) {
      memset (&r, 0, sizeof (Region));
      sprintf (r.title, "NOTHING");
      r.geo_bias = r.geo_detail = 0;
      r.mountain_height = 0;
      r.grid_pos.x = x;
      r.grid_pos.y = y;
      r.tree_threshold = 0.15f;
      from_center.x = abs (x - WORLD_GRID_CENTER);
      from_center.y = abs (y - WORLD_GRID_CENTER);
      //Geo scale is a number from -1 to 1. -1 is lowest ovean. 0 is sea level. 
      //+1 is highest elevation on the island. This is used to guide other derived numbers.
      r.geo_scale = glVectorLength (glVector ((float)from_center.x, (float)from_center.y));
      r.geo_scale /= (WORLD_GRID_CENTER - OCEAN_BUFFER);
      //Create a steep drop around the edge of the world
      if (r.geo_scale > 1.0f)
        r.geo_scale = 1.0f + (r.geo_scale - 1.0f) * 2.0f;
      r.geo_scale = 1.0f - r.geo_scale;
      r.geo_scale += (Entropy ((x + offset.x), (y + offset.y)) - 0.5f);
      r.geo_scale += (Entropy ((x + offset.x) * FREQUENCY, (y + offset.y) * FREQUENCY) - 0.2f);
      r.geo_scale = clamp (r.geo_scale, -1.0f, 1.0f);
      if (r.geo_scale > 0.0f)
        r.geo_water = 1.0f + r.geo_scale * 16.0f;
      r.geo_bias = 0.0f;
      r.geo_detail = 0.0f;
      r.color_map = glRgba (0.0f);
      r.climate = CLIMATE_INVALID;
      WorldRegionSet (x, y, r);
    }
  }



}