namespace FrontierSharp.Scene {
    using NLog;

    using Common.Scene;

    using Properties;

    class SceneProperties : Properties, ISceneProperties {
        private const string RENDER_TEXTURED = "render_textured";

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool RenderTextured{
            get { return GetProperty<bool>(RENDER_TEXTURED).Value; }
            set { GetProperty<bool>(RENDER_TEXTURED).Value = value; }
        }

        public SceneProperties() {
            try {
                AddProperty(new Property<bool>(RENDER_TEXTURED, true, "Render the scene with textures."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
