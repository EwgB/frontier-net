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
            get { return GetProperty<bool>(SHOW_SKELETON).Value; }
            set { GetProperty<bool>(SHOW_SKELETON).Value = value; }
        }

        public bool InvertMouse {
            get { return GetProperty<bool>(INVERT_MOUSE).Value; }
            set { GetProperty<bool>(INVERT_MOUSE).Value = value; }
        }

        public float MouseSensitivity {
            get { return GetProperty<float>(MOUSE_SENSITIVITY).Value; }
            set { GetProperty<float>(MOUSE_SENSITIVITY).Value = value; }
        }

        public bool Flying {
            get { return GetProperty<bool>(FLYING).Value; }
            set { GetProperty<bool>(FLYING).Value = value; }
        }

        public bool ExpandAvatar {
            get { return GetProperty<bool>(EXPAND_AVATAR).Value; }
            set { GetProperty<bool>(EXPAND_AVATAR).Value = value; }
        }

        public AvatarProperties() {
            try {
                AddProperty(new Property<bool>(SHOW_SKELETON, false, "Show the skeletons of avatars."));
                AddProperty(new Property<bool>(INVERT_MOUSE, false, "Reverse mouse y axis."));
                AddProperty(new Property<float>(MOUSE_SENSITIVITY, 1, "Mouse tracking."));
                AddProperty(new Property<bool>(FLYING, false, "Allows flight."));
                AddProperty(new Property<bool>(EXPAND_AVATAR, false, "Resize avatar proportions to be more cartoon-y."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
