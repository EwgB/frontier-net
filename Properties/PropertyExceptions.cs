namespace FrontierSharp.Properties {
    using System;

    using Common.Property;

    [Serializable]
    public class PropertyException : Exception {
        public PropertyException(string message)
            : base(message) { }

        public PropertyException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    [Serializable]
    internal class PropertyAddException : PropertyException {
        public PropertyAddException(IProperty property, Exception innerException)
            : base("Error adding property " + property.Name + ": " + innerException.Message, innerException) { }
    }

    [Serializable]
    internal class PropertyTypeException : PropertyException {
        public PropertyTypeException(Type found, Type expected)
            : base("Wrong property type. " + found.Name + " found, " + expected.Name + " expected.") { }
    }

    [Serializable]
    internal class PropertyNotFoundException : PropertyException {
        public PropertyNotFoundException(string propertyName)
            : base("Property " + propertyName + " not found.") { }
    }
}