namespace FrontierSharp.Particles {
    using OpenTK;

    using Common.Particles;
    using Common.Property;

    class DummyParticles : IParticles {
        public IParticlesProperties ParticlesProperties { get; }
        public IProperties Properties => this.ParticlesProperties;

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }

        public uint AddParticles(ParticleSet particleSet, Vector3 position) => 0;

        public ParticleSet LoadParticles(string filename) => new ParticleSet();

        public void SaveParticles(string filename, ParticleSet particleSet) { /* Do nothing */ }
    }
}
