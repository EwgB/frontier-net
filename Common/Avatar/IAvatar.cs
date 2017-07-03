namespace FrontierSharp.Common.Avatar {
    using OpenTK;

    using Region;

    /// <summary>Handles movement and player input.</summary>
    public interface IAvatar : IModule, IRenderable {
        IRegion Region { get; }
        Vector3 Position { get; set; }
        AnimType AnimationType { get; }
        Vector3 CameraAngle { get; }
        Vector3 CameraPosition { get; }
    }
}

/* From Avatar.h

AnimType  AvatarAnim ();
void      AvatarLook (int x, int y);

*/
