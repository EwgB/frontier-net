namespace FrontierSharp.Avatar {
    using NLog;

    using Common.Avatar;

    using Properties;

    internal class AvatarProperties : Properties, IAvatarProperties {
        private const string SHOW_SKELETON = "show_skeleton";
        private const string INVERT_MOUSE = "invert_mouse";
        private const string MOUSE_SENSITIVITY = "mouse_sensitivity";

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool ShowSkeleton {
            get { return base.GetProperty<bool>(SHOW_SKELETON).Value; }
            set { base.GetProperty<bool>(SHOW_SKELETON).Value = value; }
        }

        public bool InvertMouse {
            get { return base.GetProperty<bool>(INVERT_MOUSE).Value; }
            set { base.GetProperty<bool>(INVERT_MOUSE).Value = value; }
        }

        public float MouseSensitivity {
            get { return base.GetProperty<float>(MOUSE_SENSITIVITY).Value; }
            set { base.GetProperty<float>(MOUSE_SENSITIVITY).Value = value; }
        }

        public AvatarProperties() {
            try {
                base.AddProperty(new Property<bool>(SHOW_SKELETON, false, "Show the skeletons of avatars."));
                base.AddProperty(new Property<bool>(INVERT_MOUSE, false, "Reverse mouse y axis."));
                base.AddProperty(new Property<float>(MOUSE_SENSITIVITY, 1, "Mouse tracking."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
