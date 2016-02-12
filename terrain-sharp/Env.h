
enum eEnvColor
{
  ENV_COLOR_HORIZON,
  ENV_COLOR_SKY,
  ENV_COLOR_FOG,
  ENV_COLOR_LIGHT,
  ENV_COLOR_AMBIENT,
  ENV_COLOR_COUNT
};

struct Range
{
  float   rmin;
  float   rmax;
};

struct Env
{
  GLrgba      color[ENV_COLOR_COUNT];
  GLvector    light;
  Range       fog;
  float       star_fade;
  float       sunrise_fade;
  float       sunset_fade;
  float       sun_angle;
  float       cloud_cover;
  bool        draw_sun;
};

void          EnvInit ();
void          EnvUpdate ();
Env*          EnvGet ();