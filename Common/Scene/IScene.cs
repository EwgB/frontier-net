namespace FrontierSharp.Common.Scene {
    using Property;

    /// <summary>
    /// Manages all the various objects that need to be created, rendered,
    /// and deleted at various times.If it gets drawn, and if there's more than
    /// one of it, then it should go here.
    /// </summary>
    public interface IScene : IHasProperties, IModule, IRenderable {
        ISceneProperties SceneProperties { get; }

        /// <summary>How far it is from the center of the terrain grid to the outer edge</summary>
        float VisibleRange { get; }

        void RenderDebug();
    }
}
