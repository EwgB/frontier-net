namespace FrontierSharp.DummyModules {
    using System;
    using Common.Particles;
    using Common.Property;

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
    }
}
