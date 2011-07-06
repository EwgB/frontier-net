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
#include "cache.h"
#include "console.h"
#include "env.h"
#include "game.h"
#include "sdl.h"
#include "il\il.h"
#include "main.h"
#include "player.h"
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

#define SETTINGS_FILE   "user.set"

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
  GameInit ();
  PlayerInit ();
  AvatarInit ();
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

  GameTerm ();
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
    GameUpdate ();
    AvatarUpdate ();
    PlayerUpdate ();
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


bool MainIsQuit ()
{ 

  return quit;

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

int PASCAL WinMain (HINSTANCE instance_in, HINSTANCE previous_instance, LPSTR command_line, int show_style)
{

  CVarUtils::CreateCVar ("avatar.expand", false, "Resize avatar proportions to be more cartoon-y.");
  CVarUtils::CreateCVar ("render.shaders", true, "Enable vertex, fragment shaders.");
  CVarUtils::CreateCVar ("render.wireframe", false, "Overlay scene with wireframe.");
  CVarUtils::CreateCVar ("render.textured", true, "Render the scene with textures.");
  CVarUtils::CreateCVar ("show.skeleton", false, "Show the skeletons of avatars.");
  CVarUtils::CreateCVar ("show.stats", false, "Show various debug statistics.");
  CVarUtils::CreateCVar ("show.pages", false, "Show bounding boxes for paged data.");
  CVarUtils::CreateCVar ("show.vitals", false, "Show the player statistics.");
  bool& cache_active = CVarUtils::CreateCVar ("cache.active", true, "Controls saving of paged data.");
  CVarUtils::CreateCVar ("flying", false, "Allows flight.");
  CVarUtils::CreateCVar ("mouse.invert", false, "Reverse mouse y axis.");
  CVarUtils::CreateCVar ("mouse.sensitivity", 1.0f, "Mouse tracking");

  //Functions
  CVarUtils::CreateCVar ("cache.dump", CacheDump, "Clear all saved data from memory & disk.");
  CVarUtils::CreateCVar ("cache.size", CacheSize, "Returns the current size of the cache.");
  CVarUtils::CreateCVar ("game", GameCmd, "Usage: Game [ new | quit ]");
  CVarUtils::Load (SETTINGS_FILE);

  init ();
  run ();
  term ();
  CVarUtils::Save (SETTINGS_FILE);
  //CVarUtils::Save ();
  return 0;

}
