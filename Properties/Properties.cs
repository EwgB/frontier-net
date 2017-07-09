namespace FrontierSharp.Properties {
    using System;
    using System.Collections.Generic;

    using Common.Property;

    public class Properties :  IProperties {
        private readonly Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>();

        public void AddProperty(IProperty property) {
            try {
                this.properties.Add(property.Name, property);
            } catch (ArgumentException e) {
                throw new PropertyAddException(property, e);
            }
        }

        public IProperty<T> GetProperty<T>(string name) {
            if (this.properties.ContainsKey(name)) {
                var property = this.properties[name] as IProperty<T>;
                if (null != property) {
                    return property;
                } else {
                    throw new PropertyTypeException(this.properties[name].GetType(), typeof(T));
                }
            } else {
                throw new PropertyNotFoundException(name);
            }
        }
    }
}
