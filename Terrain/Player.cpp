/*-----------------------------------------------------------------------------

  Player.cpp

-------------------------------------------------------------------------------

  This handles the character stats. Hitpoints, energy, etc.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "game.h"
#include "input.h"
#include "text.h"
#include "world.h"

#define MAX_STOMACH         4000
#define MAX_POOL            3000
#define ONE_POUND           3000 //one pound of fat = about 3,000 calories
#define CALORIE_ABSORB_RATE 250  //How many calories the stomach can absorb per hour

enum
{
  HUNGER_FULL,
  HUNGER_SATISFIED,
  HUNGER_SLIGHT,
  HUNGER_HUNGRY,
  HUNGER_RAVENOUS,
  HUNGER_WEAK,
  HUNGER_STARVING
};

static char*  hunger_states [] =
{
  "Stuffed",
  "Satisfied",
  "Peckish",
  "Hungry",
  "Ravenous",
  "Weak",
  "Starving"
};


static float    burn_rate[] = 
{
  56,     //stand
  200,    //run, which we treat as "walking" for gameplay purposes
  450,    //sprint
  0,      //flying
  50,     //falling
  200,    //jumping
  450,    //swimming
  400,    //treading water
};

struct Player
{
  UCHAR           gender;
  float           last_time;
  float           distance_traveled;
  float           calories_burned;
  float           calorie_stomach;
  float           calorie_pool;
  float           calorie_fat;
  int             condition_hunger;
  GLvector        position;
};

static Player     my;
static Region     region;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void PlayerInit ()
{

  my.gender = CVarUtils::CreateCVar ("player.gender", 0, "");
  //CVarUtils::CreateCVar ("player.last_time", 0, "");
  CVarUtils::AttachCVar ("player.last_time", &my.last_time, "");
  CVarUtils::AttachCVar ("player.distance_traveled", &my.distance_traveled, "");
  
  CVarUtils::AttachCVar ("player.calories_burned",  &my.calories_burned, "");
  CVarUtils::AttachCVar ("player.calorie_stomach",  &my.calorie_stomach, "");
  CVarUtils::AttachCVar ("player.calorie_pool",     &my.calorie_pool, "");
  CVarUtils::AttachCVar ("player.calorie_fat",      &my.calorie_fat, "");

  CVarUtils::AttachCVar ("player.position_x", &my.position.x, "");
  CVarUtils::AttachCVar ("player.position_y", &my.position.y, "");
  CVarUtils::AttachCVar ("player.position_z", &my.position.z, "");
  //CVarUtils::CreateCVar ("player.position", &my.calorie_pool, "");

}


void PlayerReset ()
{

  my.distance_traveled = 0.0f;
  my.calories_burned = 0.0f;
  my.calorie_stomach = MAX_STOMACH / 2;
  my.calorie_pool = MAX_POOL / 2;
  my.calorie_fat = ONE_POUND * 5;
  my.gender = 0;
  my.last_time = GameTime ();
  my.position = glVector (0.0f, 0.0f, 0.0f);

}

void PlayerLoad ()
{

  string            filename;
  vector<string>    sub_group;

  filename = GameDirectory ();
  filename += "player.sav";
  sub_group.push_back ("player");
  CVarUtils::Load (filename, sub_group);
  AvatarPositionSet (my.position);


}

void PlayerSave ()
{
  /*
  string            filename;
  vector<string>    sub_group;

  filename = GameDirectory ();
  filename += "player.sav";
  //CVarUtils::SetCVar ("player.gender", my.gender);
  //CVarUtils::SetCVar ("player.last_time", my.last_time);
  //CVarUtils::SetCVar ("player.distance_traveled", my.distance_traveled);
  //CVarUtils::SetCVar ("player.calories_burned", my.calories_burned);
  //CVarUtils::SetCVar ("player.calorie_pool", my.calorie_pool);
  sub_group.push_back ("player");
  CVarUtils::Save (filename, sub_group);
  */
}

void PlayerUpdate ()
{

  AnimType      anim;
  float         time_passed;
  GLvector      av_pos;
  GLvector      movement_delta;
  float         calorie_burn;
  float         calorie_absorb;

  if (!GameRunning ())
    return;
  anim = AvatarAnim ();
  time_passed = GameTime () - my.last_time;
  my.last_time = GameTime ();
  ///////////  Deal with food & hunger  ////////////////////
  if (InputKeyPressed (SDLK_e))
    my.calorie_stomach = min (my.calorie_stomach + 1000.0f, MAX_STOMACH);
  //Food slowly moves from stomach, to pool, to fat
  calorie_absorb = CALORIE_ABSORB_RATE * time_passed;
  calorie_absorb = min (calorie_absorb, my.calorie_stomach);
  my.calorie_stomach -= calorie_absorb;
  //if we're starving, it bypasses the pool, leaving us hungry more often
  if (my.calorie_fat < 0.0f) 
    my.calorie_fat += calorie_absorb;
  else {
    my.calorie_pool += calorie_absorb;
    calorie_absorb = 0.0f;
    if (my.calorie_pool > MAX_POOL) 
      calorie_absorb = my.calorie_pool - MAX_POOL;
    my.calorie_pool = min (my.calorie_pool, MAX_POOL);
    my.calorie_fat += calorie_absorb;
  }
  //Now calculate energy burn
  calorie_burn = burn_rate[anim] * time_passed;
  my.calories_burned += calorie_burn;
  my.calorie_stomach -= calorie_burn;
  //if the stomach is empty, it comes from the pool
  if (my.calorie_stomach < 0.0f) {
    my.calorie_pool += my.calorie_stomach;
    my.calorie_stomach = 0.0f;
  }
  //if we don't have anything in the pool, we start burning body fat
  if (my.calorie_pool < 0.0f) {
    my.calorie_fat += my.calorie_pool;
    my.calorie_pool = 0.0f;
  }
  //determine how hungry we are.
  if (my.calorie_stomach > MAX_STOMACH * 0.9f) 
    my.condition_hunger = HUNGER_FULL;
  else if (my.calorie_stomach > MAX_STOMACH * 0.25f) 
    my.condition_hunger = HUNGER_SATISFIED;
  else if (my.calorie_stomach > 0.0f) 
    my.condition_hunger = HUNGER_SLIGHT;
  else if (my.calorie_pool > MAX_POOL * 0.5f) 
    my.condition_hunger = HUNGER_HUNGRY;
  else if (my.calorie_pool > 0.0f) 
    my.condition_hunger = HUNGER_RAVENOUS;
  else 
    my.condition_hunger = HUNGER_STARVING;


  av_pos = AvatarPosition ();
  movement_delta = my.position - av_pos;
  movement_delta.z = 0.0f;
  my.position = av_pos;
  my.distance_traveled += movement_delta.Length ();
  if (CVarUtils::GetCVar<bool> ("show.vitals")) {
    TextPrint ("Calories burned: %1.2f", my.calories_burned);
    TextPrint ("Walked %1.2fkm", my.distance_traveled / 1000.0f);
    TextPrint ("Hunger: %s", hunger_states[my.condition_hunger]);
    TextPrint ("Calorie Stomach %1.2f", my.calorie_stomach);
    TextPrint ("Calorie Pool %1.2f", my.calorie_pool);
    TextPrint ("Calorie Fat %1.2f (%+1.1f lbs)", my.calorie_fat, my.calorie_fat / ONE_POUND);
    TextPrint ("%1.1f %1.1f %1.1f", my.position.x, my.position.y, my.position.z);
  }
  region = WorldRegionGet ((int)(my.position.x + REGION_HALF) / REGION_SIZE, (int)(my.position.y + REGION_HALF) / REGION_SIZE);
  if (CVarUtils::GetCVar<bool> ("show.region")) 
    TextPrint ("Temp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Water Level: %1.2f Topography Detail:%1.2f Topography Bias:%1.2f", region.temperature * 100.0f, 186, region.moisture * 100.0f, region.geo_scale, region.geo_water, region.geo_detail, region.geo_bias);


}

void PlayerPositionSet (GLvector new_pos)
{

  my.position = new_pos;
  my.last_time = GameTime ();
  AvatarPositionSet (new_pos);

}

GLvector PlayerPositionGet ()
{

  return my.position;

}