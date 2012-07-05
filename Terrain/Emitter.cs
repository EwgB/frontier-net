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
		public string Texture;
		public BBox Volume, Speed, Size;
		public Vector3 Acceleration, Origin, Rotation, Spin;
		public ParticleBlend Blend;
		public ParticleType PanelType;
		public int FadeIn, FadeOut, Lifespan, EmitInterval, EmitCount, EmitterLifespan;
		public bool Gravity, Wind, Interpolate, ZBuffer;
		public List<Color4> Colors;
	}

	struct Particle {
		public Vector3 Position, Rotation, Velocity, Spin;
		public Vector3[] Vertices;

		public Color4 BaseColor, DrawColor;
		public Color4[] Panels;
		
		public int Released;
		public bool IsDead;

		public Particle(Vector3 position, Vector3 velocity, Vector3 rotation, Vector3 spin, Color4 baseColor, bool isDead, int released) {
			Position	= position;
			Velocity	= velocity;
			Rotation	= rotation;
			Spin			= spin;
			BaseColor = baseColor;
			IsDead		= isDead;
			Released	= released;

			// DrawColor will be set properly later, at the blending stage
			DrawColor = Color4.White;

			Vertices = new Vector3[4];
			Panels = new Color4[4];
		}
	}
	#endregion

	class Emitter {
		#region Constants, member variables and properties
		//Maximum number of particles in play from a single emitter - because botching
		//emitter properties can create millions if you're not careful!
		private const int MAX_PARTICLES = 1000;

		private static int cycler, id_pool;

		private int mDie, mLastUpdate, mNextRelease;
		private ParticleSet mSettings;
		private UVBox mUV;
		private bool mDead;
		private List<Particle> mParticles;

		public int Id { get; private set; }
		public bool IsDead { get { return mDead && (mParticles.Count == 0); } }
		#endregion

		#region Methods
		public void Retire() { mDead = true; }

		public Emitter() { Id = ++id_pool; }

		private void Emit(int count) {
			for (int n = 0; n < Math.Min(count, MAX_PARTICLES); n++) {
				Vector3 range = mSettings.Volume.Size;
				Vector3 position = new Vector3(
					mSettings.Volume.pmin.X + range.X * FWorld.NoiseFloat(cycler++),
					mSettings.Volume.pmin.Y + range.Y * FWorld.NoiseFloat(cycler++),
					mSettings.Volume.pmin.Z + range.Z * FWorld.NoiseFloat(cycler++));
		
				range = mSettings.Speed.Size;
				Vector3 velocity = new Vector3(
					mSettings.Speed.pmin.X + range.X * FWorld.NoiseFloat(cycler++),
					mSettings.Speed.pmin.Y + range.Y * FWorld.NoiseFloat(cycler++),
					mSettings.Speed.pmin.Z + range.Z * FWorld.NoiseFloat(cycler++));

				range = mSettings.Size.Size;
				range.X = mSettings.Size.pmin.X + range.X * FWorld.NoiseFloat(cycler++);
				range.Y = mSettings.Size.pmin.Y + range.Y * FWorld.NoiseFloat(cycler++);
				range.Z = mSettings.Size.pmin.Z + range.Z * FWorld.NoiseFloat(cycler++);
				
				Particle p = new Particle(position, range, mSettings.Rotation, mSettings.Spin,
					mSettings.Colors[FWorld.NoiseInt(cycler++) % mSettings.Colors.Count],					// Base color
					false, SdlTick());

				for (int x = -1, i = 0; x <= 1; x += 2) {
					for (int y = -1; y <= 1; y += 2, i++) {
						switch (mSettings.PanelType) {
							case ParticleType.PanelX:
								p.Panels[i] = new Color4(0, x * range.Y, y * range.Z, 1);
								break;
							case ParticleType.PanelY:
								p.Panels[i] = new Color4(x * range.X, 0, y * range.Z, 1);
								break;
							case ParticleType.PanelZ:
								p.Panels[i] = new Color4(x * range.X, y * range.Y, 0, 1);
								break;
						}
						p.Panels[i].R += mSettings.Origin.X;
						p.Panels[i].G += mSettings.Origin.Y;
						p.Panels[i].B += mSettings.Origin.Z;
					}
				}

				mParticles.Add(p);
			}
		}

		public void Set(ParticleSet ps) {
			mSettings = ps;
			if (mSettings.Colors.Count == 0)
				mSettings.Colors.Add(Color4.White);
			mLastUpdate = SdlTick();
			mNextRelease = mLastUpdate;
			if (mSettings.EmitterLifespan != 0)
				mDie = mLastUpdate + mSettings.EmitterLifespan;
			else
				mDie = 0;
			mParticles.Clear();
			mUV.Set(1.0f);
			mDead = false;
		}

		public void RenderBbox() {
			GL.BindTexture(TextureTarget.Texture2D, 0);
			mSettings.Volume.Render();
		}

		public void Render() {
			GL.DepthMask(false);
			if (mSettings.Blend == ParticleBlend.Add)
				GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
			else
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			//glBindTexture (GL_TEXTURE_2D, 0);
			//_settings.Volume.Render ();
			GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName(mSettings.Texture));

			GL.Begin(BeginMode.Quads);

			Vector2     uv;
			for (int i = 0; i < mParticles.Count; i++) {
				GL.Color4(p._draw_color);
				uv = mUV.Corner(0);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[0]);
				uv = mUV.Corner(1);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[1]);
				uv = mUV.Corner(2);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[3]);
				uv = mUV.Corner(3);		GL.TexCoord2(uv);		GL.Vertex3(p._vertex[2]);
			}
			GL.End();
			GL.DepthMask(true);
		}

		public void Update(float elapsed) {
			int now = SdlTick();
			if (now >= mNextRelease && !mDead) {
				Emit(mSettings.EmitCount);
				mNextRelease = now + mSettings.EmitInterval;
			}

			for (int i = 0; i < mParticles.Count; i++) {
				Particle p = mParticles[i];
				p.Position += p.Velocity * elapsed;
				p.Velocity += mSettings.Acceleration * elapsed;
				if (mSettings.Gravity)
					p.Velocity.Z -= GRAVITY * elapsed;
				p.Rotation += p.Spin * elapsed;

				int fade = now - p.Released;
				float alpha = 1.0f;
				if (fade > mSettings.Lifespan)
					p.IsDead = true;
				if (fade < mSettings.FadeIn)
					alpha = (float) fade / (float) mSettings.FadeIn;
				if (fade > mSettings.Lifespan - mSettings.FadeOut) {
					fade -= mSettings.Lifespan - mSettings.FadeOut;
					alpha = 1.0f - (float) fade / (float) mSettings.FadeOut;
				}

				if (mSettings.Blend == ParticleBlend.Add)
					p.DrawColor = p.BaseColor * alpha;
				else {
					p.DrawColor = p.BaseColor;
					p.DrawColor.A = alpha;
				}
				Matrix4 m = Matrix4.CreateRotationX(p.Rotation.X);
				m = Matrix4.Rotate(Vector3.UnitY, p.Rotation.Y);
				m = Matrix4.Rotate(Vector3.UnitZ, p.Rotation.Z);
				for (int v = 0; v < 4; v++)
					p.Vertices[v] = m.TransformPoint(p.Panels[v]) + p.Position;
			}
			if ((mDie != 0) && (mDie < now))
				mDead = true;
			while ((mParticles.Count > 0) && mParticles[0].IsDead)
				mParticles.Remove(mParticles[0]);
			mLastUpdate = now;
		}
		#endregion
	}
}
