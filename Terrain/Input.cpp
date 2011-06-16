/*-----------------------------------------------------------------------------

  Input.cpp

-------------------------------------------------------------------------------

  Track state of keyboard keys and mouse.


-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "sdl.h"

#define max_KEYS      512

static bool   down[max_KEYS];
static bool   pressed[max_KEYS];
static bool   mouselook;
static bool   fly;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void InputUpdate ()
{


}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void InputKeyDown (int id)
{

  if (id < 0 || id >= max_KEYS)
    return;
  if (!down[id])
    pressed[id] = true;
  down[id] = true;

}

void InputKeyUp (int id)
{

  if (id < 0 || id >= max_KEYS)
    return;
  down[id] = false;

}

bool InputKeyState (int id)
{

  if (id < 0 || id >= max_KEYS)
    return false;
  return down[id];

}

bool InputKeyPressed (int id)
{

  bool      val;

  val = pressed[id];
  pressed[id] = false;
  return val;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

bool InputMouselook ()
{

  return mouselook;

}

void InputMouselookSet (bool val)
{

  mouselook = val;

}
