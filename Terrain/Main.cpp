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

#include "il\il.h"

#pragma comment (lib, "opengl32.lib")
#pragma comment (lib, "sdl.lib")
#pragma comment (lib, "glu32.lib")
#pragma comment (lib, "DevIL.lib")


static bool       quit;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void init ()
{

  LogInit (APP ".log");
  ilInit ();
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
/*
void do_thing ()
{

  ILubyte *Lump;
  ILuint Size;
  FILE *fff;


  fff = fopen("textures//terrain.png", "rb");
  fseek(fff, 0, SEEK_END);
  Size = ftell(fff);

  Lump = (ILubyte*)malloc(Size);
  fseek(fff, 0, SEEK_SET);
  fread(Lump, 1, Size, fff);
  fclose(fff);
  unsigned img;
  ilGenImages (1, &img);
  ilBindImage (img);
  free(Lump);

}
*/

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

int PASCAL WinMain (HINSTANCE instance_in, HINSTANCE previous_instance, LPSTR command_line, int show_style)
{

  init ();
  //do_thing ();
  run ();
  term ();
  return 0;

}
