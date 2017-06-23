namespace FrontierSharp.Properties {
    using System;
    using System.Collections.Generic;

    public class Properties {
        private Dictionary<string, Property<object>> properties = new Dictionary<string, Property<object>>();

        protected void AddProperty<T>(Property<T> property) {
            try {
                this.properties.Add(property.Name, property as Property<object>);
            } catch (ArgumentException e) {
                throw new PropertyAddException<T>(property, e);
            }
        }

        protected Property<T> GetProperty<T>(string name) {
            return this.properties[name] as Property<T>;
        }
    }
}
