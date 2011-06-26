/*-----------------------------------------------------------------------------

  Avatar.cpp


-------------------------------------------------------------------------------

  Handles movement and player input.
 
-----------------------------------------------------------------------------*/


#include "stdafx.h"
#include "cache.h"
#include "camera.h"
#include "ini.h"
#include "input.h"
#include "sdl.h"
#include "Text.h"
#include "world.h"

#define JUMP_SPEED      4.0f
#define MOVE_SPEED      6.0f
#define EYE_HEIGHT      1.8f

static GLvector         angle;
static GLvector         position;
static bool             fly;
static bool             on_ground;
static unsigned         last_update;
static Region           region;
static float            velocity;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_move (GLvector delta)		
{

  GLvector    movement;
  float       vert_delta;
  float       forward;
  
  if (fly) 
    forward = sin (angle.x * DEGREES_TO_RADIANS);
  else
    forward = 1.0f;
  vert_delta = cos (angle.x * DEGREES_TO_RADIANS) * delta.y;
  movement.x = cos (angle.z * DEGREES_TO_RADIANS) * delta.x +  sin (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
  movement.y = -sin (angle.z * DEGREES_TO_RADIANS) * delta.x +  cos (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
  movement.z = vert_delta;
  position += movement;
  

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void AvatarUpdate (void)
{

  float     e;
  float     move;
  float     elapsed;
  char*     direction;
  GLvector  cam;

  elapsed = SdlElapsedSeconds ();
  if (InputKeyPressed (SDLK_F2))
    fly = !fly;
  if (InputKeyPressed (SDLK_SPACE)) {
    if (on_ground)
      velocity = JUMP_SPEED;
  }
  move = elapsed * MOVE_SPEED;
  if (InputMouselook ()) {
    if (fly) {
      velocity = 0.0f;
      //move *= 15;
    } else {
      position.z += velocity * elapsed;
      velocity -= elapsed * GRAVITY;
    }
    if (InputKeyState (SDLK_LSHIFT)) {
      if (!fly)
        move *= 5;
      else 
        move *= 25;
    }
    if (InputKeyState (SDLK_w))
      do_move (glVector (0, -move, 0));
    if (InputKeyState (SDLK_s))
      do_move (glVector (0, move, 0));
    if (InputKeyState (SDLK_a))
      do_move (glVector (-move, 0, 0));
    if (InputKeyState (SDLK_d))
      do_move (glVector (move, 0, 0));
  }
  e = CacheElevation (position.x, position.y);
  if (!fly) {
    if (position.z <= e) {
      on_ground = true;
      position.z = e;
    } else
      on_ground = false;
  }
  region = WorldRegionGet ((int)(position.x + REGION_HALF) / REGION_SIZE, (int)(position.y + REGION_HALF) / REGION_SIZE);
  direction = "North";
  if (angle.z < 22.5f)
    direction = "North";
  else if (angle.z < 67.5f)
    direction = "Northwest";
  else if (angle.z < 112.5f)
    direction = "West";
  else if (angle.z < 157.5f)
    direction = "Southwest";
  else if (angle.z < 202.5f)
    direction = "South";
  else if (angle.z < 247.5f)
    direction = "Southeast";
  else if (angle.z < 292.5f)
    direction = "East";
  else if (angle.z < 337.5f)
    direction = "Northeast";
  
  //TextPrint ("%s @%1.2f Y:%1.2f Z:%1.2f - Facing %s", region.title, position.x, position.y, position.z, direction);
  TextPrint ("%s @%s - Facing %s", region.title, WorldLocationName (region.grid_pos.x, region.grid_pos.y), direction);
  TextPrint ("Temp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Water Level: %1.2f Topography Detail:%1.2f Topography Bias:%1.2f", 
    region.temperature * 100.0f, 186, region.moisture * 100.0f, region.geo_scale, region.geo_water, region.geo_detail, region.geo_bias);
  
  //Cell c = WorldCell ((int)position.x, (int)position.y);
  //TextPrint ("%f %f", c.elevation, c.water_level);

  /*
  TextPrint ("%s\nTemp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Elevation Bias: %1.2f Topography Detail:%1.2f Topography Large:%1.2f", 
    region.title, region.temperature * 100.0f, 186, region.moisture * 100.0f, region.elevation * 100.0f, region.geo_water, region.geo_detail, region.geo_large);
    */
  cam = position;
  cam.z += EYE_HEIGHT;
  CameraAngleSet (angle);
  CameraPositionSet (cam);

}


void AvatarInit (void)		
{

  angle = IniVector ("AvatarAngle");
  position = IniVector ("AvatarPosition");
  fly = IniInt ("AvatarFlying") != 0;

}

void AvatarTerm (void)		
{

  //just store our most recent position in the ini
  IniVectorSet ("AvatarAngle", angle);
  IniVectorSet ("AvatarPosition", position);
  IniIntSet ("AvatarFlying", fly ? 1 : 0);
 
}

void AvatarLook (int x, int y)
{

  angle.x += x;
  angle.z += y;
  angle.x = clamp (angle.x, 0.0f, 180.0f);
  angle.z = fmod (angle.z, 360.0f);
  if (angle.z < 0.0f)
    angle.z += 360.0f;


}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector AvatarPosition ()
{

  return position;

}

void AvatarPositionSet (GLvector new_pos)		
{

  new_pos.z = clamp (new_pos.z, -25, 2048);
  new_pos.x = clamp (new_pos.x, 0, (REGION_SIZE * WORLD_GRID));
  new_pos.y = clamp (new_pos.y, 0, (REGION_SIZE * WORLD_GRID));
  position = new_pos;

}
