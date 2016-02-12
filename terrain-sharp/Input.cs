/*-----------------------------------------------------------------------------
  Input.cpp
-------------------------------------------------------------------------------
  Track state of keyboard keys and mouse.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include "sdl.h"

void InputKeyDown (int id);
void InputKeyUp (int id);
bool InputKeyState (int id);

bool InputMouselook ();
void InputMouselookSet (bool val);
bool InputKeyPressed (int id);

void InputJoystickSet (int axis, int value);
float InputJoystickGet (int axis);

#define INPUT_MWHEEL_UP   510
#define INPUT_MWHEEL_DOWN 511

#define MAX_KEYS      512
#define MAX_AXIS      6

static bool   down[MAX_KEYS];
static bool   pressed[MAX_KEYS];
static bool   mouselook;
static bool   fly;
static float  jstick[MAX_AXIS];

void InputUpdate ()
{
}

void InputKeyDown (int id)
{
  if (id < 0 || id >= MAX_KEYS)
    return;
  if (!down[id])
    pressed[id] = true;
  down[id] = true;
}

void InputKeyUp (int id)
{
  if (id < 0 || id >= MAX_KEYS)
    return;
  down[id] = false;
}

bool InputKeyState (int id)
{
  if (id < 0 || id >= MAX_KEYS)
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

void InputJoystickSet (int axis, int value)
{
  if (axis < 0 || axis >= MAX_AXIS)
    return;
  jstick[axis] = (float)value / 32768.0f;
  if (abs (jstick[axis]) < 0.15f)//"dead zone"
    jstick[axis] = 0.0f;
}

float InputJoystickGet (int axis)
{
  if (axis < 0 || axis >= MAX_AXIS)
    return 0.0f;
  return jstick[axis];
}

bool InputMouselook ()
{
  return mouselook;
}

void InputMouselookSet (bool val)
{
  mouselook = val;
}
*/