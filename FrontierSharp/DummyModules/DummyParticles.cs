namespace FrontierSharp.DummyModules {
    using System;
    using Common.Particles;
    using Common.Property;
    using OpenTK;

    class DummyParticles : IParticles {
        private IParticlesProperties properties;
        public IProperties Properties { get { return this.properties; } }
        public IParticlesProperties ParticlesProperties { get { return this.properties; } }

        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public void Render() {
            // Do nothing
        }

        public uint AddParticles(ParticleSet particleSet, Vector3 position) {
            return 0;
        }

        public void LoadParticles(string filename, ParticleSet particleSet) {
            // Do nothing
        }

        public void SaveParticles(string filename, ParticleSet particleSet) {
            // Do nothing
        }
    }
}
