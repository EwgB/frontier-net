namespace FrontierSharp.Environment {
    using System;
    using Interfaces;
    using Properties;

    public class EnvironmentImpl : IEnvironment {

        private EnvironmentProperties properties = new EnvironmentProperties();
        public Properties Properties {
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
