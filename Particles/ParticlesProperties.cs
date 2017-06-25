namespace FrontierSharp.Particles {
    using NLog;
    using OpenTK;

    using Interfaces.Particles;

    using Properties;

    internal class ParticlesProperties : Properties, IParticlesProperties {
        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string TEXTURE = "texture";
        private const string ACCELERARTION = "acceleration";
        private const string BLEND = "blend";
        private const string EMITTER_LIFESPAN = "emitter_lifespan";
        private const string EMIT_COUNT = "emit_count";
        private const string EMIT_INTERVAL = "emit_interval";
        private const string FADE_IN = "fade_in";
        private const string FADE_OUT = "fade_out";
        private const string INTERPOLATE = "interpolate";
        private const string PARTICLE_LIFESPAN = "particle_lifespan";
        private const string ORIGIN = "origin";
        private const string PANEL_TYPE = "panel_type";
        private const string ROTATION = "rotation";
        private const string SIZE_MIN = "size_min";
        private const string SIZE_MAX = "size_max";
        private const string SPEED_MIN = "speed_min";
        private const string SPEED_MAX = "speed_max";
        private const string SPIN = "spin";
        private const string VOLUME_MIN = "volume_min";
        private const string VOLUME_MAX = "volume_max";
        private const string WIND = "wind";
        private const string GRAVITY = "gravity";
        private const string Z_BUFFER = "z_buffer";
        private const string CURRENT_PARTICLE = "current_particle";

        ParticlesProperties() {
            try {
                base.AddProperty(new Property<string>(TEXTURE, ""));
                base.AddProperty(new Property<Vector3>(ACCELERARTION, Vector3.Zero));
                base.AddProperty(new Property<int>(BLEND, 0));
                base.AddProperty(new Property<int>(EMITTER_LIFESPAN, 0));
                base.AddProperty(new Property<int>(EMIT_COUNT, 0));
                base.AddProperty(new Property<int>(EMIT_INTERVAL, 0));
                base.AddProperty(new Property<int>(FADE_IN, 0));
                base.AddProperty(new Property<int>(FADE_OUT, 0));
                base.AddProperty(new Property<int>(INTERPOLATE, 0));
                base.AddProperty(new Property<int>(PARTICLE_LIFESPAN, 0));
                base.AddProperty(new Property<Vector3>(ORIGIN, Vector3.Zero));
                base.AddProperty(new Property<int>(PANEL_TYPE, 0));
                base.AddProperty(new Property<Vector3>(ROTATION, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(SIZE_MIN, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(SIZE_MAX, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(SPEED_MIN, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(SPEED_MAX, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(SPIN, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(VOLUME_MIN, Vector3.Zero));
                base.AddProperty(new Property<Vector3>(VOLUME_MAX, Vector3.Zero));
                base.AddProperty(new Property<bool>(WIND, false));
                base.AddProperty(new Property<bool>(GRAVITY, false));
                base.AddProperty(new Property<bool>(Z_BUFFER, false));
                base.AddProperty(new Property<string>(CURRENT_PARTICLE, ""));
            } catch (PropertyException e) {
                Log.Error(e.Message);
            }
        }
    }
}
