using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	#region Structs and enums
	enum ParticleBlend { Alpha, Add }
	enum ParticleType { Facer, PanelX, PanelY, PanelZ }

	struct ParticleSet {
		public string texture;
		public BBox volume, speed, size;
		public Vector3 acceleration, origin, rotation, spin;
		public ParticleBlend blend;
		public ParticleType  panel_type;
		public int fade_in, fade_out, lifespan, emit_interval, emit_count, emitter_lifespan;
		public bool gravity, wind, interpolate, z_buffer;
		public List<Color4> colors;
	}

	struct Particle {
		public Vector3 _position, _rotation, _velocity, _spin;
		public Vector3[] _vertex = new Vector3[4];
		public Color4 _base_color, _draw_color;
		public Color4[] _panel = new Color4[4];
		public int _released;
		public bool _dead;
	}
	#endregion

	class Emitter {
		#region Constants, member variables and properties
		//Maximum number of particles in play from a single emitter - because botching
		//emitter properties can create millions if you're not careful!
		private const int MAX_PARTICLES = 1000;

		private static int cycler, id_pool;

		private int _die, _last_update, _next_release;
		private ParticleSet _settings;
		private UVBox _uv;
		private bool _dead;
		private List<Particle> _particle;

		public int Id { get; private set; }
		public bool IsDead { get { return _dead && (_particle.Count == 0); } }
		#endregion

		#region Methods
		public void Retire() { _dead = true; }

		public Emitter() { Id = ++id_pool; }

		private void Emit(int count) {
			for (int n = 0; n < count; n++) {
				Particle  p;

				Vector3 range = _settings.volume.Size;
				p._position.X = _settings.volume.pmin.X + range.X * WorldNoisef(cycler++);
				p._position.Y = _settings.volume.pmin.Y + range.Y * WorldNoisef(cycler++);
				p._position.Z = _settings.volume.pmin.Z + range.Z * WorldNoisef(cycler++);
		
				range = _settings.speed.Size;
				p._velocity.X = _settings.speed.pmin.X + range.X * WorldNoisef(cycler++);
				p._velocity.Y = _settings.speed.pmin.Y + range.Y * WorldNoisef(cycler++);
				p._velocity.Z = _settings.speed.pmin.Z + range.Z * WorldNoisef(cycler++);
				p._base_color = _settings.colors[WorldNoisei(cycler++) % _settings.colors.size()];
				
				range = _settings.size.Size;
				range.X = _settings.size.pmin.X + range.X * WorldNoisef(cycler++);
				range.Y = _settings.size.pmin.Y + range.Y * WorldNoisef(cycler++);
				range.Z = _settings.size.pmin.Z + range.Z * WorldNoisef(cycler++);
				
				p._rotation = _settings.rotation;
				p._spin = _settings.spin;
				
				int i = 0;
				for (int x = -1; x <= 1; x += 2) {
					for (int y = -1; y <= 1; y += 2, i++) {
						switch (_settings.panel_type) {
							case ParticleType.PanelX:
								p._panel[i] = new Color4(0, x * range.Y, y * range.Z, 1);
								break;
							case ParticleType.PanelY:
								p._panel[i] = new Color4(x * range.X, 0, y * range.Z, 1);
								break;
							case ParticleType.PanelZ:
								p._panel[i] = new Color4(x * range.X, y * range.Y, 0, 1);
								break;
						}
						p._panel[i].R += _settings.origin.X;
						p._panel[i].G += _settings.origin.Y;
						p._panel[i].B += _settings.origin.Z;
					}
				}
				p._dead = false;
				p._released = SdlTick();
				if (_particle.Count < MAX_PARTICLES) //just make sure they don't get away from us
					_particle.Add(p);
			}
		}

		public void Set(ParticleSet ps) {
			_settings = ps;
			if (_settings.colors.Count == 0)
				_settings.colors.Add(Color4.White);
			_last_update = SdlTick();
			_next_release = _last_update;
			if (_settings.emitter_lifespan != 0)
				_die = _last_update + _settings.emitter_lifespan;
			else
				_die = 0;
			_particle.Clear();
			_uv.Set(1.0f);
			_dead = false;
		}

		public void RenderBbox() {
			GL.BindTexture(TextureTarget.Texture2D, 0);
			_settings.volume.Render();
		}

		public void Render() {
			GL.DepthMask(false);
			if (_settings.blend == ParticleBlend.Add)
				GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
			else
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			//glBindTexture (GL_TEXTURE_2D, 0);
			//_settings.volume.Render ();
			GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName(_settings.texture));

			GL.Begin(BeginMode.Quads);

			Vector2     uv;
			for (int i = 0; i < _particle.Count; i++) {
				GL.Color4(p._draw_color);
				uv = _uv.Corner(0);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[0]);
				uv = _uv.Corner(1);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[1]);
				uv = _uv.Corner(2);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[3]);
				uv = _uv.Corner(3);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[2]);
			}
			GL.End();
			GL.DepthMask(true);
		}

		public void Update(float elapsed) {
			int now = SdlTick();
			if (now >= _next_release && !_dead) {
				Emit(_settings.emit_count);
				_next_release = now + _settings.emit_interval;
			}

			for (int i = 0; i < _particle.Count; i++) {
				Particle p = _particle[i];
				p._position += p._velocity * elapsed;
				p._velocity += _settings.acceleration * elapsed;
				if (_settings.gravity)
					p._velocity.Z -= GRAVITY * elapsed;
				p._rotation += p._spin * elapsed;

				int fade = now - p._released;
				float alpha = 1.0f;
				if (fade > _settings.lifespan)
					p._dead = true;
				if (fade < _settings.fade_in)
					alpha = (float) fade / (float) _settings.fade_in;
				if (fade > _settings.lifespan - _settings.fade_out) {
					fade -= _settings.lifespan - _settings.fade_out;
					alpha = 1.0f - (float) fade / (float) _settings.fade_out;
				}

				if (_settings.blend == ParticleBlend.Add)
					p._draw_color = p._base_color * alpha;
				else {
					p._draw_color = p._base_color;
					p._draw_color.A = alpha;
				}
				Matrix4 m = Matrix4.CreateRotationX(p._rotation.X);
				m = Matrix4.Rotate(Vector3.UnitY, p._rotation.Y);
				m = Matrix4.Rotate(Vector3.UnitZ, p._rotation.Z);
				for (int v = 0; v < 4; v++)
					p._vertex[v] = m.TransformPoint(p._panel[v]) + p._position;
			}
			if ((_die != 0) && (_die < now))
				_dead = true;
			while ((_particle.Count > 0) && _particle[0]._dead)
				_particle.Remove(_particle[0]);
			_last_update = now;
		}
		#endregion
	}
}
