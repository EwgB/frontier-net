namespace FrontierSharp.Properties {
    using System.Collections.Generic;

    public class Properties {
        private Dictionary<string, Property<object>> properties = new Dictionary<string, Property<object>>();

        protected void AddOrSet<T>(Property<T> property) {
            this.properties[property.Name] = property as Property<object>;
        }

        protected Property<T> Get<T>(string name) {
            return this.properties[name] as Property<T>;
        }
    }
}
