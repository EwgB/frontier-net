namespace FrontierSharp.DummyModules {
    using Interfaces.Particles;
    using Interfaces.Property;

    class DummyParticlesImpl : IParticles {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }

        public void Init() {
            // Do nothing
        }
    }
}
