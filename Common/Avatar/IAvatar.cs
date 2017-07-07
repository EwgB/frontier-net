namespace FrontierSharp.Common.Avatar {
    using OpenTK;

    using Animation;
    using Property;
    using Region;

    /// <summary>Handles movement and player input.</summary>
    public interface IAvatar : IModule, IHasProperties, IRenderable {
        IAvatarProperties AvatarProperties { get; }

        IRegion Region { get; }
        Vector3 Position { get; set; }
        AnimTypes AnimationType { get; }
        Vector3 CameraAngle { get; }
        Vector3 CameraPosition { get; }

        void Look(int x, int y);
    }
}
