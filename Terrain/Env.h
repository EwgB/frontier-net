
enum eEnvColor
{
  ENV_COLOR_NORTH,
  ENV_COLOR_SOUTH,
  ENV_COLOR_EAST,
  ENV_COLOR_WEST,
  ENV_COLOR_TOP,
  ENV_COLOR_FOG,
  ENV_COLOR_LIGHT,
  ENV_COLOR_AMBIENT,
  ENV_COLOR_COUNT
};

struct Env
{
  GLrgba      color[ENV_COLOR_COUNT];
  GLvector    light;
  float       fog_min;
  float       fog_max;
  float       star_fade;
};

void      EnvInit ();
void      EnvUpdate ();
Env*      EnvGet ();