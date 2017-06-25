namespace FrontierSharp.Particles {
    using Interfaces;
    using Interfaces.Property;

    ///<summary>Manages the list of active particle emitters.</summary>
    public class ParticlesImpl : IParticles {

        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        //private static readonly List<CEmitter> EmitterList = new List<CEmitter>();

        public void Init() {
            //  CVarUtils::CreateCVar<string> ("particle.texture", "");
            //  CVarUtils::CreateCVar ("particle.acceleration", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.blend", 0);
            //  CVarUtils::CreateCVar ("particle.emitter_lifespan", 0);
            //  CVarUtils::CreateCVar ("particle.emit_count", 0);
            //  CVarUtils::CreateCVar ("particle.emit_interval", 0);
            //  CVarUtils::CreateCVar ("particle.fade_in", 0);
            //  CVarUtils::CreateCVar ("particle.fade_out", 0);
            //  CVarUtils::CreateCVar ("particle.interpolate", 0);
            //  CVarUtils::CreateCVar ("particle.lifespan", 0);
            //  CVarUtils::CreateCVar ("particle.origin", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.panel_type", 0);
            //  CVarUtils::CreateCVar ("particle.rotation", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.size.min", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.size.max", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.speed.min", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.speed.max", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.spin", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.volume.min", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar ("particle.volume.max", glVector (0.0f, 0.0f, 0.0f));
            //  CVarUtils::CreateCVar<bool> ("particle.wind", false);
            //  CVarUtils::CreateCVar<bool> ("particle.gravity", false);
            //  CVarUtils::CreateCVar<bool> ("particle.z_buffer", false);

            //  CVarUtils::CreateCVar<string> ("current_particle", "");
        }

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

        //UINT ParticleAdd (ParticleSet* p_in, GLvector position)
        //{

        //  CEmitter*   e;
        //  CEmitter    new_emitter;
        //  ParticleSet p;

        //  p = *p_in;
        //  p.volume.pmin += position;
        //  p.volume.pmax += position;
        //  EmitterList.resize (EmitterList.size () + 1);
        //  e = &EmitterList[EmitterList.size () - 1];
        //  e->Set (&p);
        //  return e->Id ();

        //}

        //void ParticleDestroy (UINT id)
        //{

        //  unsigned    i;

        //  for (i = 0; i < EmitterList.size (); i++) {
        //    if (EmitterList[i].Id () == id) {
        //      ConsoleLog ("ParticleDestroy: Removing effect #%d.", id);
        //      EmitterList.erase (EmitterList.begin () + i);
        //      return;
        //    }
        //  }
        //  ConsoleLog ("ParticleDestroy: Effect #%d not found.", id);

        //}


        //void ParticleRetire (UINT id)
        //{

        //  unsigned    i;

        //  for (i = 0; i < EmitterList.size (); i++) {
        //    if (EmitterList[i].Id () == id) {
        //      ConsoleLog ("ParticleRetire: Disabling effect #%d.", id);
        //      EmitterList[i].Retire ();
        //      return;
        //    }
        //  }
        //  ConsoleLog ("ParticleRetire: Effect #%d not found.", id);

        //}


        //void ParticleUpdate ()
        //{

        //  unsigned    i;

        //  for (i = 0; i < EmitterList.size (); i++) 
        //    EmitterList[i].Update (SdlElapsedSeconds ());
        //  for (i = 0; i < EmitterList.size (); i++) {
        //    if (EmitterList[i].Dead ()) {
        //      EmitterList.erase (EmitterList.begin () + i);
        //      break;
        //    }
        //  }
        //  if (InputKeyPressed (SDLK_f)) 
        //    place_emitter ();

        //}

        //void ParticleRender ()
        //{

        //  unsigned    i;

        //  glEnable (GL_BLEND);
        //  glEnable (GL_TEXTURE_2D);
        //  glDisable (GL_LIGHTING);
        //  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        //  for (i = 0; i < EmitterList.size (); i++) 
        //    EmitterList[i].Render ();

        //}

        //void ParticleSave (char* filename_in, ParticleSet* p)
        //{

        //  string            filename;
        //  vector<string>    sub_group;

        //  CVarUtils::SetCVar<string> ("particle.texture", p->texture);
        //  CVarUtils::SetCVar ("particle.acceleration", p->acceleration);
        //  CVarUtils::SetCVar ("particle.blend", p->blend);
        //  CVarUtils::SetCVar ("particle.emitter_lifespan", p->emitter_lifespan);
        //  CVarUtils::SetCVar ("particle.emit_count", p->emit_count);
        //  CVarUtils::SetCVar ("particle.emit_interval", p->emit_interval);
        //  CVarUtils::SetCVar ("particle.fade_in", p->fade_in);
        //  CVarUtils::SetCVar ("particle.fade_out", p->fade_out);
        //  CVarUtils::SetCVar ("particle.interpolate", p->interpolate);
        //  CVarUtils::SetCVar ("particle.lifespan", p->lifespan);
        //  CVarUtils::SetCVar ("particle.origin", p->origin);
        //  CVarUtils::SetCVar ("particle.panel_type", p->panel_type);
        //  CVarUtils::SetCVar ("particle.rotation", p->rotation);
        //  CVarUtils::SetCVar ("particle.size.min", p->size.pmin);
        //  CVarUtils::SetCVar ("particle.size.max", p->size.pmax);
        //  CVarUtils::SetCVar ("particle.speed.min", p->speed.pmin);
        //  CVarUtils::SetCVar ("particle.speed.max", p->speed.pmax);
        //  CVarUtils::SetCVar ("particle.spin", p->spin);
        //  CVarUtils::SetCVar ("particle.volume.min", p->volume.pmin);
        //  CVarUtils::SetCVar ("particle.volume.max", p->volume.pmax);
        //  CVarUtils::SetCVar<bool> ("particle.wind", p->wind);
        //  CVarUtils::SetCVar<bool> ("particle.gravity", p->gravity);
        //  CVarUtils::SetCVar<bool> ("particle.z_buffer", p->z_buffer);
        //  filename = "particles//";
        //  filename += filename_in;
        //  filename += ".prt";
        //  sub_group.push_back ("particle");
        //  CVarUtils::Save (filename, sub_group);

        //}

        //void ParticleLoad (const char* filename_in, ParticleSet* p)
        //{

        //  string            filename;
        //  vector<string>    sub_group;

        //  filename = "particles//";
        //  filename += filename_in;
        //  filename += ".prt";
        //  sub_group.push_back ("particle");
        //  CVarUtils::Load (filename, sub_group);
        //  p->colors.clear ();
        //  p->texture = CVarUtils::GetCVar<string> ("particle.texture");
        //  p->acceleration = CVarUtils::GetCVar<GLvector> ("particle.acceleration");
        //  p->blend = (PBlend)CVarUtils::GetCVar<int> ("particle.blend");
        //  p->emitter_lifespan = CVarUtils::GetCVar<int> ("particle.emitter_lifespan");
        //  p->emit_count = CVarUtils::GetCVar<int> ("particle.emit_count");
        //  p->emit_interval = CVarUtils::GetCVar<int> ("particle.emit_interval");
        //  p->fade_in = CVarUtils::GetCVar<int> ("particle.fade_in");
        //  p->fade_out = CVarUtils::GetCVar<int> ("particle.fade_out");
        //  p->interpolate = CVarUtils::GetCVar<bool> ("particle.interpolate");
        //  p->lifespan = CVarUtils::GetCVar<int> ("particle.lifespan");
        //  p->origin = CVarUtils::GetCVar<GLvector> ("particle.origin");
        //  p->panel_type = (PType)CVarUtils::GetCVar<int> ("particle.panel_type");
        //  p->rotation = CVarUtils::GetCVar<GLvector> ("particle.rotation");
        //  p->size.pmin = CVarUtils::GetCVar<GLvector> ("particle.size.min");
        //  p->size.pmax = CVarUtils::GetCVar<GLvector> ("particle.size.max");
        //  p->speed.pmin = CVarUtils::GetCVar<GLvector> ("particle.speed.min");
        //  p->speed.pmax = CVarUtils::GetCVar<GLvector> ("particle.speed.max");
        //  p->spin= CVarUtils::GetCVar<GLvector> ("particle.spin");
        //  p->volume.pmin = CVarUtils::GetCVar<GLvector> ("particle.volume.min");
        //  p->volume.pmax = CVarUtils::GetCVar<GLvector> ("particle.volume.max");
        //  p->wind = CVarUtils::GetCVar<bool> ("particle.wind");
        //  p->gravity = CVarUtils::GetCVar<bool> ("particle.gravity");
        //  p->z_buffer = CVarUtils::GetCVar<bool> ("particle.z_buffer");

        //}


        //bool ParticleCmd (vector<string> *args)
        //{

        //  if (args->empty ()) {
        //    ConsoleLog (CVarUtils::GetHelp ("game").data ());
        //    return true;
        //  }
        //  CVarUtils::SetCVar<string> ("current_particle", args->data ()[0].c_str ());
        //  place_emitter ();
        //  return true;

        //}

    }
}
