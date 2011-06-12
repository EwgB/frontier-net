/*-----------------------------------------------------------------------------

  Env.cpp

-------------------------------------------------------------------------------

  The environment. Lighting, fog, and so on.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "region.h"
#include "camera.h"
#include "env.h"
#include "input.h"
#include "math.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"

#define MAX_DISTANCE        400
#define ENV_TRANSITION      0.05f
#define UPDATE_INTERVAL     200 //milliseconds

static Env        desired;
static Env        current;
static long       seconds;
static int        minutes;
static int        hours;
static int        update;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_time (float delta)
{

  Region*   r;
  bool      day;

  r = (Region*)CameraRegion ();
  day = (hours >= 6 && hours < 21);
  if (day) {
    switch (hours) {
    case 6:
      desired.color[ENV_COLOR_NORTH] = glRgba (0.0f, 0.2f, 0.5f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.0f, 0.2f, 0.5f);
      desired.color[ENV_COLOR_EAST] = glRgba (0.5f, 0.5f, 0.3f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.0f, 0.0f, 0.4f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.0f, 0.0f, 0.2f);
      desired.color[ENV_COLOR_FOG] = glRgba (0.0f, 0.2f, 0.5f);
      desired.color[ENV_COLOR_LIGHT] = glRgba (0.0f, 0.5f, 1.0f);
      desired.color[ENV_COLOR_AMBIENT] = glRgba (0.0f, 0.0f, 1.0f);
      desired.light = glVector (-0.9f, 0.0f, -0.1f);
      desired.star_fade = 0.5f;
      desired.fog_max = MAX_DISTANCE / 4;
      desired.fog_min = desired.fog_max / 2;
      break;
    case 7:
      desired.color[ENV_COLOR_EAST] = glRgba (1.0f, 1.0f, 0.5f);
      desired.color[ENV_COLOR_NORTH] = glRgba (0.0f, 0.3f, 0.7f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.0f, 0.3f, 0.7f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.0f, 0.2f, 0.5f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.0f, 0.0f, 0.2f);
      desired.color[ENV_COLOR_FOG] = glRgba (0.0f, 0.3f, 0.7f);
      desired.color[ENV_COLOR_LIGHT] = glRgba (0.0f, 0.5f, 1.0f);
      desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 0.7f);
      desired.light = glVector (-0.5f, 0.0f, -0.5f);
      desired.star_fade = 0.0f;
      desired.fog_max = MAX_DISTANCE / 3;
      desired.fog_min = desired.fog_max / 2;
      break;
    case 8:
    case 9:
    case 10:
    case 11:
    case 12:
    case 13:
    case 14:
    case 15:
    case 16:
    case 17:
    case 18:
      desired.color[ENV_COLOR_NORTH] = glRgba (0.5f, 0.9f, 1.0f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.5f, 0.9f, 1.0f);
      desired.color[ENV_COLOR_EAST] = glRgba (0.5f, 0.9f, 1.0f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.5f, 0.7f, 1.0f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.0f, 0.0f, 1.0f);
      desired.color[ENV_COLOR_FOG] = glRgba (0.5f, 0.9f, 1.0f);
      desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.5f);
      desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
      desired.light = glVector (-0.5f, 0.0f, -0.5f);
      desired.star_fade = 0.0f;
      desired.fog_max = MAX_DISTANCE;
      desired.fog_min = MAX_DISTANCE -  MAX_DISTANCE * r->moisture;
      if (r->has_flowers) {
        desired.color[ENV_COLOR_NORTH] = glRgba (1.0f, 1.0f, 0.8f);
        desired.color[ENV_COLOR_SOUTH] = glRgba (1.0f, 1.0f, 0.8f);
        desired.color[ENV_COLOR_EAST] = glRgba (1.0f, 1.0f, 0.8f);
        desired.color[ENV_COLOR_WEST] = glRgba (1.0f, 1.0f, 0.8f);
        desired.color[ENV_COLOR_TOP] = glRgba (1.0f, 0.7f, 0.2f);
        desired.color[ENV_COLOR_FOG] = glRgba (1.0f, 1.0f, 0.8f);
        desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.0f);
        desired.color[ENV_COLOR_AMBIENT] = glRgba (0.0f, 0.0f, 1.0f);
        desired.fog_min = 1;
      }
      break;
    case 19:
      desired.color[ENV_COLOR_NORTH] = glRgba (0.1f, 0.5f, 0.5f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.1f, 0.5f, 0.5f);
      desired.color[ENV_COLOR_EAST] = glRgba (0.1f, 0.2f, 0.5f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.8f, 0.7f, 0.4f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.4f, 0.4f, 0.0f);
      desired.color[ENV_COLOR_FOG] = glRgba (0.5f, 0.9f, 1.0f);
      desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.0f);
      desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
      desired.light = glVector (-0.5f, 0.0f, -0.5f);
      desired.star_fade = 0.0f;
      desired.fog_max = MAX_DISTANCE;
      desired.fog_min = MAX_DISTANCE -  MAX_DISTANCE * r->moisture;
      break;
    case 20:
      desired.color[ENV_COLOR_NORTH] = glRgba (0.0f, 0.0f, 0.5f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.0f, 0.0f, 0.5f);
      desired.color[ENV_COLOR_EAST] = glRgba (0.0f, 0.0f, 0.2f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.8f, 0.5f, 0.2f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.5, 0.5f, 0.0f);
      desired.color[ENV_COLOR_FOG] = glRgba (0.5f, 0.5f, 0.0f);
      desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.0f);
      desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
      desired.light = glVector (-0.5f, 0.0f, -0.5f);
      desired.star_fade = 0.0f;
      desired.fog_max = MAX_DISTANCE;
      desired.fog_min = MAX_DISTANCE * 0.7f * r->moisture;
      break;
    default:
      desired.color[ENV_COLOR_NORTH] = glRgba (0.5f, 0.7f, 1.0f);
      desired.color[ENV_COLOR_SOUTH] = glRgba (0.5f, 0.7f, 1.0f);
      desired.color[ENV_COLOR_EAST] = glRgba (0.5f, 0.7f, 1.0f);
      desired.color[ENV_COLOR_WEST] = glRgba (0.5f, 0.7f, 1.0f);
      desired.color[ENV_COLOR_TOP] = glRgba (0.1f, 0.2f, 1.0f);
      desired.color[ENV_COLOR_FOG] = glRgba (1.0f);
      desired.light = glVector (-0.5f, 0.0f, -0.5f);
      desired.star_fade = 0.0f;
      desired.fog_max = MAX_DISTANCE;
      desired.fog_min = desired.fog_max / 2;
      break;
    }
  } else { //night
    desired.color[ENV_COLOR_NORTH] = glRgba (0.0f, 0.0f, 0.2f);
    desired.color[ENV_COLOR_SOUTH] = glRgba (0.0f, 0.0f, 0.2f);
    desired.color[ENV_COLOR_EAST] = glRgba (0.0f, 0.0f, 0.2f);
    desired.color[ENV_COLOR_WEST] = glRgba (0.0f, 0.0f, 0.2f);
    desired.color[ENV_COLOR_TOP] = glRgba (0.0f);
    desired.color[ENV_COLOR_FOG] = glRgba (0.0f, 0.0f, 0.1f);
    desired.color[ENV_COLOR_LIGHT] = glRgba (0.3f, 0.6f, 1.0f);
    desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
    desired.light = glVector (0.0f, -0.5f, -0.5f);
    desired.fog_min = 1;
    desired.fog_max = MAX_DISTANCE / 5;
    desired.star_fade = 1.0f;
  }
  for (int i = 0; i < ENV_COLOR_COUNT; i++) 
    current.color[i] = glRgbaInterpolate (current.color[i], desired.color[i], delta);
  current.fog_min = MathInterpolate (current.fog_min, desired.fog_min, delta);
  current.fog_max = MathInterpolate (current.fog_max, desired.fog_max, delta);
  current.star_fade = MathInterpolate (current.star_fade, desired.star_fade, delta);
  current.light = glVectorInterpolate (current.light, desired.light, delta);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void    EnvInit ()
{

  hours = 10;
  do_time (1);
  current = desired;

}

void    EnvUpdate ()
{

  //seconds += SdlElapsed ();
  if (InputKeyPressed (SDLK_RIGHTBRACKET))
    hours++;
  if (InputKeyPressed (SDLK_LEFTBRACKET)) {
    hours--;
    if (hours < 0)
      hours += 24;
  }
  if (seconds >= 250) {
    seconds -= 250;
    minutes++;
  }
  if (minutes >= 60) {
    minutes -= 60;
    hours++;
  }
  if (hours >= 24)
    hours -= 24;
    

  update += SdlElapsed ();
  if (update > UPDATE_INTERVAL) {
    do_time (ENV_TRANSITION);
    update -= UPDATE_INTERVAL;
  }
  TextPrint ("Time: %02d:%02d", hours, minutes);
  
}

Env* EnvGet ()
{

  return &current;

}
