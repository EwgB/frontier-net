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
			p.volume.pmin += position;
			p.volume.pmax += position;
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

			CVarUtils.SetCVar<string>("particle.texture", p.texture);
			CVarUtils.SetCVar("particle.acceleration", p.acceleration);
			CVarUtils.SetCVar("particle.blend", p.blend);
			CVarUtils.SetCVar("particle.emitter_lifespan", p.emitter_lifespan);
			CVarUtils.SetCVar("particle.emit_count", p.emit_count);
			CVarUtils.SetCVar("particle.emit_interval", p.emit_interval);
			CVarUtils.SetCVar("particle.fade_in", p.fade_in);
			CVarUtils.SetCVar("particle.fade_out", p.fade_out);
			CVarUtils.SetCVar("particle.interpolate", p.interpolate);
			CVarUtils.SetCVar("particle.lifespan", p.lifespan);
			CVarUtils.SetCVar("particle.origin", p.origin);
			CVarUtils.SetCVar("particle.panel_type", p.panel_type);
			CVarUtils.SetCVar("particle.rotation", p.rotation);
			CVarUtils.SetCVar("particle.size.min", p.size.pmin);
			CVarUtils.SetCVar("particle.size.max", p.size.pmax);
			CVarUtils.SetCVar("particle.speed.min", p.speed.pmin);
			CVarUtils.SetCVar("particle.speed.max", p.speed.pmax);
			CVarUtils.SetCVar("particle.spin", p.spin);
			CVarUtils.SetCVar("particle.volume.min", p.volume.pmin);
			CVarUtils.SetCVar("particle.volume.max", p.volume.pmax);
			CVarUtils.SetCVar<bool>("particle.wind", p.wind);
			CVarUtils.SetCVar<bool>("particle.gravity", p.gravity);
			CVarUtils.SetCVar<bool>("particle.z_buffer", p.z_buffer);
			CVarUtils.Save(filename, sub_group);
		}

		public static void ParticleLoad(string filename, ParticleSet p) {
			List<string> sub_group = new List<string>();
			filename = "particles//" += filename + ".prt";
			sub_group.Add("particle");
			CVarUtils.Load(filename, sub_group);
			p.colors.clear();
			p.texture = CVarUtils.GetCVar<string>("particle.texture");
			p.acceleration = CVarUtils.GetCVar<Vector3>("particle.acceleration");
			p.blend = (PBlend) CVarUtils.GetCVar<int>("particle.blend");
			p.emitter_lifespan = CVarUtils.GetCVar<int>("particle.emitter_lifespan");
			p.emit_count = CVarUtils.GetCVar<int>("particle.emit_count");
			p.emit_interval = CVarUtils.GetCVar<int>("particle.emit_interval");
			p.fade_in = CVarUtils.GetCVar<int>("particle.fade_in");
			p.fade_out = CVarUtils.GetCVar<int>("particle.fade_out");
			p.interpolate = CVarUtils.GetCVar<bool>("particle.interpolate");
			p.lifespan = CVarUtils.GetCVar<int>("particle.lifespan");
			p.origin = CVarUtils.GetCVar<Vector3>("particle.origin");
			p.panel_type = (PType) CVarUtils.GetCVar<int>("particle.panel_type");
			p.rotation = CVarUtils.GetCVar<Vector3>("particle.rotation");
			p.size.pmin = CVarUtils.GetCVar<Vector3>("particle.size.min");
			p.size.pmax = CVarUtils.GetCVar<Vector3>("particle.size.max");
			p.speed.pmin = CVarUtils.GetCVar<Vector3>("particle.speed.min");
			p.speed.pmax = CVarUtils.GetCVar<Vector3>("particle.speed.max");
			p.spin = CVarUtils.GetCVar<Vector3>("particle.spin");
			p.volume.pmin = CVarUtils.GetCVar<Vector3>("particle.volume.min");
			p.volume.pmax = CVarUtils.GetCVar<Vector3>("particle.volume.max");
			p.wind = CVarUtils.GetCVar<bool>("particle.wind");
			p.gravity = CVarUtils.GetCVar<bool>("particle.gravity");
			p.z_buffer = CVarUtils.GetCVar<bool>("particle.z_buffer");
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
