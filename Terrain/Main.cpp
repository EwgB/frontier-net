/*-----------------------------------------------------------------------------

  Main.cpp


-------------------------------------------------------------------------------

 
-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include <iostream>
#include <stdio.h>
//#include <tchar.h>

#include "camera.h"
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
    CameraUpdate ();
    EnvUpdate ();
    SkyUpdate ();
    SceneUpdate (stop);
    WorldUpdate (stop);
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
