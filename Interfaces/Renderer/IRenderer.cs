namespace FrontierSharp.Interfaces.Renderer {
    using Property;

    public interface IRenderer : IHasProperties {
        IRendererProperties RendererProperties { get; }

        void Init();
        void Render();
    }
}
