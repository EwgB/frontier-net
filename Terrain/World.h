#define REGION_SIZE             64
#define REGION_HALF             (REGION_SIZE / 2)
#define WORLD_GRID              128
#define WORLD_GRID_EDGE         (WORLD_GRID + 1)
#define WORLD_GRID_CENTER       (WORLD_GRID / 2)
#define WORLD_SIZE_METERS       (REGION_SIZE * WORLD_GRID)

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
#define REGION_FLAG_RIVER_ANY   (REGION_FLAG_RIVERNS | REGION_FLAG_RIVEREW)

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
  CLIMATE_ROCKY,
  CLIMATE_FIELD,
  CLIMATE_CANYON,
  CLIMATE_TYPES,
};


struct Region
{
  char      title[50];
  GLcoord   grid_pos;
  int       mountain_height;
  int       river_id;
  int       river_segment;
  float     river_width;
  float     geo_scale; //Number from -1 to 1, lowest to highest elevation. 0 is sea level
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

Cell      WorldCell (int world_x, int world_y);
GLrgba    WorldColorGet (int world_x, int world_y, SurfaceColor c);
void      WorldGenerate ();
void      WorldInit ();
unsigned  WorldMap ();
unsigned  WorldNoisei (int index);Region    WorldRegionGet (int index_x, int index_y);
float     WorldNoisef (int index);
void      WorldRegionSet (int index_x, int index_y, Region val);
Region    WorldRegionFromPosition (int world_x, int world_y);
Region    WorldRegionFromPosition (float world_x, float world_y);
float     WorldWaterLevel (int world_x, int world_y);
