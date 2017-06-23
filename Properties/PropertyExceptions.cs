using System;

namespace FrontierSharp.Properties {
    [Serializable]
    public class PropertyException : Exception {
        public PropertyException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    [Serializable]
    internal class PropertyAddException<T> : PropertyException {
        public PropertyAddException(Property<T> property, Exception innerException)
            : base("Error adding property " + property.Name + ": " + innerException.Message, innerException) { }
    }
}