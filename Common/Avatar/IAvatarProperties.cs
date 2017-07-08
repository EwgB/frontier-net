namespace FrontierSharp.Common.Avatar {
    using Property;

    public interface IAvatarProperties : IProperties {
        bool ShowSkeleton { get; set; }
        bool InvertMouse { get; set; }
        float MouseSensitivity { get; set; }
        bool Flying { get; set; }
        bool ExpandAvatar { get; set; }
    }
}
