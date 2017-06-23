namespace FrontierSharp.Renderer {
    using Properties;

    class RendererProperties : Properties {
        private const string RENDER_WIREFRAME = "render_wireframe";

        public Property<bool> RenderWireframe {
            get { return base.Get<bool>(RENDER_WIREFRAME); }
            set { base.AddOrSet<bool>(value); }
        }

        public RendererProperties() {
            this.RenderWireframe = new Property<bool>(RENDER_WIREFRAME, false, "Overlay scene with wireframe.");

        }
    }
}
