/*-----------------------------------------------------------------------------
  Particle.cpp
-------------------------------------------------------------------------------
  This manages the list of active particle emitters.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Frontier {
	class Particles {
		private static List<Emitter> elist;

		//For debugging.  Place an emitter at our feet
		public static void place_emitter() {
			ParticleSet p;
			Vector3     v;
			string      name;

			name = CVarUtils.GetCVar<string>("current_particle");
			v = Avatar.Position;
			ParticleLoad(name, p);
			ParticleSave("test", p);
			ParticleAdd(p, v);
		}

		public static int ParticleAdd(ParticleSet p, Vector3 position) {
			p.Volume.pmin += position;
			p.Volume.pmax += position;
			elist.Capacity = elist.Count + 1;
			Emitter e = elist[elist.Count - 1];
			e.Set(p);
			return e.Id;
		}

		public static void ParticleDestroy(int id) {
			for (int i = elist.Count - 1; i >= 0 ; i--) {
				if (elist[i].Id == id) {
					ConsoleLog("ParticleDestroy: Removing effect #%d.", id);
					elist.Remove(elist[i]);
					return;
				}
			}
			ConsoleLog("ParticleDestroy: Effect #%d not found.", id);
		}

		public static void ParticleRetire(int id) {
			for (int i = 0; i < elist.Count; i++) {
				if (elist[i].Id == id) {
					ConsoleLog("ParticleRetire: Disabling effect #%d.", id);
					elist[i].Retire();
					return;
				}
			}
			ConsoleLog("ParticleRetire: Effect #%d not found.", id);
		}

		public static void ParticleInit() {
			CVarUtils.CreateCVar<string>("particle.texture", "");
			CVarUtils.CreateCVar("particle.acceleration", Vector3.Zero);
			CVarUtils.CreateCVar("particle.blend", 0);
			CVarUtils.CreateCVar("particle.emitter_lifespan", 0);
			CVarUtils.CreateCVar("particle.emit_count", 0);
			CVarUtils.CreateCVar("particle.emit_interval", 0);
			CVarUtils.CreateCVar("particle.fade_in", 0);
			CVarUtils.CreateCVar("particle.fade_out", 0);
			CVarUtils.CreateCVar("particle.interpolate", 0);
			CVarUtils.CreateCVar("particle.lifespan", 0);
			CVarUtils.CreateCVar("particle.origin", Vector3.Zero);
			CVarUtils.CreateCVar("particle.panel_type", 0);
			CVarUtils.CreateCVar("particle.rotation", Vector3.Zero);
			CVarUtils.CreateCVar("particle.size.min", Vector3.Zero);
			CVarUtils.CreateCVar("particle.size.max", Vector3.Zero);
			CVarUtils.CreateCVar("particle.speed.min", Vector3.Zero);
			CVarUtils.CreateCVar("particle.speed.max", Vector3.Zero);
			CVarUtils.CreateCVar("particle.spin", Vector3.Zero);
			CVarUtils.CreateCVar("particle.volume.min", Vector3.Zero);
			CVarUtils.CreateCVar("particle.volume.max", Vector3.Zero);
			CVarUtils.CreateCVar<bool>("particle.wind", false);
			CVarUtils.CreateCVar<bool>("particle.gravity", false);
			CVarUtils.CreateCVar<bool>("particle.z_buffer", false);

			CVarUtils.CreateCVar<string>("current_particle", "");
		}

		public static void ParticleUpdate() {
			for (int i = 0; i < elist.Count; i++)
				elist[i].Update(SdlElapsedSeconds());

			for (int i = 0; i < elist.Count; i++) {
				if (elist[i].IsDead) {
					elist.Remove(elist[i]);
					break;
				}
			}
			if (InputKeyPressed(SDLK_f))
				place_emitter();
		}

		public static void ParticleRender() {
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Lighting);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			for (int i = 0; i < elist.Count; i++)
				elist[i].Render();
		}

		public static void ParticleSave(string filename, ParticleSet p) {
			filename = "particles//" + filename + ".prt";
			List<string> sub_group = new List<string>();
			sub_group.Add("particle");

			CVarUtils.SetCVar<string>("particle.texture", p.Texture);
			CVarUtils.SetCVar("particle.acceleration", p.Acceleration);
			CVarUtils.SetCVar("particle.blend", p.Blend);
			CVarUtils.SetCVar("particle.emitter_lifespan", p.EmitterLifespan);
			CVarUtils.SetCVar("particle.emit_count", p.EmitCount);
			CVarUtils.SetCVar("particle.emit_interval", p.EmitInterval);
			CVarUtils.SetCVar("particle.fade_in", p.FadeIn);
			CVarUtils.SetCVar("particle.fade_out", p.FadeOut);
			CVarUtils.SetCVar("particle.interpolate", p.Interpolate);
			CVarUtils.SetCVar("particle.lifespan", p.Lifespan);
			CVarUtils.SetCVar("particle.origin", p.Origin);
			CVarUtils.SetCVar("particle.panel_type", p.PanelType);
			CVarUtils.SetCVar("particle.rotation", p.Rotation);
			CVarUtils.SetCVar("particle.size.min", p.Size.pmin);
			CVarUtils.SetCVar("particle.size.max", p.Size.pmax);
			CVarUtils.SetCVar("particle.speed.min", p.Speed.pmin);
			CVarUtils.SetCVar("particle.speed.max", p.Speed.pmax);
			CVarUtils.SetCVar("particle.spin", p.Spin);
			CVarUtils.SetCVar("particle.volume.min", p.Volume.pmin);
			CVarUtils.SetCVar("particle.volume.max", p.Volume.pmax);
			CVarUtils.SetCVar<bool>("particle.wind", p.Wind);
			CVarUtils.SetCVar<bool>("particle.gravity", p.Gravity);
			CVarUtils.SetCVar<bool>("particle.z_buffer", p.ZBuffer);
			CVarUtils.Save(filename, sub_group);
		}

		public static void ParticleLoad(string filename, ParticleSet p) {
			List<string> sub_group = new List<string>();
			filename = "particles//" += filename + ".prt";
			sub_group.Add("particle");
			CVarUtils.Load(filename, sub_group);
			p.Colors.clear();
			p.Texture = CVarUtils.GetCVar<string>("particle.texture");
			p.Acceleration = CVarUtils.GetCVar<Vector3>("particle.acceleration");
			p.Blend = (PBlend) CVarUtils.GetCVar<int>("particle.blend");
			p.EmitterLifespan = CVarUtils.GetCVar<int>("particle.emitter_lifespan");
			p.EmitCount = CVarUtils.GetCVar<int>("particle.emit_count");
			p.EmitInterval = CVarUtils.GetCVar<int>("particle.emit_interval");
			p.FadeIn = CVarUtils.GetCVar<int>("particle.fade_in");
			p.FadeOut = CVarUtils.GetCVar<int>("particle.fade_out");
			p.Interpolate = CVarUtils.GetCVar<bool>("particle.interpolate");
			p.Lifespan = CVarUtils.GetCVar<int>("particle.lifespan");
			p.Origin = CVarUtils.GetCVar<Vector3>("particle.origin");
			p.PanelType = (PType) CVarUtils.GetCVar<int>("particle.panel_type");
			p.Rotation = CVarUtils.GetCVar<Vector3>("particle.rotation");
			p.Size.pmin = CVarUtils.GetCVar<Vector3>("particle.size.min");
			p.Size.pmax = CVarUtils.GetCVar<Vector3>("particle.size.max");
			p.Speed.pmin = CVarUtils.GetCVar<Vector3>("particle.speed.min");
			p.Speed.pmax = CVarUtils.GetCVar<Vector3>("particle.speed.max");
			p.Spin = CVarUtils.GetCVar<Vector3>("particle.spin");
			p.Volume.pmin = CVarUtils.GetCVar<Vector3>("particle.volume.min");
			p.Volume.pmax = CVarUtils.GetCVar<Vector3>("particle.volume.max");
			p.Wind = CVarUtils.GetCVar<bool>("particle.wind");
			p.Gravity = CVarUtils.GetCVar<bool>("particle.gravity");
			p.ZBuffer = CVarUtils.GetCVar<bool>("particle.z_buffer");
		}

		public static bool ParticleCmd(List<string> args) {
			if (args.Count == 0) {
				ConsoleLog(CVarUtils.GetHelp("game").data());
				return true;
			}
			CVarUtils.SetCVar<string>("current_particle", args[0]);
			place_emitter();
			return true;
		}
	}
}
