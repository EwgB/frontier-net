namespace FrontierSharp.DummyModules {
    using Interfaces;
    using Interfaces.Property;

    class DummyParticlesImpl : IParticles {
        private IProperties properties;
        public IProperties Properties { get { return this.properties; } }
    }
}
