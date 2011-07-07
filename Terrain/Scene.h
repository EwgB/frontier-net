enum 
{
  DEBUG_RENDER_NONE,
  DEBUG_RENDER_MOIST,
  DEBUG_RENDER_TEMP,
  DEBUG_RENDER_UNIQUE,
  DEBUG_RENDER_TYPES
};


void            SceneClear ();
void            SceneInit ();
void            SceneGenerate ();
void            SceneUpdate (long stop);
void            SceneProgress (unsigned* ready, unsigned* total);
void            SceneRender ();
void            SceneRenderDebug ();
void            SceneRestartProgress ();
class CTerrain* SceneTerrainGet (int x, int y);
void            SceneTexturePurge ();
float           SceneVisibleRange ();
