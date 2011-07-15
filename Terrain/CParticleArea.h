#define PARTICLE_AREA_SIZE      64

#ifndef GRID
#include "cgrid.h"
#endif

enum
{
  PARTICLE_STAGE_BEGIN,
  PARTICLE_STAGE_DONE
};

class CParticleArea : public GridData
{

  int               _stage;
  vector<UINT>      _emitter;
  GLcoord           _origin;

  void              DoFog (GLcoord pos);
  void              DoSandStorm (GLcoord pos);
  void              DoWindFlower ();
  bool              ZoneCheck ();

public:
  unsigned          Sizeof () { return sizeof (CParticleArea); }; 
  void              Set (int x, int y, int distance);
  void              Render ();
  void              Update (long stop);
  bool              Ready () { return _stage == PARTICLE_STAGE_DONE; };
  void              Invalidate ();
};
