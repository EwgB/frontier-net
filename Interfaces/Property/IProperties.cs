namespace FrontierSharp.Interfaces.Property {
    public interface IProperties {
        void AddProperty(IProperty property);

        IProperty<T> GetProperty<T>(string name);
    }
}
