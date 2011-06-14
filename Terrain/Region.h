#define REGION_SIZE       64
#define REGION_HALF       (REGION_SIZE / 2)
#define REGION_GRID       128
#define REGION_GRID_EDGE  (REGION_GRID + 1)
#define REGION_CENTER     (REGION_GRID / 2)

#define REGION_FLAG_TEST        0x0001
#define REGION_FLAG_MESAS       0x0002
#define REGION_FLAG_CRATER      0x0004
#define REGION_FLAG_BEACH       0x0008
#define REGION_FLAG_BEACH_CLIFF 0x0010
#define REGION_FLAG_SINKHOLE    0x0020
#define REGION_FLAG_CRACK       0x0040
#define REGION_FLAG_TIERED      0x0080
#define REGION_FLAG_CANYON_NS   0x0100
#define REGION_FLAG_NOBLEND     0x0200
#define REGION_FLAG_RIVERN      0x1000
#define REGION_FLAG_RIVERE      0x2000
#define REGION_FLAG_RIVERS      0x4000
#define REGION_FLAG_RIVERW      0x8000

#define REGION_FLAG_RIVERNS     (REGION_FLAG_RIVERN | REGION_FLAG_RIVERS)
#define REGION_FLAG_RIVEREW     (REGION_FLAG_RIVERE | REGION_FLAG_RIVERW)

#define REGION_FLAG_RIVER_ANY   0xf000

#define FLOWERS                 3

enum Climate
{
  CLIMATE_INVALID,
  CLIMATE_OCEAN,
  CLIMATE_COAST,
  CLIMATE_MOUNTAIN,
  CLIMATE_RIVER,
  CLIMATE_RIVER_BANK,
  CLIMATE_SWAMP,
  CLIMATE_TYPES,
};
//#define REGION_FLAG_DESERT      0x4000

struct Region
{
  char      title[50];
  GLcoord   grid_pos;
  int       mountain_height;
  int       river_id;
  int       river_segment;
  float     river_width;
  //float     elevation;
  float     geo_scale; //Number from -1 to 1, lowest to highest elevation
  float     geo_bias;
  float     geo_detail;
  float     geo_large;
  unsigned  flags_shape;
  Climate   climate;
  float     temperature;
  float     moisture;
  float     threshold;
  float     beach_threshold;
  GLrgba    color_map;
  GLrgba    color_rock;
  GLrgba    color_dirt;
  GLrgba    color_grass;
  GLrgba    color_atmosphere;
  GLrgba    color_flowers[FLOWERS];
  unsigned  flower_shape[FLOWERS];
  bool      has_flowers;
};

Region    RegionGet (int x, int y);
void      RegionSet (int x, int y, Region val);
Region    RegionGet (float x, float y);
GLrgba    RegionColorGet (int world_x, int world_y, SurfaceColor c);
GLrgba    RegionAtmosphere (int world_x, int world_y);
float     RegionElevation (int world_x, int world_y);
Cell      RegionCell (int world_x, int world_y);
void      RegionInit ();
unsigned  RegionMap ();
float     RegionWaterLevel (int world_x, int world_y);
void      RegionGenerate ();