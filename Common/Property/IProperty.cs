namespace FrontierSharp.Common.Property {
    public interface IProperty {
        string Description { get; }
        string Name { get; }
    }

    public interface IProperty<T> : IProperty {
        T Value { get; set; }
    }
}
