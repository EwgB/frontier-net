/*-----------------------------------------------------------------------------
  Console.cpp
-------------------------------------------------------------------------------
  This module runs the "quake-like" console, using the GLconsole 
  by Gabe Sibley: 
  
  http://www.robots.ox.ac.uk/~gsibley/GLConsole/
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include <stdarg.h>
#include "sdl.h"

void ConsoleInit ();
void ConsoleInput (int key, int char_code);
bool ConsoleIsOpen ();
void ConsoleLog (const char* message, ...);
void ConsoleRender ();
void ConsoleToggle ();
void ConsoleUpdate ();

#define MAX_MSG_LEN     1024  // 1k

static GLConsole      con;  
static bool           ready;
static vector<string> queue;

void ConsoleInit ()
{
  con.m_fOverlayPercent = 0.75f;
  con.SetHelpColor (0, 255, 255);
  //con._IsConsoleFunc (
}

void ConsoleToggle ()
{
  con.ToggleConsole();
  con.m_fOverlayPercent = 0.75f;
  con.SetHelpColor (0, 255, 255);
}

void ConsoleInput (int key, int char_code)
{
  if (key == SDLK_UP) {
    con.HistoryBack ();
    return;
  }
  if (key == SDLK_DOWN) {
    con.HistoryForward ();
    return;
  }
  if (key == SDLK_PAGEDOWN) {
    con.ScrollUpPage ();
    return;
  }
  if (key == SDLK_PAGEUP) {
    con.ScrollDownPage ();
    return;
  }
  if (key == SDLK_HOME) {
    con.CursorToBeginningOfLine ();
    return;
  }
  if (key == SDLK_END) {
    con.CursorToEndOfLine ();
    return;
  }
  if (key == SDLK_RSHIFT || key == SDLK_LSHIFT) 
    return;
  con.KeyboardFunc (char_code);
}

bool ConsoleIsOpen ()
{
  return con.IsOpen ();
}

void ConsoleRender ()
{
  if (!ConsoleIsOpen ())
    return;
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glDisable(GL_TEXTURE_2D);
//  &con.m_consoleColor.a
  //con.SetLogColor (1,1,1);
  con.RenderConsole();
}

void ConsoleUpdate ()
{
  unsigned    i;

  ready = true;
  for (i = 0; i < queue.size (); i++)
    con.EnterLogLine (queue[i].c_str (), LINEPROP_LOG, true);
  queue.clear ();
}

void ConsoleLog (const char* message, ...)
{
  static char    msg_text[MAX_MSG_LEN];
  va_list           marker;

  va_start (marker, message);
  vsprintf (msg_text, message, marker);
  va_end (marker);
  //If the console object isn't ready for input, just store these messages for later.
  if (!ready) {
    queue.push_back (msg_text);
    return;
  }
  con.EnterLogLine (msg_text, LINEPROP_LOG, true);
}
*/