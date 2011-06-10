enum 
{
  DEBUG_RENDER_NONE,
  DEBUG_RENDER_MOIST,
  DEBUG_RENDER_TEMP,
  DEBUG_RENDER_UNIQUE,
  DEBUG_RENDER_TYPES
};


void            SceneTexturePurge ();
void            SceneInit ();
void            SceneUpdate (long stop);
void            SceneRender ();
void            SceneRenderDebug (int style);
class CTerrain*  SceneTerrainGet (int x, int y);
