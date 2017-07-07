namespace FrontierSharp.Common.Particles {
    using OpenTK;

    using Property;

    public interface IParticles : IModule, IHasProperties, IRenderable {
        IParticlesProperties ParticlesProperties { get; }

        void LoadParticles(string filename, ParticleSet particleSet);
        void SaveParticles(string filename, ParticleSet particleSet);
        uint AddParticles (ParticleSet particleSet, Vector3 position);
    }
}

/* From Particle.h

bool ParticleCmd (vector<string> *args);
void ParticleDestroy (UINT id);
void ParticleRetire (UINT id);
*/
