namespace terrain_sharp {
	using System;

	using SDL2;

	static internal class Sdl {
		private static uint last_update;

		static internal void Init() {
			if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING | SDL.SDL_INIT_JOYSTICK) != 0) {
				//ConsoleLog("Unable to initialize SDL: %s\n", SDL.SDL_GetError());
				Console.WriteLine("Unable to initialize SDL: {0}", SDL.SDL_GetError());
				return;
			}
			var window = Render.Create(1400, 800, 32, false);
			//RenderCreate (1920, 1200, 32, true);
			SDL.SDL_SetWindowIcon(window, SDL.SDL_LoadBMP("textures/f.bmp"));
			//Here we initialize SDL as we would do with any SDL application.
			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
			// We want to enable key repeat. TODO SDL deal with API changes
			//SDL.SDL_EnableKeyRepeat(SDL.SDL_DEFAULT_REPEAT_DELAY, SDL.SDL_DEFAULT_REPEAT_INTERVAL);

			//if (!screen) {
			// Log ("Unable to set video mode: %s\n", SDL.SDL_GetError());
			//	return false;
			//}
			//return true;

			last_update = SDL.SDL_GetTicks();
			//ConsoleLog("SDLInit: %i joysticks found.", SDL.SDL_NumJoysticks());
			Console.WriteLine("SDLInit: {0} joysticks found.", SDL.SDL_NumJoysticks());
			for (int i = 0; i < SDL.SDL_NumJoysticks(); i++) {
				SDL.SDL_JoystickEventState(SDL.SDL_ENABLE);
				//joystick = 
				SDL.SDL_JoystickOpen(i);
			}
		}
	}
}

/*
#include "stdafx.h"

#define MOUSE_SCALING       0.01f

#include "avatar.h"
#include "console.h"
#include "input.h"
#include "main.h"
#include "render.h"
#include "sdl.h"

long  SdlElapsed ();
float SdlElapsedSeconds ();
void  SdlSetCaption (const char* caption);
void  SdlSwapBuffers ();
void  SdlTerm ();
long  SdlTick ();
void  SdlUpdate ();

static bool           lmb;
static bool           mmb;
static int            center_x;
static int            center_y;
static long           elapsed;
static float          elapsed_seconds;

void SdlTerm ()
{
  SDL.SDL_Quit();
}

void SdlSetCaption (const char* caption)
{
  SDL.SDL_WM_SetCaption (caption, "Frontier");
}

void SdlSwapBuffers ()
{
}

void SdlUpdate ()
{
  SDL.SDL_Event event;
  long      now;

  while (SDL.SDL_PollEvent(&event)) { 
    switch(event.type){ 
    case SDL.SDL_QUIT: 
      MainQuit ();
      break; 
    case SDL.SDL_KEYDOWN:
      if (event.key.keysym.sym == SDLK_ESCAPE)
        MainQuit ();
      if (event.key.keysym.sym == SDLK_BACKQUOTE) {
        ConsoleToggle ();      
        break;
      }
      if (ConsoleIsOpen ())
        ConsoleInput (event.key.keysym.sym, event.key.keysym.unicode);
      else
        InputKeyDown (event.key.keysym.sym);
      break;
    case SDL.SDL_JOYAXISMOTION:
      InputJoystickSet (event.jaxis.axis, event.jaxis.value);
      break;
    case SDL.SDL_KEYUP:
      InputKeyUp (event.key.keysym.sym);
      break;
    case SDL.SDL_MOUSEBUTTONDOWN:
      if (event.button.button == SDL.SDL_BUTTON_RIGHT) {
        InputMouselookSet (!InputMouselook ());
        SDL.SDL_ShowCursor (false);
        SDL.SDL_WM_GrabInput (SDL.SDL_GRAB_ON);
      }
      if(event.button.button == SDL.SDL_BUTTON_WHEELUP)
        InputKeyDown (INPUT_MWHEEL_UP);
      if(event.button.button == SDL.SDL_BUTTON_WHEELDOWN)
        InputKeyDown (INPUT_MWHEEL_DOWN);
      if (event.button.button == SDL.SDL_BUTTON_LEFT && !InputMouselook ())
        RenderClick (event.motion.x, event.motion.y);        
      break;
    case SDL.SDL_MOUSEBUTTONUP:
      if (event.button.button == SDL.SDL_BUTTON_LEFT)
        lmb = false;
      else if (event.button.button == SDL.SDL_BUTTON_MIDDLE)
        mmb = false;
      if (InputMouselook ())
        SDL.SDL_ShowCursor (false);
      else { 
        SDL.SDL_ShowCursor (true);
        SDL.SDL_WM_GrabInput (SDL.SDL_GRAB_OFF);
      }
      if(event.button.button == SDL.SDL_BUTTON_WHEELUP)
        InputKeyUp (INPUT_MWHEEL_UP);
      if(event.button.button == SDL.SDL_BUTTON_WHEELDOWN)
        InputKeyUp (INPUT_MWHEEL_DOWN);
      break;
    case SDL.SDL_MOUSEMOTION:
      if (InputMouselook ()) 
        AvatarLook (event.motion.yrel, -event.motion.xrel);
      break;
    case SDL.SDL_VIDEORESIZE: //User resized window
      center_x = event.resize.w / 2;
      center_y = event.resize.h / 2;
      RenderCreate (event.resize.w, event.resize.h, 32, false);
      break; 
    } //Finished with current event

  } //Done with all events for now
  now = SDL.SDL_GetTicks ();
  elapsed = now - last_update;
  elapsed_seconds = (float)elapsed / 1000.0f;
  last_update = now;
}

long SdlElapsed ()
{
  return elapsed;
}

float SdlElapsedSeconds ()
{
  return elapsed_seconds;
}

long SdlTick ()
{
  return SDL.SDL_GetTicks ();
}
*/
	