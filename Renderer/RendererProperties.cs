namespace FrontierSharp.Renderer {
    using NLog;

    using Common.Renderer;
    using Properties;

    internal class RendererProperties : Properties, IRendererProperties {
        private const string RENDER_WIREFRAME = "render_wireframe";
        private const string RENDER_SHADERS = "render_shaders";
        private const string SHOW_PAGES = "show_pages";

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool RenderWireframe {
            get { return base.GetProperty<bool>(RENDER_WIREFRAME).Value; }
            set { base.GetProperty<bool>(RENDER_WIREFRAME).Value = value; }
        }

        public bool RenderShaders {
            get { return base.GetProperty<bool>(RENDER_SHADERS).Value; }
            set { base.GetProperty<bool>(RENDER_SHADERS).Value = value; }
        }

        public bool ShowPages {
            get { return base.GetProperty<bool>(SHOW_PAGES).Value; }
            set { base.GetProperty<bool>(SHOW_PAGES).Value = value; }
        }

        public RendererProperties() {
            try {
                base.AddProperty(new Property<bool>(RENDER_WIREFRAME, false, "Overlay scene with wireframe."));
                base.AddProperty(new Property<bool>(RENDER_SHADERS, true, "Enable vertex, fragment shaders."));
                base.AddProperty(new Property<bool>(SHOW_PAGES, false, "Show bounding boxes for paged data."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
