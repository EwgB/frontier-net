namespace FrontierSharp.Common.Renderer {
    using Property;

    public interface IRenderer : IHasProperties, IModule, IRenderable {
        IRendererProperties RendererProperties { get; }

        void ToggleShowMap();
        void RenderLoadingScreen(float progress);
    }
}
