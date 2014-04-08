using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Frontier {
	class Game : GameWindow {
		private Vector3
		  cameraPosition = new Vector3(15, 10, 15),
		  playerPosition = new Vector3(0, 0, 0);
		private Vector4
		  lightPosition = new Vector4(3, 3, 3, 1);
		private const float fov = 1.04719755f;
		private const int width = 1024;
		private const int height = 800;

		private double gameTime = 0;
		//private Vector2 prevMousePos = new Vector2(0, 0);
		//private Matrix4 Rotation = new Matrix4(
		//  1, 0, 0, 0,
		//  0, 1, 0, 0,
		//  0, 0, 1, 0,
		//  0, 0, 0, 1);

		//private bool
		//  gridXY = false, 
		//  gridXZ = true,
		//  gridYZ = false,
		//  VBOSwitched = false,
		//  RotatingCamera = false;

		//private int[,] vboIDs;
		//private int currentVBO = 0;
		//private const int SIZE_OF_DATA = 6;
		//private const int SIZE_OF_FLOAT = sizeof(float);
		//private const string GEO_PATH = "../../res/geometry/";

		/// <summary>Creates a 1024x800 window with the specified title.</summary>
		public Game() : base(width, height, GraphicsMode.Default, "OpenTK Test") {
			VSync = VSyncMode.On;
		}

		/// <summary>Load resources here.</summary>
		/// <param name="e">Not used.</param>
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			//Setup OpenGL
			GL.Light(LightName.Light0, LightParameter.Position, lightPosition);
			GL.Enable(EnableCap.Light0);
			GL.Enable(EnableCap.Lighting);

			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Normalize);

			GL.ClearColor(0.1f, 0.2f, 0.5f, 0.0f);

			//Setup Projection and Viewport
			GL.Viewport(0, 0, 1024, 800);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(fov, (float) width / (float) height, 0.1f, 100.0f);
			GL.LoadMatrix(ref perspective);
			GL.MatrixMode(MatrixMode.Modelview);

	//ConsoleLog ("%s: Begin startup.", APP);
	//ConsoleInit ();
	//ParticleInit ();
	//ilInit ();
	//Init (11);
	//SdlInit ();
	//RenderInit ();
	//EnvInit ();
	//GameInit ();
	//PlayerInit ();
	//AvatarInit ();
	//TextureInit ();
	//WorldInit ();
	//SceneInit ();
	//SkyInit ();
	//TextInit ();
	//ConsoleLog ("init: Done.");

		}

		/// <summary>
		/// Called when your window is resized. Set your viewport here. It is also
		/// a good place to set up your projection Matrix (which probably changes
		/// along when the aspect ratio of your window).
		/// </summary>
		/// <param name="e">Not used.</param>
		protected override void OnResize(EventArgs e) {
			base.OnResize(e);

			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float) Math.PI / 4, Width / (float) Height, 1.0f, 64.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);
		}

		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		protected override void OnUpdateFrame(FrameEventArgs e) {
			base.OnUpdateFrame(e);

			if (Keyboard[Key.Escape])
				Exit();
		}

		/// <summary>
		/// Called when it is time to render the next frame. Add your rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		protected override void OnRenderFrame(FrameEventArgs e) {
			base.OnRenderFrame(e);
			gameTime += e.Time;

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//Setting the current object transformation
			Matrix4 modelview = Matrix4.LookAt(cameraPosition, playerPosition, Vector3.UnitY);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelview);

			SwapBuffers();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (Game game = new Game()) {
				game.Run(30.0);
			}
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
#include "Texture.h"
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

static void term ()
{

  GameTerm ();
  TextureTerm ();
  SdlTerm ();

}

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

void MainQuit ()
{ 

  quit = true;

}


bool MainIsQuit ()
{ 

  return quit;

}

bool ConsoleCgCompile (List<string> *args) 
{

  CgCompile ();
  return true;

}

int PASCAL WinMain (HINSTANCE instance_in, HINSTANCE previous_instance, LPSTR command_line, int show_style)
{

  //Variables
  CVarUtils::CreateCVar ("avatar.expand", false, "Resize avatar proportions to be more cartoon-y.");
  CVarUtils::CreateCVar ("render.shaders", true, "Enable vertices, fragment shaders.");
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
  CVarUtils::CreateCVar ("cache.Size", CacheSize, "Returns the current Size of the cache.");
  CVarUtils::CreateCVar ("game", GameCmd, "Usage: Game [ new | quit ]");
  CVarUtils::CreateCVar ("particle", ParticleCmd, "Usage: particle <filename>");
  CVarUtils::Load (SETTINGS_FILE);

  init ();
  run ();
  term ();
  CVarUtils::Save (SETTINGS_FILE);
  return 0;

}

 */