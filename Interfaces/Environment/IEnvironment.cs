namespace FrontierSharp.Interfaces.Environment {
    using Property;

    public interface IEnvironment : IHasProperties, IModule {
        EnvironmentData GetCurrent();
    }
}
