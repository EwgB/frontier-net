namespace FrontierSharp.Interfaces {
    using OpenTK;

    using Property;
    using Region;

    public interface IAvatar : IHasProperties, IModule {
        Vector3 GetCameraPosition();
        Vector3 GetCameraAngle();
        IRegion Region { get; }
    }
}
