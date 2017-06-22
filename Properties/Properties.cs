namespace FrontierSharp.Properties {
    using System.Collections.Generic;

    public class Properties {
        private Dictionary<string, object> properties = new Dictionary<string, object>();
        protected object this[string name] {
            get { return properties[name]; }
            set { properties[name] = value; }
        }
    }
}
