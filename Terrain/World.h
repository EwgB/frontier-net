float       WorldElevation (int x, int y);
float       WorldDetail (int x, int y);
float       WorldElevation (float x, float y);
SurfaceType WorldSurface (int x, int y);
GLvector    WorldPosition (int x, int y);
void        WorldUpdate (long stop);
void        WorldRenderDebug ();
bool        WorldPointAvailable (int x, int y);
GLrgba      WorldSurfaceColor (int x, int y, SurfaceColor sc);
void        WorldUpdateZone (int world_x, int world_y, long stop);
void        WorldPurge ();