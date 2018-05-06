namespace FrontierSharp.Common.Particles {
    using OpenTK;

    using Property;

    /// <summary>Manages the list of active particle emitters.</summary>
    public interface IParticles : IModule, IHasProperties, IRenderable {
        IParticlesProperties ParticlesProperties { get; }

        ParticleSet LoadParticles(string filename);
        void SaveParticles(string filename, ParticleSet particleSet);
        int AddParticles (ParticleSet particleSet, Vector3 position);
    }
}

/* From Particle.h

bool ParticleCmd (vector<string> *args);
void ParticleDestroy (int id);
void ParticleRetire (int id);
*/
