namespace FrontierSharp.Properties {
    public class Property<T> {
        public T Value { get; set; }
        public string Name { get; }
        public string Description { get; }

        public Property(string name, T initialValue, string description) {
            Name = name;
            Description = description;
            Value = initialValue;
        }
    }
}
