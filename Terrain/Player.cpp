/*-----------------------------------------------------------------------------

  Player.cpp

-------------------------------------------------------------------------------

  This handles the character stats. Hitpoints, energy, etc.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "game.h"
#include "text.h"


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
  float           calorie_pool;
  GLvector        position;
};

static Player     my;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void PlayerInit ()
{

  my.gender = CVarUtils::CreateCVar ("player.gender", 0, "");
  //CVarUtils::CreateCVar ("player.last_time", 0, "");
  CVarUtils::AttachCVar ("player.last_time", &my.last_time, "");
  CVarUtils::AttachCVar ("player.distance_traveled", &my.distance_traveled, "");
  CVarUtils::AttachCVar ("player.calories_burned", &my.calories_burned, "");
  CVarUtils::AttachCVar ("player.calorie_pool", &my.calorie_pool, "");
  CVarUtils::AttachCVar ("player.position_x", &my.position.x, "");
  CVarUtils::AttachCVar ("player.position_y", &my.position.y, "");
  CVarUtils::AttachCVar ("player.position_z", &my.position.z, "");
  //CVarUtils::CreateCVar ("player.position", &my.calorie_pool, "");

}


void PlayerReset ()
{

  my.distance_traveled = 0.0f;
  my.calories_burned = 0.0f;
  my.calorie_pool = 2000.0f;
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

  if (!GameRunning ())
    return;
  anim = AvatarAnim ();
  time_passed = GameTime () - my.last_time;
  my.last_time = GameTime ();
  my.calories_burned += burn_rate[anim] * time_passed;
  av_pos = AvatarPosition ();
  movement_delta = my.position - av_pos;
  movement_delta.z = 0.0f;
  my.position = av_pos;
  my.distance_traveled += movement_delta.Length ();
  if (CVarUtils::GetCVar<bool> ("show.vitals")) {
    TextPrint ("Calories burned: %1.2f", my.calories_burned);
    TextPrint ("Walked %1.2fkm", my.distance_traveled / 1000.0f);
  }


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