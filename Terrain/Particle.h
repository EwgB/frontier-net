#include "cemitter.h"

void ParticleAdd (ParticleSet* p_in, GLvector position);
bool ParticleCmd (vector<string> *args);
void ParticleInit ();
void ParticleLoad (const char* filename_in, struct ParticleSet* p);
void ParticleRender ();
void ParticleSave (char* filename_in, struct ParticleSet* p);
void ParticleUpdate ();
