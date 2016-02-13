// Original TODO:
//* More biomes
//x World saving
//x Undergrowth
//? Fix opaque trees on mike's PC
//* shadows
//* Collision
//* Particles
//* Weather
//
//http://awesomium.com/
//
//http://www.bramstein.com/projects/gui/
//
//glActiveTexture(GL_TEXTURE0);glBindTexture(GL_TEXTURE_2D, decal)
namespace terrain_sharp {
	using System;

	static class Program {
		internal const string APP = "Frontier";

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			// TODO: CVar Variables
			//CVarUtils::CreateCVar("avatar.expand", false, "Resize avatar proportions to be more cartoon-y.");
			//CVarUtils::CreateCVar("render.shaders", true, "Enable vertex, fragment shaders.");
			//CVarUtils::CreateCVar("render.wireframe", false, "Overlay scene with wireframe.");
			//CVarUtils::CreateCVar("render.textured", true, "Render the scene with textures.");
			//CVarUtils::CreateCVar("show.skeleton", false, "Show the skeletons of avatars.");
			//CVarUtils::CreateCVar("show.stats", false, "Show various debug statistics.");
			//CVarUtils::CreateCVar("show.pages", false, "Show bounding boxes for paged data.");
			//CVarUtils::CreateCVar("show.vitals", false, "Show the player statistics.");
			//CVarUtils::CreateCVar("show.region", false, "Show information about the currently occupied region.");
			//CVarUtils::CreateCVar("cache.active", false, "Controls saving of paged data.");
			//CVarUtils::CreateCVar("flying", false, "Allows flight.");
			//CVarUtils::CreateCVar("mouse.invert", false, "Reverse mouse y axis.");
			//CVarUtils::CreateCVar("mouse.sensitivity", 1.0f, "Mouse tracking");
			//CVarUtils::CreateCVar("last_played", 0, "");
			// TODO: CVar Functions
			//CVarUtils::CreateCVar("compile", ConsoleCgCompile, "");
			//CVarUtils::CreateCVar("cache.dump", CacheDump, "Clear all saved data from memory & disk.");
			//CVarUtils::CreateCVar("cache.size", CacheSize, "Returns the current size of the cache.");
			//CVarUtils::CreateCVar("game", GameCmd, "Usage: Game [ new | quit ]");
			//CVarUtils::CreateCVar("particle", ParticleCmd, "Usage: particle <filename>");
			//CVarUtils::Load(SETTINGS_FILE);

			init();
			run();
			term();
			//CVarUtils::Save(SETTINGS_FILE);
		}

		static void init() {
			//ConsoleLog("%s: Begin startup.", APP);
			Console.WriteLine("{0}: Begin startup.", APP);
			//ConsoleInit();
			Particle.Init();
			//ilInit(); What is this anyway?
			//RandomInit(11);
			Sdl.Init();
			//RenderInit();
			//EnvInit();
			//GameInit();
			//PlayerInit();
			//AvatarInit();
			//TextureInit();
			//WorldInit();
			//SceneInit();
			//SkyInit();
			//TextInit();
			//ConsoleLog("init: Done.");
			Console.WriteLine("init: Done.");
		}

		static void term() {
			//GameTerm();
			//TextureTerm();
			//SdlTerm();
		}

		static void run() {
			//long stop;
			//long remaining;

			//while (!quit) {
			//	stop = SdlTick() + 15;
			//	ConsoleUpdate();
			//	SdlUpdate();
			//	GameUpdate();
			//	AvatarUpdate();
			//	PlayerUpdate();
			//	EnvUpdate();
			//	SkyUpdate();
			//	SceneUpdate(stop);
			//	CacheUpdate(stop);
			//	ParticleUpdate();
			//	RenderUpdate();
			//	Render();
			//	remaining = stop - SdlTick();
			//	if (remaining > 0)
			//		Sleep(remaining);
			//}
		}

		static bool Quit;

		internal static void MainQuit() {
			Quit = true;
		}

		internal static bool MainIsQuit() {
			return Quit;
		}
	}
}

/*
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

bool ConsoleCgCompile (vector<string> *args) 
{
  CgCompile ();
  return true;
}
*/
