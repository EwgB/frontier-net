namespace FrontierSharp.Properties {
    public class Property<T> {
        T Value { get; set; }
        string Name { get; }
        string Description { get; }

        public Property(string name, T initialValue, string description) {
            Name = name;
            Description = description;
            Value = initialValue;
        }
    }
}
