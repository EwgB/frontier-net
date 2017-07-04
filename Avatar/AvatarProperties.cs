namespace FrontierSharp.Avatar {
    using NLog;

    using Common.Avatar;

    using Properties;

    internal class AvatarProperties : Properties, IAvatarProperties {
        private const string SHOW_SKELETON = "show_skeleton";

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public bool ShowSkeleton {
            get { return base.GetProperty<bool>(SHOW_SKELETON).Value; }
            set { base.GetProperty<bool>(SHOW_SKELETON).Value = value; }
        }

        public AvatarProperties() {
            try {
                base.AddProperty(new Property<bool>(SHOW_SKELETON, false, "Show the skeletons of avatars."));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
