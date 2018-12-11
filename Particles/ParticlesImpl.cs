namespace FrontierSharp.Particles {
    using System.Collections.Generic;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common.Particles;
    using Common.Property;

    ///<summary>Manages the list of active particle emitters.</summary>
    internal class ParticlesImpl : IParticles {

        public IProperties Properties => this.ParticlesProperties;
        public IParticlesProperties ParticlesProperties { get; } = new ParticlesProperties();

        private readonly List<IEmitter> emitterList = new List<IEmitter>();

        public void Init() { }

        public void Update() {
            // TODO

            //  for (var i = 0; i < EmitterList.size (); i++) 
            //    EmitterList[i].Update (SdlElapsedSeconds ());
            //  for (i = 0; i < EmitterList.size (); i++) {
            //    if (EmitterList[i].Dead ()) {
            //      EmitterList.erase (EmitterList.begin () + i);
            //      break;
            //    }
            //  }
            //  if (InputKeyPressed (SDLK_f)) 
            //    place_emitter ();

        }

        public void Render() {
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            foreach (var emitter in this.emitterList) {
                emitter.Render();
            }
        }

        public int AddParticles(ParticleSet particleSet, Vector3 position) {
            return 0;
            // TODO
            //CEmitter* e;
            //CEmitter new_emitter;
            //ParticleSet p;

            //p = *p_in;
            //p.volume.pmin += position;
            //p.volume.pmax += position;
            //EmitterList.resize(EmitterList.size() + 1);
            //e = &EmitterList[EmitterList.size() - 1];
            //e.Set(&p);
            //return e.Id();
        }

        public ParticleSet LoadParticles(string filename) {
            return new ParticleSet();
            // TODO
            //  string            filename;
            //  vector<string>    sub_group;

            //  filename = "particles//";
            //  filename += filename_in;
            //  filename += ".prt";
            //  sub_group.push_back ("particle");
            //  CVarUtils::Load (filename, sub_group);
            //  p.colors.clear ();
            //  p.texture = CVarUtils::GetCVar<string> ("particle.texture");
            //  p.acceleration = CVarUtils::GetCVar<GLvector> ("particle.acceleration");
            //  p.blend = (PBlend)CVarUtils::GetCVar<int> ("particle.blend");
            //  p.emitter_lifespan = CVarUtils::GetCVar<int> ("particle.emitter_lifespan");
            //  p.emit_count = CVarUtils::GetCVar<int> ("particle.emit_count");
            //  p.emit_interval = CVarUtils::GetCVar<int> ("particle.emit_interval");
            //  p.fade_in = CVarUtils::GetCVar<int> ("particle.fade_in");
            //  p.fade_out = CVarUtils::GetCVar<int> ("particle.fade_out");
            //  p.interpolate = CVarUtils::GetCVar<bool> ("particle.interpolate");
            //  p.lifespan = CVarUtils::GetCVar<int> ("particle.lifespan");
            //  p.origin = CVarUtils::GetCVar<GLvector> ("particle.origin");
            //  p.panel_type = (PType)CVarUtils::GetCVar<int> ("particle.panel_type");
            //  p.rotation = CVarUtils::GetCVar<GLvector> ("particle.rotation");
            //  p.size.pmin = CVarUtils::GetCVar<GLvector> ("particle.size.min");
            //  p.size.pmax = CVarUtils::GetCVar<GLvector> ("particle.size.max");
            //  p.speed.pmin = CVarUtils::GetCVar<GLvector> ("particle.speed.min");
            //  p.speed.pmax = CVarUtils::GetCVar<GLvector> ("particle.speed.max");
            //  p.spin= CVarUtils::GetCVar<GLvector> ("particle.spin");
            //  p.volume.pmin = CVarUtils::GetCVar<GLvector> ("particle.volume.min");
            //  p.volume.pmax = CVarUtils::GetCVar<GLvector> ("particle.volume.max");
            //  p.wind = CVarUtils::GetCVar<bool> ("particle.wind");
            //  p.gravity = CVarUtils::GetCVar<bool> ("particle.gravity");
            //  p.z_buffer = CVarUtils::GetCVar<bool> ("particle.z_buffer");
        }

        public void SaveParticles(string filename, ParticleSet particleSet) {
            // TODO
            //  string            filename;
            //  vector<string>    sub_group;

            //  CVarUtils::SetCVar<string> ("particle.texture", p.texture);
            //  CVarUtils::SetCVar ("particle.acceleration", p.acceleration);
            //  CVarUtils::SetCVar ("particle.blend", p.blend);
            //  CVarUtils::SetCVar ("particle.emitter_lifespan", p.emitter_lifespan);
            //  CVarUtils::SetCVar ("particle.emit_count", p.emit_count);
            //  CVarUtils::SetCVar ("particle.emit_interval", p.emit_interval);
            //  CVarUtils::SetCVar ("particle.fade_in", p.fade_in);
            //  CVarUtils::SetCVar ("particle.fade_out", p.fade_out);
            //  CVarUtils::SetCVar ("particle.interpolate", p.interpolate);
            //  CVarUtils::SetCVar ("particle.lifespan", p.lifespan);
            //  CVarUtils::SetCVar ("particle.origin", p.origin);
            //  CVarUtils::SetCVar ("particle.panel_type", p.panel_type);
            //  CVarUtils::SetCVar ("particle.rotation", p.rotation);
            //  CVarUtils::SetCVar ("particle.size.min", p.size.pmin);
            //  CVarUtils::SetCVar ("particle.size.max", p.size.pmax);
            //  CVarUtils::SetCVar ("particle.speed.min", p.speed.pmin);
            //  CVarUtils::SetCVar ("particle.speed.max", p.speed.pmax);
            //  CVarUtils::SetCVar ("particle.spin", p.spin);
            //  CVarUtils::SetCVar ("particle.volume.min", p.volume.pmin);
            //  CVarUtils::SetCVar ("particle.volume.max", p.volume.pmax);
            //  CVarUtils::SetCVar<bool> ("particle.wind", p.wind);
            //  CVarUtils::SetCVar<bool> ("particle.gravity", p.gravity);
            //  CVarUtils::SetCVar<bool> ("particle.z_buffer", p.z_buffer);
            //  filename = "particles//";
            //  filename += filename_in;
            //  filename += ".prt";
            //  sub_group.push_back ("particle");
            //  CVarUtils::Save (filename, sub_group);
        }
    }
}

// From Particle.cpp

        ////For debugging.  Place an emitter at our feet
        //void place_emitter ()
        //{

        //  ParticleSet   p;
        //  GLvector      v;
        //  string        name;

        //  name = CVarUtils::GetCVar<string> ("current_particle");
        //  v = AvatarPosition ();
        //  ParticleLoad (name.c_str (), &p);
        //  ParticleSave ("test", &p);
        //  ParticleAdd (&p, v);

        //}

        //void ParticleDestroy (int id)
        //{

        //  int    i;

        //  for (i = 0; i < EmitterList.size (); i++) {
        //    if (EmitterList[i].Id () == id) {
        //      ConsoleLog ("ParticleDestroy: Removing effect #%d.", id);
        //      EmitterList.erase (EmitterList.begin () + i);
        //      return;
        //    }
        //  }
        //  ConsoleLog ("ParticleDestroy: Effect #%d not found.", id);

        //}


        //void ParticleRetire (int id)
        //{

        //  int    i;

        //  for (i = 0; i < EmitterList.size (); i++) {
        //    if (EmitterList[i].Id () == id) {
        //      ConsoleLog ("ParticleRetire: Disabling effect #%d.", id);
        //      EmitterList[i].Retire ();
        //      return;
        //    }
        //  }
        //  ConsoleLog ("ParticleRetire: Effect #%d not found.", id);

        //}

        //bool ParticleCmd (vector<string> *args)
        //{

        //  if (args.empty ()) {
        //    ConsoleLog (CVarUtils::GetHelp ("game").data ());
        //    return true;
        //  }
        //  CVarUtils::SetCVar<string> ("current_particle", args.data ()[0].c_str ());
        //  place_emitter ();
        //  return true;

        //}
