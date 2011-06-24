#define PAGE_SIZE       64
#define PAGE_HALF       (PAGE_SIZE / 2)
#define PAGE_EXPIRE     30000 //milliseconds
#define TREE_SPACING    8 //Power of 2, how far apart trees should be. (Roughly)
#define TREE_MAP        (PAGE_SIZE / TREE_SPACING)

enum
{
  PAGE_STAGE_BEGIN,
  PAGE_STAGE_POSITION,
  PAGE_STAGE_NORMAL,
  PAGE_STAGE_SURFACE1,
  PAGE_STAGE_SURFACE2,
  PAGE_STAGE_COLOR,
  PAGE_STAGE_TREES,
  PAGE_STAGE_DONE
};

struct pcell
{
  SurfaceType surface;
  float       water_level;
  float       detail;
  GLrgba      grass;
  GLrgba      rock;
  GLrgba      dirt;
  GLvector    normal;
  GLvector    pos;
  unsigned    tree_id;
};

class CPage
{
  GLcoord         _origin;
  GLcoord         _walk;
  int             _stage;
  pcell           _cell[PAGE_SIZE][PAGE_SIZE];
  GLbbox          _bbox;
  int             _last_touched;

  void            DoTrees ();
  void            DoPosition ();
  void            DoSurface ();
  void            DoColor ();
  void            DoNormal ();
public:
  void            Cache (int origin_x, int origin_y);
  float           Elevation (int x, int y);
  float           Detail (int x, int y);
  GLvector        Position (int x, int y);
  GLvector        Normal (int x, int y);
  unsigned        Tree (int x, int y);
  GLrgba          ColorGrass (int x, int y);
  GLrgba          ColorDirt (int x, int y);
  GLrgba          ColorRock (int x, int y);
  SurfaceType     Surface (int x, int y);
  void            Build (int stop);
  void            Render ();
  bool            Ready ();
  bool            Expired ();
};
