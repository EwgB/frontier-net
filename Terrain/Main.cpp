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

http://www.bramstein.com/projects/gui/

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "avatar.h"
#include "camera.h"
#include "cache.h"
#include "console.h"
#include "env.h"
#include "sdl.h"
#include "il\il.h"
#include "main.h"
#include "random.h"
#include "render.h"
#include "scene.h"
#include "sky.h"
#include "text.h"
#include "texture.h"
#include "world.h"

#pragma comment (lib, "opengl32.lib") //OpenGL
#pragma comment (lib, "glu32.lib")    //OpenGL
#pragma comment (lib, "sdl.lib")      //Good 'ol SDL.
#pragma comment (lib, "DevIL.lib")    //For loading images
#pragma comment( lib, "cg.lib" )		  //NVIDIA Cg toolkit			
#pragma comment( lib, "cggl.lib" )	  //NVIDIA Cg toolkit			
#pragma comment( lib, "H:/SDK/glConsole/lib/debug/cvars.lib" )	  //NVIDIA Cg toolkit		

#define SETTINGS_FILE   "frontier.set"

static bool           quit;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void init ()
{

  ConsoleLog ("%s: Begin startup.", APP);
  ConsoleInit ();
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
  ConsoleLog ("init: Done.");

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
    ConsoleUpdate ();
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

  CVarUtils::Load (SETTINGS_FILE);
  //int& nTest = CVarUtils::CreateCVar ("testVar", 100, "Another test CVar");
  bool& render_shaders = CVarUtils::CreateCVar ("render.shaders", true, "enable vertex, fragment shaders");
  bool& render_wireframe = CVarUtils::CreateCVar ("render.wireframe", false, "overlay scene with wireframe");
  bool& show_skeleton = CVarUtils::CreateCVar ("show.skeleton", false, "show the skeletons of avatars");
  bool& show_stats = CVarUtils::CreateCVar ("show.stats", false, "show various debug statistics");

  init ();
  run ();
  term ();
  CVarUtils::Save (SETTINGS_FILE);
  return 0;

}
