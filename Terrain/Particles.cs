/*-----------------------------------------------------------------------------
  Particle.cpp
-------------------------------------------------------------------------------
  This manages the list of active particle emitters.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using CVars;

namespace Frontier {
	class Particles {
		private static List<Emitter> elist;

		//For debugging.  Place an emitter at our feet
		public static void place_emitter() {
			ParticleSet p;
			Vector3     v;
			string      name;

			name = CVars.GetCVar<string>("current_particle");
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
			Console.WriteLine("ParticleRetire: Effect #{0} not found.", id);
		}

		public static void ParticleInit() {
			CVars.CVars.CreateCVar("particle.texture", "");
			CVars.CVars.CreateCVar("particle.acceleration", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.blend", 0);
			CVars.CVars.CreateCVar("particle.emitter_lifespan", 0);
			CVars.CVars.CreateCVar("particle.emit_count", 0);
			CVars.CVars.CreateCVar("particle.emit_interval", 0);
			CVars.CVars.CreateCVar("particle.fade_in", 0);
			CVars.CVars.CreateCVar("particle.fade_out", 0);
			CVars.CVars.CreateCVar("particle.interpolate", 0);
			CVars.CVars.CreateCVar("particle.lifespan", 0);
			CVars.CVars.CreateCVar("particle.origin", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.panel_type", 0);
			CVars.CVars.CreateCVar("particle.rotation", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.size.min", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.size.max", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.speed.min", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.speed.max", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.spin", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.volume.min", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.volume.max", Vector3.Zero);
			CVars.CVars.CreateCVar("particle.wind", false);
			CVars.CVars.CreateCVar("particle.gravity", false);
			CVars.CVars.CreateCVar("particle.z_buffer", false);

			CVars.CVars.CreateCVar("current_particle", "");
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

			CVars.CVars.SetCVar<string>("particle.texture", p.Texture);
			CVars.CVars.SetCVar("particle.acceleration", p.Acceleration);
			CVars.CVars.SetCVar("particle.blend", p.Blend);
			CVars.CVars.SetCVar("particle.emitter_lifespan", p.EmitterLifespan);
			CVars.CVars.SetCVar("particle.emit_count", p.EmitCount);
			CVars.CVars.SetCVar("particle.emit_interval", p.EmitInterval);
			CVars.CVars.SetCVar("particle.fade_in", p.FadeIn);
			CVars.CVars.SetCVar("particle.fade_out", p.FadeOut);
			CVars.CVars.SetCVar("particle.interpolate", p.Interpolate);
			CVars.CVars.SetCVar("particle.lifespan", p.Lifespan);
			CVars.CVars.SetCVar("particle.origin", p.Origin);
			CVars.CVars.SetCVar("particle.panel_type", p.PanelType);
			CVars.CVars.SetCVar("particle.rotation", p.Rotation);
			CVars.CVars.SetCVar("particle.size.min", p.Size.pmin);
			CVars.CVars.SetCVar("particle.size.max", p.Size.pmax);
			CVars.CVars.SetCVar("particle.speed.min", p.Speed.pmin);
			CVars.CVars.SetCVar("particle.speed.max", p.Speed.pmax);
			CVars.CVars.SetCVar("particle.spin", p.Spin);
			CVars.CVars.SetCVar("particle.volume.min", p.Volume.pmin);
			CVars.CVars.SetCVar("particle.volume.max", p.Volume.pmax);
			CVars.CVars.SetCVar<bool>("particle.wind", p.Wind);
			CVars.CVars.SetCVar<bool>("particle.gravity", p.Gravity);
			CVars.CVars.SetCVar<bool>("particle.z_buffer", p.ZBuffer);
			CVars.CVars.Save(filename, sub_group);
		}

		public static void ParticleLoad(string filename, ParticleSet p) {
			List<string> sub_group = new List<string>();
			filename = "particles//" += filename + ".prt";
			sub_group.Add("particle");
			CVars.CVars.Load(filename, sub_group);
			p.Colors.Clear();
			p.Texture = CVars.CVars.GetCVar<string>("particle.texture");
			p.Acceleration = CVars.CVars.GetCVar<Vector3>("particle.acceleration");
			p.Blend = (PBlend) CVars.CVars.GetCVar<int>("particle.blend");
			p.EmitterLifespan = CVars.CVars.GetCVar<int>("particle.emitter_lifespan");
			p.EmitCount = CVars.CVars.GetCVar<int>("particle.emit_count");
			p.EmitInterval = CVars.CVars.GetCVar<int>("particle.emit_interval");
			p.FadeIn = CVars.CVars.GetCVar<int>("particle.fade_in");
			p.FadeOut = CVars.CVars.GetCVar<int>("particle.fade_out");
			p.Interpolate = CVars.CVars.GetCVar<bool>("particle.interpolate");
			p.Lifespan = CVars.CVars.GetCVar<int>("particle.lifespan");
			p.Origin = CVars.CVars.GetCVar<Vector3>("particle.origin");
			p.PanelType = (PanelType) CVars.CVars.GetCVar<int>("particle.panel_type");
			p.Rotation = CVars.CVars.GetCVar<Vector3>("particle.rotation");
			p.Size.pmin = CVars.CVars.GetCVar<Vector3>("particle.size.min");
			p.Size.pmax = CVars.CVars.GetCVar<Vector3>("particle.size.max");
			p.Speed.pmin = CVars.CVars.GetCVar<Vector3>("particle.speed.min");
			p.Speed.pmax = CVars.CVars.GetCVar<Vector3>("particle.speed.max");
			p.Spin = CVars.CVars.GetCVar<Vector3>("particle.spin");
			p.Volume.pmin = CVars.CVars.GetCVar<Vector3>("particle.volume.min");
			p.Volume.pmax = CVars.CVars.GetCVar<Vector3>("particle.volume.max");
			p.Wind = CVars.CVars.GetCVar<bool>("particle.wind");
			p.Gravity = CVars.CVars.GetCVar<bool>("particle.gravity");
			p.ZBuffer = CVars.CVars.GetCVar<bool>("particle.z_buffer");
		}

		public static bool ParticleCmd(List<string> args) {
			if (args.Count == 0) {
				Console.WriteLine(CVars.CVars.GetHelp("game"));
				return true;
			}
			CVars.CVars.SetCVar<string>("current_particle", args[0]);
			place_emitter();
			return true;
		}
	}
}
