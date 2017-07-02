namespace FrontierSharp.Common.Renderer {
    using Property;

    public interface IRendererProperties : IProperties {
        bool RenderWireframe { get; set; }

        bool RenderShaders { get; set; }

        bool ShowPages { get; set; }
    }
}
