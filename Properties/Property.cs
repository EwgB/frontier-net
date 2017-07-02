namespace FrontierSharp.Properties {
    using Common.Property;

    public class Property<T> : IProperty<T> {
        public T Value { get; set; }
        public string Name { get; }
        public string Description { get; }

        public Property(string name, T initialValue) : this(name, initialValue, "") { }

        public Property(string name, T initialValue, string description) {
            Name = name;
            Description = description;
            Value = initialValue;
        }
    }
}
