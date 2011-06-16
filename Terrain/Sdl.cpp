/*-----------------------------------------------------------------------------

  Sdl.cpp

-------------------------------------------------------------------------------

 
-----------------------------------------------------------------------------*/

#include "stdafx.h"

#define MOUSE_SCALING       0.01f

#include "Avatar.h"
#include "input.h"
#include "log.h"
#include "main.h"
#include "render.h"
#include "sdl.h"

static bool           lmb;
static bool           mmb;
static int            center_x;
static int            center_y;
static long           last_update;
static long           elapsed;
static float          elapsed_seconds;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SdlInit ()
{

  if (SDL_Init (SDL_INIT_EVERYTHING) != 0) {
    Log ("Unable to initialize SDL: %s\n", SDL_GetError());
    return;
  }
  SDL_WM_SetIcon(SDL_LoadBMP("/textures/blob.bmp"), NULL);
  RenderCreate (1280, 800, 32, false);
  //Here we initialize SDL as we would do with any SDL application.
	SDL_Init(SDL_INIT_VIDEO);
	SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
	// We want unicode
	SDL_EnableUNICODE(1);
	// We want to enable key repeat
	SDL_EnableKeyRepeat(SDL_DEFAULT_REPEAT_DELAY, SDL_DEFAULT_REPEAT_INTERVAL);
  /*
  if (!screen) {
	  Log ("Unable to set video mode: %s\n", SDL_GetError());
  	return false;
  }
  return true;
  */
  last_update = SDL_GetTicks ();

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SdlTerm ()
{

  SDL_Quit();

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SdlSwapBuffers ()
{

  

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SdlUpdate ()
{

  SDL_Event event;
  long      now;

  while (SDL_PollEvent(&event)) { 
    switch(event.type){ 
    case SDL_QUIT: 
      MainQuit ();
      break; 
    case SDL_KEYDOWN:
      if (event.key.keysym.sym == SDLK_ESCAPE)
        MainQuit ();
      InputKeyDown (event.key.keysym.sym);
      break;
    case SDL_KEYUP:
      InputKeyUp (event.key.keysym.sym);
      break;
    case SDL_MOUSEBUTTONDOWN:
      if (event.button.button == SDL_BUTTON_RIGHT) {
        InputMouselookSet (!InputMouselook ());
        SDL_ShowCursor (false);
        SDL_WM_GrabInput (SDL_GRAB_ON);
      }
      if (event.button.button == SDL_BUTTON_LEFT && !InputMouselook ())
        RenderClick (event.motion.x, event.motion.y);        
      break;
    case SDL_MOUSEBUTTONUP:
      if (event.button.button == SDL_BUTTON_LEFT)
        lmb = false;
      else if (event.button.button == SDL_BUTTON_MIDDLE)
        mmb = false;
      if (InputMouselook ())
        SDL_ShowCursor (false);
      else { 
        SDL_ShowCursor (true);
        SDL_WM_GrabInput (SDL_GRAB_OFF);
      }
      break;
    case SDL_MOUSEMOTION:
      if (InputMouselook ()) 
        AvatarLook (event.motion.yrel, -event.motion.xrel);
      break;
    case SDL_VIDEORESIZE: //User resized window
      center_x = event.resize.w / 2;
      center_y = event.resize.h / 2;
      RenderCreate (event.resize.w, event.resize.h, 32, false);
      break; 
    } //Finished with current event

  } //Done with all events for now
  now = SDL_GetTicks ();
  elapsed = now - last_update;
  elapsed_seconds = (float)elapsed / 1000.0f;
  last_update = now;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

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

  return SDL_GetTicks ();;

}