#define ENV_TRANSITION    0.2f

enum eEnvColor
{
  ENV_COLOR_NORTH,
  ENV_COLOR_SOUTH,
  ENV_COLOR_EAST,
  ENV_COLOR_WEST,
  ENV_COLOR_FOG,
  ENV_COLOR_LIGHT,
  ENV_COLOR_AMBIENT,
  ENV_COLOR_COUNT
};

void      EnvInit ();
void      EnvUpdate ();
GLrgba    EnvColor (eEnvColor type);
GLvector2 EnvFog ();
float     EnvStars ();
