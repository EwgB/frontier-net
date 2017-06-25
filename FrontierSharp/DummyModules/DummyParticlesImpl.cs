namespace FrontierSharp.DummyModules {
    using Interfaces.Particles;
    using Interfaces.Property;

    class DummyParticlesImpl : IParticles {
        private IParticlesProperties properties;
        public IProperties Properties { get { return this.properties; } }
        public IParticlesProperties ParticlesProperties { get { return this.properties; } }

        public void Init() {
            // Do nothing
        }
    }
}
