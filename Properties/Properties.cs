namespace FrontierSharp.Properties {
    using System;
    using System.Collections.Generic;

    using Common.Property;

    public class Properties :  IProperties {
        private readonly Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>();

        public void AddProperty(IProperty property) {
            try {
                properties.Add(property.Name, property);
            } catch (ArgumentException e) {
                throw new PropertyAddException(property, e);
            }
        }

        public IProperty<T> GetProperty<T>(string name) {
            if (properties.ContainsKey(name)) {
                if (properties[name] is IProperty<T> property) {
                    return property;
                } else {
                    throw new PropertyTypeException(properties[name].GetType(), typeof(T));
                }
            } else {
                throw new PropertyNotFoundException(name);
            }
        }
    }
}
