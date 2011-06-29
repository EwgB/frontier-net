/*-----------------------------------------------------------------------------

  Main.cpp


-------------------------------------------------------------------------------

TODO:

* Small trees up, big trees down
http://opengameart.org/content/very-low-poly-human


* Lighting
* Culling
* Pathing
* Saving development costs
* Dynamic content & replayability

* FUEL and Minecraft

http://www.youtube.com/watch?v=-d2-PtK4F6Y

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "avatar.h"
#include "camera.h"
#include "cache.h"
#include "env.h"
#include "sdl.h"
#include "main.h"
#include "log.h"
#include "random.h"
#include "render.h"
#include "scene.h"
#include "sky.h"
#include "text.h"
#include "texture.h"
#include "world.h"

#pragma comment (lib, "opengl32.lib")
#pragma comment (lib, "sdl.lib")
#pragma comment (lib, "glu32.lib")

static bool       quit;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void init ()
{

  LogInit (APP ".log");
  RandomInit (11);
  SdlInit ();
  RenderInit ();
  EnvInit ();
  AvatarInit ();
  CameraInit ();
  TextureInit ();
  WorldInit ();
  SceneInit ();
  SkyInit ();
  TextInit ();

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void term ()
{

  AvatarTerm ();
  CameraTerm ();
  TextureTerm ();
  SdlTerm ();

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void run ()
{

  long    stop;
  long    remaining;

  while (!quit) {
    stop = SdlTick () + 15;
    SdlUpdate ();
    AvatarUpdate ();
    CameraUpdate ();
    EnvUpdate ();
    SkyUpdate ();
    SceneUpdate (stop);
    CacheUpdate (stop);
    RenderUpdate ();
    Render ();	
    remaining = stop - SdlTick ();
    if (remaining > 0) 
      Sleep (remaining);
  }

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void MainQuit ()
{ 

  quit = true;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

int PASCAL WinMain (HINSTANCE instance_in, HINSTANCE previous_instance, LPSTR command_line, int show_style)
{

  init ();
  run ();
  term ();
  return 0;

}
