namespace FrontierSharp.Avatar {
    using NLog;

    using Common.Avatar;

    using Properties;

    internal class AvatarProperties : Properties, IAvatarProperties {
        private const string SHOW_SKELETON = "show_skeleton";
        private const string INVERT_MOUSE = "invert_mouse";
        private const string MOUSE_SENSITIVITY = "mouse_sensitivity";
        private const string FLYING = "flying";
        private const string EXPAND_AVATAR = "expand_avatar";

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

        public bool Flying {
            get { return base.GetProperty<bool>(FLYING).Value; }
            set { base.GetProperty<bool>(FLYING).Value = value; }
        }

        public bool ExpandAvatar {
            get { return base.GetProperty<bool>(EXPAND_AVATAR).Value; }
            set { base.GetProperty<bool>(EXPAND_AVATAR).Value = value; }
        }

        public AvatarProperties() {
            try {
                base.AddProperty(new Property<bool>(SHOW_SKELETON, false, "Show the skeletons of avatars."));
                base.AddProperty(new Property<bool>(INVERT_MOUSE, false, "Reverse mouse y axis."));
                base.AddProperty(new Property<float>(MOUSE_SENSITIVITY, 1, "Mouse tracking."));
                base.AddProperty(new Property<bool>(FLYING, false, "Allows flight."));
                base.AddProperty(new Property<bool>(EXPAND_AVATAR, false, "Resize avatar proportions to be more cartoon-y."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
