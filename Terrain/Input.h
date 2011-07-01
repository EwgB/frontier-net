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