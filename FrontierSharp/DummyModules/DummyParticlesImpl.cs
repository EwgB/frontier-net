namespace FrontierSharp.DummyModules {
    using Interfaces;
    using Properties;

    class DummyParticlesImpl : IParticles {
        private Properties properties;
        public Properties Properties {
            get { return this.properties; }
        }
    }
}
