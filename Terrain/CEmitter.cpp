/*-----------------------------------------------------------------------------

  CEmitter.cpp


-------------------------------------------------------------------------------

  The emitter class generates particle effects.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "console.h"
#include "cemitter.h"
#include "sdl.h"
#include "texture.h"
#include "world.h"

//Maximum number of particles in play from a single emitter - because botching
//emitter properties can create millions if you're not careful!
#define MAX_PARTICLES       1000

static UINT       cycler;

/*-----------------------------------------------------------------------------
                              
-----------------------------------------------------------------------------*/

void CEmitter::Emit (UINT count)
{

  Particle  p;
  GLvector  range;
  float     x, y;
  unsigned  i;
  unsigned  n;

  for (n = 0; n < count; n++) {
    range = _settings.volume.Size ();
    p._position.x = _settings.volume.pmin.x + range.x * WorldNoisef (cycler++);
    p._position.y = _settings.volume.pmin.y + range.y * WorldNoisef (cycler++);
    p._position.z = _settings.volume.pmin.z + range.z * WorldNoisef (cycler++);
    range = _settings.speed.Size ();
    p._velocity.x = _settings.speed.pmin.x + range.x * WorldNoisef (cycler++);
    p._velocity.y = _settings.speed.pmin.y + range.y * WorldNoisef (cycler++);
    p._velocity.z = _settings.speed.pmin.z + range.z * WorldNoisef (cycler++);
    p._base_color = _settings.colors[WorldNoisei (cycler++) % _settings.colors.size ()];
    range = _settings.size.Size ();
    range.x = _settings.size.pmin.x + range.x * WorldNoisef (cycler++);
    range.y = _settings.size.pmin.y + range.y * WorldNoisef (cycler++);
    range.z = _settings.size.pmin.z + range.z * WorldNoisef (cycler++);
    p._rotation = _settings.rotation;
    p._spin = _settings.spin;
    i = 0;
    for (x = -1; x <= 1; x += 2) {
      for (y = -1; y <= 1; y += 2) {
        switch (_settings.panel_type) {
        case PARTICLE_PANEL_X: 
          p._panel[i] = glVector (0.0f, x * range.y, y * range.z); 
          break;
        case PARTICLE_PANEL_Y: 
          p._panel[i] = glVector (x * range.x, 0.0f, y * range.z); 
          break;
        case PARTICLE_PANEL_Z: 
          p._panel[i] = glVector (x * range.x, y * range.y, 0.0f); 
          break;
        }
        p._panel[i] += _settings.origin;
        i++;
      }
    }
    p._dead = false;
    p._released = SdlTick ();
    if (_particle.size () < MAX_PARTICLES) //just make sure they don't get away from us
      _particle.push_back (p);
  }

}

void CEmitter::Set (ParticleSet* ps)
{

  _settings = *ps;
  if (_settings.colors.empty ())
    _settings.colors.push_back (glRgba (1.0f, 1.0f, 1.0f));
  _last_update = SdlTick ();
  _next_release = _last_update;
  if (_settings.emitter_lifespan) 
    _die = _last_update + _settings.emitter_lifespan;
  else
    _die = 0;
  _particle.clear ();
  _uv.Set (1.0f);
  _dead = false;

}

void CEmitter::Render ()
{

  unsigned      i;
  GLvector2     uv;

  glDepthMask (false);
  if (_settings.blend == PARTICLE_BLEND_ADD)
    glBlendFunc (GL_ONE, GL_ONE);
  else
    glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBindTexture (GL_TEXTURE_2D, 0);
  //_settings.volume.Render ();
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName (_settings.texture.c_str ()));

  glBegin (GL_QUADS);
  for (i = 0; i < _particle.size (); i++) {
    glColor4fv (&_particle[i]._draw_color.red);
    uv = _uv.Corner (0); glTexCoord2fv (&uv.x);
    glVertex3fv (&_particle[i]._vertex[0].x);
    uv = _uv.Corner (1); glTexCoord2fv (&uv.x);
    glVertex3fv (&_particle[i]._vertex[1].x);
    uv = _uv.Corner (2); glTexCoord2fv (&uv.x);
    glVertex3fv (&_particle[i]._vertex[3].x);
    uv = _uv.Corner (3); glTexCoord2fv (&uv.x);
    glVertex3fv (&_particle[i]._vertex[2].x);
  }
  glEnd ();
  glDepthMask (true);

}

void CEmitter::Update (float elapsed)
{

  UINT      now;
  UINT      i, v;
  UINT      fade;
  GLmatrix  m;
  float     alpha;

  now = SdlTick ();
  if (now >= _next_release && !_dead) {
    Emit (_settings.emit_count);
    _next_release = now + _settings.emit_interval;
  }
  for (i = 0; i < _particle.size (); i++) {
    _particle[i]._position += _particle[i]._velocity * elapsed;
    _particle[i]._velocity += _settings.acceleration * elapsed;
    if (_settings.gravity)
      _particle[i]._velocity.z -= GRAVITY * elapsed;
    _particle[i]._rotation += _particle[i]._spin * elapsed;
    fade = now - _particle[i]._released;
    if (fade > _settings.lifespan)
      _particle[i]._dead = true;
    alpha = 1.0f;
    if (fade < _settings.fade_in) 
      alpha = (float)fade / (float)_settings.fade_in;
    if (fade > _settings.lifespan - _settings.fade_out) {
      fade -= _settings.lifespan - _settings.fade_out;
      alpha = 1.0f - (float)fade / (float)_settings.fade_out;
    }
    if (_settings.blend == PARTICLE_BLEND_ADD)
      _particle[i]._draw_color = _particle[i]._base_color * alpha;
    else {
      _particle[i]._draw_color = _particle[i]._base_color;
      _particle[i]._draw_color.alpha = alpha;
    }
    m.Identity ();
    m.Rotate (_particle[i]._rotation.x, 1.0f, 0.0f, 0.0f);
    m.Rotate (_particle[i]._rotation.y, 0.0f, 1.0f, 0.0f);
    m.Rotate (_particle[i]._rotation.z, 0.0f, 0.0f, 1.0f);
    for (v = 0; v < 4; v++) 
      _particle[i]._vertex[v] = m.TransformPoint (_particle[i]._panel[v]) + _particle[i]._position;
  }
  if (_die &&_die < now)
    _dead = true;
  while (!_particle.empty () && _particle[0]._dead)
    _particle.erase (_particle.begin());
  _last_update = now;

}