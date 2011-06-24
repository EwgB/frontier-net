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
#define REGION_FLAG_RIVERNW     (REGION_FLAG_RIVERN | REGION_FLAG_RIVERW)
#define REGION_FLAG_RIVERSE     (REGION_FLAG_RIVERS | REGION_FLAG_RIVERE)
#define REGION_FLAG_RIVERNE     (REGION_FLAG_RIVERN | REGION_FLAG_RIVERE)
#define REGION_FLAG_RIVERSW     (REGION_FLAG_RIVERS | REGION_FLAG_RIVERW)
#define REGION_FLAG_RIVER_ANY   (REGION_FLAG_RIVERNS | REGION_FLAG_RIVEREW)

#define REGION_SIZE             64
#define REGION_HALF             (REGION_SIZE / 2)
#define WORLD_GRID              512
#define WORLD_GRID_EDGE         (WORLD_GRID + 1)
#define WORLD_GRID_CENTER       (WORLD_GRID / 2)
#define WORLD_SIZE_METERS       (REGION_SIZE * WORLD_GRID)

#define FLOWERS           3
//We keep a list of random numbers so we can have deterministic "randomness". 
//This is the size of that list.
#define NOISE_BUFFER      1024              
//This is the size of the grid of trees.  The total number of tree species 
//in the world is the square of this value, minus one. ("tree zero" is actually
//"no trees at all".)
#define TREE_TYPES        8

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
  CLIMATE_FOREST,
  CLIMATE_TYPES,
};


struct Region
{
  char      title[50];
  unsigned  tree_type;
  unsigned  flags_shape;
  Climate   climate;
  GLcoord   grid_pos;
  int       mountain_height;
  int       river_id;
  int       river_segment;
  float     tree_threshold;
  float     river_width;
  float     geo_scale; //Number from -1 to 1, lowest to highest elevation. 0 is sea level
  float     geo_water;
  float     geo_detail;
  float     geo_bias;
  float     temperature;
  float     moisture;
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

//Only one of these is ever instanced.  This is everything that goes into a "save file".
//Using only this, the entire world can be re-created.
struct World
{
  bool         wind_from_west;
  bool         northern_hemisphere;
  unsigned     river_count;
  float        noisef[NOISE_BUFFER];
  unsigned     noisei[NOISE_BUFFER];
  Region       map[WORLD_GRID][WORLD_GRID];
};


Cell          WorldCell (int world_x, int world_y);
GLrgba        WorldColorGet (int world_x, int world_y, SurfaceColor c);
char*         WorldLocationName (int world_x, int world_y);
Region        WorldRegionFromPosition (int world_x, int world_y);
Region        WorldRegionFromPosition (int world_x, int world_y);
float         WorldWaterLevel (int world_x, int world_y);

void          WorldGenerate ();
unsigned      WorldCanopyTree ();
void          WorldInit ();
unsigned      WorldMap ();
unsigned      WorldNoisei (int index);
float         WorldNoisef (int index);
Region        WorldRegionGet (int index_x, int index_y);
void          WorldRegionSet (int index_x, int index_y, Region val);
unsigned      WorldTreeType (float moisture, float temperature);
class CTree*  WorldTree (unsigned id);
World*        WorldPtr ();
