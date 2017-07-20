namespace FrontierSharp.Common.Renderer {
    using Property;

    /// <summary>Kicks off most of the rendering jobs and handles the GL setup.</summary>
    public interface IRenderer : IHasProperties, IModule, IRenderable {
        IRendererProperties RendererProperties { get; }

        void ToggleShowMap();
        void RequestLoadingScreen(float progress);
    }
}
