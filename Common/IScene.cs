namespace FrontierSharp.Common {
    using Property;

    public interface IScene : IHasProperties, IModule, IRenderable {
        /// <summary>How far it is from the center of the terrain grid to the outer edge</summary>
        float VisibleRange { get; }

        void RenderDebug();
    }
}
