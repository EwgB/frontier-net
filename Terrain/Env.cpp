/*-----------------------------------------------------------------------------

  Env.cpp

-------------------------------------------------------------------------------

  The environment. Lighting, fog, and so on.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "env.h"
#include "input.h"
#include "math.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "world.h"

#define TIME_SCALE          250  //how many "milliseconds per in-game minute
#define max_DISTANCE        450
#define NIGHT_FOG           (max_DISTANCE / 5)
#define ENV_TRANSITION      0.2f
#define UPDATE_INTERVAL     200 //milliseconds
#define SECONDS_TO_DECIMAL  (1.0f / 60.0f)

#define TIME_DAWN           5.5f  // 5:30am
#define TIME_DAY            6.5f  // 6:30am
#define TIME_SUNSET         19.5f // 7:30pm
#define TIME_DUSK           20.5f // 8:30pm

#define NIGHT_COLOR         glRgba (0.0f, 0.2f, 0.5f)
#define DAY_COLOR           glRgba (0.4f, 0.7f, 1.0f)

#define NIGHT_SCALING       glRgba (0.0f, 0.1f, 0.4f)
#define DAY_SCALING         glRgba (1.0f)

#define VECTOR_NIGHT        glVector ( 0.0f, 0.0f, -1.0f)
#define VECTOR_SUNRISE      glVector (-0.8f, 0.0f, -0.2f)
#define VECTOR_MORNING      glVector (-0.5f, 0.0f, -0.5f)
#define VECTOR_AFTERNOON    glVector ( 0.5f, 0.0f, -0.5f)
#define VECTOR_SUNSET       glVector ( 0.8f, 0.0f, -0.2f)


static Env        desired;
static Env        current;
static long       seconds;
static int        minutes;
static int        hours;
static int        update;
static float      decimal_time;
static int        last_decimal_time;    
static bool       cycle_on;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_cycle ()
{

  Region*   r;
  int       i;
  GLrgba    average;
  GLrgba    base_color;
  GLrgba    color_scaling;
  float     fade;
  float     humid_fog;

  r = (Region*)CameraRegion ();
  humid_fog = (1.0f - r->moisture) * max_DISTANCE;
  desired.sunrise_fade = desired.sunset_fade = 0.0f;
  if (decimal_time >= TIME_DAWN && decimal_time < TIME_DAY) { //sunrise
    fade = (decimal_time - TIME_DAWN) / (TIME_DAY - TIME_DAWN);
    base_color = glRgbaInterpolate (NIGHT_COLOR, DAY_COLOR, fade);
    desired.fog_max = MathInterpolate (NIGHT_FOG, max_DISTANCE, fade);
    desired.fog_min = min (humid_fog, fade * max_DISTANCE);
    desired.star_fade = max (1.0f - fade * 2.0f, 0.0f);
    //Sunrise fades in, then back out
    desired.sunrise_fade = 1.0f - abs (fade -0.5f) * 2.0f;
    color_scaling = glRgbaInterpolate (NIGHT_SCALING, DAY_SCALING, fade);
    //The light in the sky doesn't lighten until the second half of sunrise
    if (fade > 0.5f)
      desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.5f);
    else
      glRgba (0.5f, 0.7f, 1.0f);
    desired.light = glVectorInterpolate (VECTOR_SUNRISE, VECTOR_MORNING, fade);
  } else if (decimal_time >= TIME_DAY && decimal_time < TIME_SUNSET)  { //day
    fade = (decimal_time - TIME_DAY) / (TIME_SUNSET - TIME_DAY);
    base_color = DAY_COLOR;
    desired.fog_max = max_DISTANCE;
    desired.fog_min = humid_fog;
    desired.star_fade = 0.0f;
    color_scaling = DAY_SCALING;
    desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f) + r->color_atmosphere;
    desired.color[ENV_COLOR_LIGHT].Normalize ();
    desired.light = glVector (0, 0.5f, -0.5f);
    desired.light = glVectorInterpolate (VECTOR_MORNING, VECTOR_AFTERNOON, fade);
  } else if (decimal_time >= TIME_SUNSET && decimal_time < TIME_DUSK) { // sunset
    fade = (decimal_time - TIME_SUNSET) / (TIME_DUSK - TIME_SUNSET);
    base_color = glRgbaInterpolate (DAY_COLOR, NIGHT_COLOR, fade);
    desired.fog_max = MathInterpolate (max_DISTANCE, NIGHT_FOG, fade);
    desired.fog_min = min (humid_fog, (1.0f - fade) * max_DISTANCE);
    if (fade > 0.5f)
      desired.star_fade = (fade - 0.5f) * 2.0f;
    //Sunset fades in, then back out
    desired.sunset_fade = 1.0f - abs (fade -0.5f) * 2.0f;
    color_scaling = glRgbaInterpolate (DAY_SCALING, NIGHT_SCALING, fade);
    desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 0.5f, 0.5f);
    desired.light = glVector (0.8f, 0.0f, -0.2f);
    desired.light = glVectorInterpolate (VECTOR_AFTERNOON, VECTOR_SUNSET, fade);
 } else { //night
    color_scaling = NIGHT_SCALING;
    base_color = NIGHT_COLOR;
    desired.fog_min = 1;
    desired.fog_max = NIGHT_FOG;
    desired.star_fade = 1.0f;
    desired.color[ENV_COLOR_LIGHT] = glRgba (0.5f, 0.7f, 1.0f);
    desired.light = VECTOR_NIGHT;
  }
  for (i = 0; i < ENV_COLOR_COUNT; i++) {
    if (i == ENV_COLOR_LIGHT) 
      continue;
    average = base_color + r->color_atmosphere;
    average.Normalize ();
    desired.color[i] = average;
    if (i == ENV_COLOR_SKY) 
      desired.color[i] = base_color * 0.75f;
    desired.color[i] *= color_scaling;
  }   

}

static void do_time (float delta)
{

  //Convert out hours and minutes into a decimal number. (100 "minutes" per hour.)
  desired.light = glVector (-0.5f, 0.0f, -0.5f);
  decimal_time = (float)hours + (float)minutes * SECONDS_TO_DECIMAL;
  if (decimal_time != last_decimal_time)
    do_cycle ();
   
  for (int i = 0; i < ENV_COLOR_COUNT; i++) 
    current.color[i] = glRgbaInterpolate (current.color[i], desired.color[i], delta);
  current.fog_min = MathInterpolate (current.fog_min, desired.fog_min, delta);
  current.fog_max = MathInterpolate (current.fog_max, desired.fog_max, delta);
  current.star_fade = MathInterpolate (current.star_fade, desired.star_fade, delta);
  current.sunset_fade = MathInterpolate (current.sunset_fade, desired.sunset_fade, delta);
  current.sunrise_fade = MathInterpolate (current.sunrise_fade, desired.sunrise_fade, delta);
  current.light = glVectorInterpolate (current.light, desired.light, delta);
  current.light.Normalize ();

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void    EnvInit ()
{

  hours = 8;
  do_time (1);
  current = desired;

}

void    EnvUpdate ()
{

  if (InputKeyPressed (SDLK_RIGHTBRACKET))
    hours++;
  if (InputKeyPressed (SDLK_LEFTBRACKET)) {
    hours--;
    if (hours < 0)
      hours += 24;
  }
  if (InputKeyPressed (SDLK_BACKSLASH))
    cycle_on = !cycle_on;
  if (cycle_on)
    seconds += SdlElapsed ();
  if (seconds >= TIME_SCALE) {
    seconds -= TIME_SCALE;
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
