namespace FrontierSharp.Interfaces.Property {
    public interface IProperties {
        void AddProperty<T>(IProperty<T> property);

        IProperty<T> GetProperty<T>(string name);
    }
}
