/*-----------------------------------------------------------------------------
  Env.cpp
-------------------------------------------------------------------------------
  The environment. Lighting, fog, and so on.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include "avatar.h"
#include "env.h"
#include "game.h"
#include "input.h"
#include "math.h"
#include "scene.h"
#include "sdl.h"
#include "text.h"
#include "texture.h"
#include "world.h"


enum eEnvColor
{
  ENV_COLOR_HORIZON,
  ENV_COLOR_SKY,
  ENV_COLOR_FOG,
  ENV_COLOR_LIGHT,
  ENV_COLOR_AMBIENT,
  ENV_COLOR_COUNT
};

struct Range
{
  float   rmin;
  float   rmax;
};

struct Env
{
  GLrgba      color[ENV_COLOR_COUNT];
  GLvector    light;
  Range       fog;
  float       star_fade;
  float       sunrise_fade;
  float       sunset_fade;
  float       sun_angle;
  float       cloud_cover;
  bool        draw_sun;
};

void          EnvInit ();
void          EnvUpdate ();
Env*          EnvGet ();

#define TIME_SCALE          300  //how many milliseconds per in-game minute
//#define max_distance        900
#define NIGHT_FOG           (max_distance / 5)
#define ENV_TRANSITION      0.02f
#define UPDATE_INTERVAL     50 //milliseconds
#define SECONDS_TO_DECIMAL  (1.0f / 60.0f)

#define TIME_DAWN           5.5f  // 5:30am
#define TIME_DAY            6.5f  // 6:30am
#define TIME_SUNSET         19.5f // 7:30pm
#define TIME_DUSK           20.5f // 8:30pm

#define NIGHT_COLOR         glRgba (0.0f, 0.0f, 0.3f)
#define DAY_COLOR           glRgba (1.0f, 1.0f, 1.0f)

#define NIGHT_SCALING       glRgba (0.0f, 0.1f, 0.4f)
#define DAY_SCALING         glRgba (1.0f)

#define VECTOR_NIGHT        glVector ( 0.0f, 0.0f, -1.0f)
#define VECTOR_SUNRISE      glVector (-0.8f, 0.0f, -0.2f)
#define VECTOR_MORNING      glVector (-0.5f, 0.0f, -0.5f)
#define VECTOR_AFTERNOON    glVector ( 0.5f, 0.0f, -0.5f)
#define VECTOR_SUNSET       glVector ( 0.8f, 0.0f, -0.2f)

#define SUN_ANGLE_SUNRISE   -10
#define SUN_ANGLE_MORNING   15
#define SUN_ANGLE_AFTERNOON 165
#define SUN_ANGLE_SUNSET    190

static Env        desired;
static Env        current;
static int        update;
static int        last_decimal_time;    
static bool       cycle_on;

static void do_cycle ()
{
  Region*   r;
  int       i;
  GLrgba    average;
  GLrgba    base_color;
  GLrgba    color_scaling;
  GLrgba    atmosphere;
  float     fade;
  float     late_fade;
  //float     humid_fog;
  float     decimal_time;
  float     max_distance;
  Range     time_fog;
  Range     humid_fog;

  max_distance = SceneVisibleRange ();
  r = (Region*)AvatarRegion ();
  //atmosphere = r->color_atmosphere;
  humid_fog.rmax = MathInterpolate (max_distance, max_distance * 0.75f, r->moisture);
  humid_fog.rmin = MathInterpolate (max_distance * 0.85f, max_distance * 0.25f, r->moisture);
  if (r->climate == CLIMATE_SWAMP) {
    humid_fog.rmax /= 2.0f;
    humid_fog.rmin /= 2.0f;
  }
  desired.cloud_cover = clamp (r->moisture, 0.20f, 0.6f);
  desired.sunrise_fade = desired.sunset_fade = 0.0f;
  decimal_time = fmod (GameTime (), 24.0f);
  if (decimal_time >= TIME_DAWN && decimal_time < TIME_DAY) { //sunrise
    fade = (decimal_time - TIME_DAWN) / (TIME_DAY - TIME_DAWN);
    late_fade = max ((fade -0.5f) * 2.0f, 0);
    base_color = glRgbaInterpolate (NIGHT_COLOR, DAY_COLOR, late_fade);
    atmosphere = glRgbaInterpolate (glRgba (0.0f), glRgba (1.0f), late_fade);
    time_fog.rmax = MathInterpolate (NIGHT_FOG, max_distance, fade);
    time_fog.rmin = time_fog.rmax / 2.0f;
    desired.star_fade = max (1.0f - fade * 2.0f, 0.0f);
    //Sunrise fades in, then back out
    desired.sunrise_fade = 1.0f - abs (fade -0.5f) * 2.0f;
    color_scaling = glRgbaInterpolate (NIGHT_SCALING, DAY_SCALING, fade);
    //The light in the sky doesn't lighten until the second half of sunrise
    if (fade > 0.5f)
      desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.5f);
    else
      desired.color[ENV_COLOR_LIGHT] = glRgba (0.5f, 0.7f, 1.0f);
    desired.light = glVectorInterpolate (VECTOR_SUNRISE, VECTOR_MORNING, fade);
    desired.sun_angle = MathInterpolate (SUN_ANGLE_SUNRISE, SUN_ANGLE_MORNING, fade);
    desired.draw_sun = true;
    desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 0.6f);
  } else if (decimal_time >= TIME_DAY && decimal_time < TIME_SUNSET)  { //day
    atmosphere = glRgba (1.0f);
    fade = (decimal_time - TIME_DAY) / (TIME_SUNSET - TIME_DAY);
    base_color = DAY_COLOR;
    time_fog.rmax = max_distance;
    time_fog.rmin = time_fog.rmax / 2.0f;
    desired.star_fade = 0.0f;
    color_scaling = DAY_SCALING;
    desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f) + r->color_atmosphere;
    desired.color[ENV_COLOR_LIGHT].Normalize ();
    desired.light = glVector (0, 0.5f, -0.5f);
    desired.light = glVectorInterpolate (VECTOR_MORNING, VECTOR_AFTERNOON, fade);
    desired.sun_angle = MathInterpolate (SUN_ANGLE_MORNING, SUN_ANGLE_AFTERNOON, fade);
    desired.draw_sun = true;
    desired.color[ENV_COLOR_AMBIENT] = glRgba (0.4f, 0.4f, 0.4f);
  } else if (decimal_time >= TIME_SUNSET && decimal_time < TIME_DUSK) { // sunset
    fade = (decimal_time - TIME_SUNSET) / (TIME_DUSK - TIME_SUNSET);
    base_color = glRgbaInterpolate (DAY_COLOR, NIGHT_COLOR, fade);
    time_fog.rmax = MathInterpolate (max_distance, NIGHT_FOG, fade);
    time_fog.rmin = time_fog.rmax / 2.0f;
    if (fade > 0.5f)
      desired.star_fade = (fade - 0.5f) * 2.0f;
    //Sunset fades in, then back out
    atmosphere = glRgbaInterpolate (glRgba (1.0f), glRgba (0.0f), min (1.0f, fade * 2.0f));
    desired.sunset_fade = 1.0f - abs (fade -0.5f) * 2.0f;
    color_scaling = glRgbaInterpolate (DAY_SCALING, NIGHT_SCALING, fade);
    desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 0.5f, 0.5f);
    desired.light = glVector (0.8f, 0.0f, -0.2f);
    desired.light = glVectorInterpolate (VECTOR_AFTERNOON, VECTOR_SUNSET, fade);
    desired.sun_angle = MathInterpolate (SUN_ANGLE_AFTERNOON, SUN_ANGLE_SUNSET, fade);
    desired.draw_sun = true;
    desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 0.6f);
 } else { //night
   atmosphere = glRgba (0.0f);
    color_scaling = NIGHT_SCALING;
    base_color = NIGHT_COLOR;
    time_fog.rmin = 1;
    time_fog.rmax = NIGHT_FOG;
    desired.star_fade = 1.0f;
    desired.color[ENV_COLOR_LIGHT] = glRgba (0.1f, 0.3f, 0.7f);
    desired.color[ENV_COLOR_AMBIENT] = glRgba (0.0f, 0.0f, 0.4f);
    desired.light = VECTOR_NIGHT;
    desired.sun_angle = -90.0f;
    desired.draw_sun = false;
  }
  desired.fog.rmax = min (humid_fog.rmax, time_fog.rmax);
  desired.fog.rmin = min (humid_fog.rmin, time_fog.rmin);
  for (i = 0; i < ENV_COLOR_COUNT; i++) {
    if (i == ENV_COLOR_LIGHT || i == ENV_COLOR_AMBIENT) 
      continue;
    average = base_color * atmosphere;
    //average.Normalize ();
    //average /= 3;
    desired.color[i] = average;
    if (i == ENV_COLOR_SKY) 
      desired.color[i] = base_color * 0.75f;
    desired.color[i] *= color_scaling;
  }   
  desired.color[ENV_COLOR_SKY] = r->color_atmosphere;
  desired.color[ENV_COLOR_HORIZON] = (desired.color[ENV_COLOR_SKY] + atmosphere + atmosphere) / 3.0f;
  desired.color[ENV_COLOR_FOG] = desired.color[ENV_COLOR_HORIZON];//desired.color[ENV_COLOR_SKY];
  //desired.color[ENV_COLOR_SKY] = desired.color[ENV_COLOR_HORIZON] * glRgba (0.2f, 0.2f, 0.8f);

}

static void do_time (float delta)
{

  //Convert out hours and minutes into a decimal number. (100 "minutes" per hour.)
  desired.light = glVector (-0.5f, 0.0f, -0.5f);
  if (GameTime () != last_decimal_time)
    do_cycle ();
  last_decimal_time = GameTime ();     
  for (int i = 0; i < ENV_COLOR_COUNT; i++) 
    current.color[i] = glRgbaInterpolate (current.color[i], desired.color[i], delta);
  current.fog.rmin = MathInterpolate (current.fog.rmin, desired.fog.rmin, delta);
  current.fog.rmax = MathInterpolate (current.fog.rmax, desired.fog.rmax, delta);
  current.star_fade = MathInterpolate (current.star_fade, desired.star_fade, delta);
  current.sunset_fade = MathInterpolate (current.sunset_fade, desired.sunset_fade, delta);
  current.sunrise_fade = MathInterpolate (current.sunrise_fade, desired.sunrise_fade, delta);
  current.light = glVectorInterpolate (current.light, desired.light, delta);
  current.sun_angle = MathInterpolate (current.sun_angle, desired.sun_angle, delta);
  current.cloud_cover = MathInterpolate (current.cloud_cover, desired.cloud_cover, delta);
  current.draw_sun = desired.draw_sun;
  current.light.Normalize ();
}

void    EnvInit ()
{
  do_time (1);
  current = desired;
}

void    EnvUpdate ()
{
  update += SdlElapsed ();
  if (update > UPDATE_INTERVAL) {
    do_time (ENV_TRANSITION);
    update -= UPDATE_INTERVAL;
  }
}

Env* EnvGet ()
{
  return &current;
}
*/