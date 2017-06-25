namespace FrontierSharp.Interfaces {
    using OpenTK;

    using Property;

    public interface IAvatar : IHasProperties, IModule {
        Vector3 GetCameraPosition();
        Vector3 GetCameraAngle();
    }
}
