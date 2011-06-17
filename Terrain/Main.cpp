/*-----------------------------------------------------------------------------

  Main.cpp


-------------------------------------------------------------------------------

 TODO:

 x Move player movement to Avatar.cpp
 x Migrate region building to Terraform.cpp
 * Make water only build needed polygons.
 x Move normals to pages, use them on grass.
 * Move "world" stuff to new "cache.cpp".  Move world-generation stuf to world.cpp.
 * Fix potential wasted time in page update.

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
#include "region.h"
#include "render.h"
#include "scene.h"
#include "sky.h"
#include "text.h"
#include "texture.h"

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
  RegionInit ();
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
