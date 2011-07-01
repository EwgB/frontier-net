void InputKeyDown (int id);
void InputKeyUp (int id);
bool InputKeyState (int id);

bool InputMouselook ();
void InputMouselookSet (bool val);
bool InputKeyPressed (int id);

#define SDL_MWHEEL_UP   510
#define SDL_MWHEEL_DOWN 511