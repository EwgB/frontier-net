namespace FrontierSharp.Interfaces.Renderer {
    using Property;

    public interface IRenderer : IHasProperties, IModule {
        IRendererProperties RendererProperties { get; }

        void Render();
        void ToggleShowMap();
    }
}
