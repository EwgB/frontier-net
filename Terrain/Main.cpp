/*-----------------------------------------------------------------------------

  Main.cpp


-------------------------------------------------------------------------------

TODO:

x Fix canopy trees
x Fix UV mapping on trees.
x Clouds
* More biomes
x World saving
* Undergrowth
x Terrains eat less CPU cycles per frame
x Grass rotate
x Fix avatar rendering
? Fix opaque trees on mike's PC
* shadows
* Collision
* Particles

http://www.bramstein.com/projects/gui/

//glActiveTexture(GL_TEXTURE0);glBindTexture(GL_TEXTURE_2D, decal)


-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "avatar.h"
#include "cache.h"
#include "console.h"
#include "cg.h"
#include "env.h"
#include "game.h"
#include "sdl.h"
#include "il\il.h"
#include "main.h"
#include "particle.h"
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
#ifdef DEBUG
#pragma comment( lib, "H:/SDK/glConsole/lib/debug/cvars.lib" )	 
#else
#pragma comment( lib, "H:/SDK/glConsole/lib/release/cvars.lib" )	  
#endif

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
    ParticleUpdate ();
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

bool ConsoleCgCompile (vector<string> *args) 
{
  CgCompile ();
  return true;
}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

int PASCAL WinMain (HINSTANCE instance_in, HINSTANCE previous_instance, LPSTR command_line, int show_style)
{

  //Variables
  CVarUtils::CreateCVar ("avatar.expand", false, "Resize avatar proportions to be more cartoon-y.");
  CVarUtils::CreateCVar ("render.shaders", true, "Enable vertex, fragment shaders.");
  CVarUtils::CreateCVar ("render.wireframe", false, "Overlay scene with wireframe.");
  CVarUtils::CreateCVar ("render.textured", true, "Render the scene with textures.");
  CVarUtils::CreateCVar ("show.skeleton", false, "Show the skeletons of avatars.");
  CVarUtils::CreateCVar ("show.stats", false, "Show various debug statistics.");
  CVarUtils::CreateCVar ("show.pages", false, "Show bounding boxes for paged data.");
  CVarUtils::CreateCVar ("show.vitals", false, "Show the player statistics.");
  CVarUtils::CreateCVar ("show.region", false, "Show information about the currently occupied region.");
  CVarUtils::CreateCVar ("cache.active", false, "Controls saving of paged data.");
  CVarUtils::CreateCVar ("flying", false, "Allows flight.");
  CVarUtils::CreateCVar ("mouse.invert", false, "Reverse mouse y axis.");
  CVarUtils::CreateCVar ("mouse.sensitivity", 1.0f, "Mouse tracking");
  CVarUtils::CreateCVar ("last_played", 0, "");
  //Functions
  CVarUtils::CreateCVar ("compile", ConsoleCgCompile, "");
  CVarUtils::CreateCVar ("cache.dump", CacheDump, "Clear all saved data from memory & disk.");
  CVarUtils::CreateCVar ("cache.size", CacheSize, "Returns the current size of the cache.");
  CVarUtils::CreateCVar ("game", GameCmd, "Usage: Game [ new | quit ]");
  CVarUtils::Load (SETTINGS_FILE);

  init ();
  run ();
  term ();
  CVarUtils::Save (SETTINGS_FILE);
  return 0;

}
