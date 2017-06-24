namespace FrontierSharp.Environment {
    using System;
    using Interfaces;
    using Interfaces.Property;

    public class EnvironmentImpl : IEnvironment {

        private EnvironmentProperties properties = new EnvironmentProperties();
        public IProperties Properties {
            get { return this.properties; }
        }

        public EnvironmentData GetCurrent() {
            throw new NotImplementedException();
        }

        public void Init() {
            throw new NotImplementedException();
        }
    }
}
