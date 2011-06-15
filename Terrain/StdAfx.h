//#define LIMIT_INTERVAL(interval)  { static unsigned next_update; if (next_update > GetTickCount ()) return; next_update = GetTickCount () + interval;}
#define CLAMP(a,b,c)              (a < b ? b : (a > c ? c : a))
#define WRAP(x,y)                 ((unsigned)x % y)
#define SIGN(x)                   (((x) > 0) ? 1 : ((x) < 0) ? -1 : 0)
#define SIGNF(x)                  (((x) > NEGLIGIBLE) ? 1 : ((x) < -NEGLIGIBLE) ? -1 : 0)
#define ABS(x)                    (((x) < 0 ? (-x) : (x)))
#define SMALLEST(x,y)             (ABS(x) < ABS(y) ? 0 : x)                
#define SWAP(a,b)                 {int temp = a;a = b; b = temp;}
#define SWAPF(a,b)                {float temp = a;a = b; b = temp;}
#define ARGS(text, args)          { va_list		ap;	text[0] = 0; if (args != NULL)	{ va_start(ap, args); vsprintf(text, args, ap); va_end(ap);}	}
#define INTERPOLATE(a,b,delta)    (a * (1.0f - delta) + b * delta)
#define MIN(x,y)                  ((x) < (y) ? x : y)                
#define MAX(x,y)                  ((x) > (y) ? x : y)                
#define clamp(n,lower,upper)      (max (min(n,(upper)), (lower)))
 

#define FREEZING                  0.32f
#define TEMP_COLD                 0.45f
#define TEMP_TEMPERATE            0.6f
#define TEMP_HOT                  0.9f
#define MIN_TEMP                  0.0f
#define MAX_TEMP                  1.0f
#define DEGREES_TO_RADIANS        .017453292F
#define RADIANS_TO_DEGREES        57.29577951F
#define NEGLIGIBLE                0.000000000001f
#define PI                        ((double)3.1415926535F)
#define GRAVITY                   9.5f

#include <vector>
using namespace std;
#include <windows.h>
#include <SDL.h>
#include <SDL_opengl.h>
#include "gltypes.h"
#include "gl/gl.h"

enum SurfaceColor
{
  SURFACE_COLOR_BLACK,
  SURFACE_COLOR_SAND,
  SURFACE_COLOR_DIRT,
  SURFACE_COLOR_GRASS,
  SURFACE_COLOR_ROCK,
  SURFACE_COLOR_SNOW,
};

enum SurfaceType
{
  SURFACE_NULL,
  SURFACE_SAND_DARK,
  SURFACE_SAND,
  SURFACE_DIRT_DARK,
  SURFACE_DIRT,
  SURFACE_EDGE,
  SURFACE_GRASS,
  SURFACE_GRASS_EDGE,
  SURFACE_DEEPGRASS,
  SURFACE_ROCK,
  SURFACE_SNOW,
  SURFACE_TYPES
};

enum
{
  NORTH,
  SOUTH,
  EAST,
  WEST
};

struct Cell
{
  float elevation;
  float water_level;
  float detail;
};