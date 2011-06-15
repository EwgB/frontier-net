
enum eEnvColor
{
  ENV_COLOR_HORIZON,
  ENV_COLOR_SKY,
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