/*-----------------------------------------------------------------------------

  Env.cpp

-------------------------------------------------------------------------------

  The environment. Lighting, fog, and so on.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "region.h"
#include "camera.h"
#include "env.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"

#define MAX_DISTANCE        400

static GLrgba     color[ENV_COLOR_COUNT];
static long       seconds;
static int        minutes;
static int        hours;
static GLvector2  fog;
static float      stars;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_time ()
{

  Region*  r;

  r = (Region*)CameraRegion ();
  if (hours < 6 || hours > 22) {
    color[ENV_COLOR_NORTH] = glRgba (0.0f);
    color[ENV_COLOR_SOUTH] = glRgba (0.0f);
    color[ENV_COLOR_EAST] = glRgba (0.0f);
    color[ENV_COLOR_WEST] = glRgba (0.0f);
    color[ENV_COLOR_FOG] = glRgba (0.0f);
    color[ENV_COLOR_LIGHT] = glRgba (0.3f, 0.6f, 1.0f);
    color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
    fog.x = 1.0f;
    fog.y = MAX_DISTANCE / 3;
    stars = 1.0f;
  } else {
    color[ENV_COLOR_NORTH] = glRgba (0.0f);
    color[ENV_COLOR_SOUTH] = glRgba (0.0f);
    color[ENV_COLOR_EAST] = glRgba (0.0f);
    color[ENV_COLOR_WEST] = glRgba (0.0f);
    color[ENV_COLOR_FOG] = glRgba (1.0f);
    color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.5f);
    color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 1.0f);
    fog.x = MAX_DISTANCE * 0.7f * r->moisture;
    fog.y = MAX_DISTANCE;
    stars = 0.0f;
  }

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void    EnvInit ()
{

  hours = 23;


}

void    EnvUpdate ()
{

  seconds += SdlElapsed ();
  if (seconds >= 150) {
    seconds -= 150;
    minutes++;
  }
  if (minutes >= 60) {
    minutes -= 60;
    hours++;
  }
  if (hours >= 24)
    hours -= 24;
  do_time ();
  TextPrint ("Time: %02d:%02d", hours, minutes);
  
}

GLrgba  EnvColor (eEnvColor type)
{

  return color[type];

}

GLvector2 EnvFog ()
{

  return fog;

}

float EnvStars ()
{

  return stars;

}

