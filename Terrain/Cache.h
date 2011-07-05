//Module functions
void CachePurge ();
void CacheRenderDebug ();
void CacheUpdate (long stop);
void CacheUpdatePage (int world_x, int world_y, long stop);

//Look up individual cell data


float       CacheDetail (int world_x, int world_y);
bool        CacheDump (vector<string> *args);
float       CacheElevation (int world_x, int world_y);
float       CacheElevation (float x, float y);
GLvector    CacheNormal (int world_x, int world_y);
bool        CachePointAvailable (int world_x, int world_y);
GLvector    CachePosition (int world_x, int world_y);
bool        CacheSize (vector<string> *args);
SurfaceType CacheSurface (int world_x, int world_y);
GLrgba      CacheSurfaceColor (int world_x, int world_y, SurfaceColor sc);
unsigned    CacheTree (int world_x, int world_y);