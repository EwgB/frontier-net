#include "cemitter.h"

UINT ParticleAdd (ParticleSet* p_in, GLvector position);
bool ParticleCmd (vector<string> *args);
void ParticleDestroy (UINT id);
void ParticleInit ();
void ParticleLoad (const char* filename_in, struct ParticleSet* p);
void ParticleRender ();
void ParticleRetire (UINT id);
void ParticleSave (char* filename_in, struct ParticleSet* p);
void ParticleUpdate ();
