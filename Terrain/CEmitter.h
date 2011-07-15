enum PBlend
{
  PARTICLE_BLEND_ALPHA,
  PARTICLE_BLEND_ADD,
};

enum PType
{
  PARTICLE_FACER,
  PARTICLE_PANEL_X,
  PARTICLE_PANEL_Y,
  PARTICLE_PANEL_Z
};

struct ParticleSet
{
  string            texture;
  GLbbox            volume;
  GLbbox            speed;
  GLvector          acceleration;
  GLbbox            size;
  GLvector          origin;
  GLvector          rotation;
  GLvector          spin;
  PBlend            blend;
  PType             panel_type;
  UINT              fade_in;
  UINT              fade_out;
  UINT              lifespan;
  UINT              emit_interval;
  UINT              emit_count;
  UINT              emitter_lifespan;
  bool              gravity;
  bool              wind;
  bool              interpolate;
  bool              z_buffer;
  vector<GLrgba>    colors;
};

struct Particle
{
  GLvector          _position;
  GLvector          _rotation;
  GLvector          _velocity;
  GLvector          _spin;
  GLrgba            _base_color;
  GLrgba            _draw_color;
  GLvector          _panel[4];
  GLvector          _vertex[4];
  UINT              _released;
  bool              _dead;
};

class CEmitter 
{
  UINT              _id;
  ParticleSet       _settings;
  UINT              _die;
  UINT              _last_update;
  UINT              _next_release;
  GLuvbox           _uv;
  bool              _dead;
  vector<Particle>  _particle;
  
  void              Emit (UINT number);

public:
  CEmitter ();
  UINT              Id () { return _id; };
  void              Set (ParticleSet* s);
  void              Update (float elapsed);
  void              Render ();
  void              RenderBbox ();
  bool              Dead () { return _dead && _particle.empty (); };

};
