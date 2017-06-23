namespace FrontierSharp.Interfaces {
    using OpenTK;

    public interface IAvatar : IHasProperties {
        Vector3 GetCameraPosition();
        Vector3 GetCameraAngle();
    }
}
