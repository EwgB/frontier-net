/*-----------------------------------------------------------------------------

  Camera.cpp

  2009 Shamus Young

-------------------------------------------------------------------------------

  This tracks the position and oritentation of the camera. In screensaver 
  mode, it moves the camera around the world in order to create dramatic 
  views of the hot zone.  

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ini.h"
#include "input.h"
#include "Region.h"
#include "sdl.h"
#include "Text.h"
#include "world.h"

#define MOVE_SPEED                4.0f

static GLvector     angle;
static GLvector     position;
static unsigned     last_update;
static float        velocity;
static bool         fly;
static bool         on_ground;
static Region       region;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraYaw (float delta)
{

  angle.y -= delta;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraPitch (float delta)
{

  angle.x -= delta;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector CameraPosition (void)		
{
 
  return position;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraReset ()		
{

  position.y = 20.0f;
  position.x = 20;
  position.z = 10;
  angle.x = 0.0f;
  angle.y = 0.0f;
  angle.z = 0.0f;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraPositionSet (GLvector new_pos)		
{

  new_pos.z = CLAMP (new_pos.z, -25, 1024);
  new_pos.x = CLAMP (new_pos.x, -512, (REGION_SIZE * REGION_GRID));
  new_pos.y = CLAMP (new_pos.y, -512, (REGION_SIZE * REGION_GRID));
  position = new_pos;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraMove (GLvector delta)		
{

  GLvector    movement;
  GLvector    new_pos;
  float       vert_delta;
  float       forward;
  
  if (fly) 
    forward = sin (angle.x * DEGREES_TO_RADIANS);
  else
    forward = 1.0f;
  vert_delta = cos (angle.x * DEGREES_TO_RADIANS) * delta.y;
  movement.x = cos (angle.z * DEGREES_TO_RADIANS) * delta.x +  sin (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
  movement.y = -sin (angle.z * DEGREES_TO_RADIANS) * delta.x +  cos (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
  //movement.x = 0;
  movement.z = vert_delta;
  /*
  if (fly) {
    horz_delta = sin (angle.x * DEGREES_TO_RADIANS);
    movement.x *= horz_delta;
    movement.y *= horz_delta;
  }
  */
  new_pos = movement + position;
  CameraPositionSet (new_pos);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector CameraAngle (void)		
{

  return angle;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraAngleSet (GLvector new_angle)		
{

  angle = new_angle;
  angle.x = CLAMP (angle.x, 0.0f, 180.0f);
  angle.z = fmod (angle.z, 360.0f);
  if (angle.z < 0.0f)
    angle.z += 360.0f;
  //angle.z = CLAMP (angle.z, -60.0f, 60.0f);

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void* CameraRegion ()
{

  return (void*)&region;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraInit (void)		
{

  angle = IniVector ("CameraAngle");
  position = IniVector ("CameraPosition");
  fly = true;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraUpdate (void)		
{

  float     e;
  float     move;
  float     elapsed;
  char*     direction;

  elapsed = SdlElapsedSeconds ();
  if (InputKeyPressed (SDLK_F2))
    fly = !fly;
  if (InputKeyPressed (SDLK_SPACE)) {
    if (on_ground)
      velocity = 3;
  }
  move = elapsed * MOVE_SPEED;
  if (InputMouselook ()) {
    if (fly) {
      velocity = 0.0f;
      move *= 15;
    } else {
      position.z += velocity * elapsed;
      velocity -= elapsed * GRAVITY;
    }
    if (InputKeyState (SDLK_LSHIFT))
      move *= 5;
    if (InputKeyState (SDLK_w))
      CameraMove (glVector (0, -move, 0));
    if (InputKeyState (SDLK_s))
      CameraMove (glVector (0, move, 0));
    if (InputKeyState (SDLK_a))
      CameraMove (glVector (-move, 0, 0));
    if (InputKeyState (SDLK_d))
      CameraMove (glVector (move, 0, 0));
  }
  e = WorldElevation (position.x, position.y) + 1.6f;
  if (!fly) {
    if (position.z <= e) {
      on_ground = true;
      position.z = e;
    } else
      on_ground = false;
  }
  region = RegionGet (position.x, position.y);
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
  TextPrint ("%s @%1.2f Y:%1.2f Z:%1.2f - Facing %s", region.title, position.x, position.y, position.z, direction);
  TextPrint ("Temp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Elevation Bias: %1.2f Topography Detail:%1.2f Topography Large:%1.2f", 
    region.temperature * 100.0f, 186, region.moisture * 100.0f, region.geo_scale, region.geo_bias, region.geo_detail, region.geo_large);

  /*
  TextPrint ("%s\nTemp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Elevation Bias: %1.2f Topography Detail:%1.2f Topography Large:%1.2f", 
    region.title, region.temperature * 100.0f, 186, region.moisture * 100.0f, region.elevation * 100.0f, region.geo_bias, region.geo_detail, region.geo_large);
    */
}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraTerm (void)		
{

  //just store our most recent position in the ini
  IniVectorSet ("CameraAngle", angle);
  IniVectorSet ("CameraPosition", position);
 
}
