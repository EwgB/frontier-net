namespace FrontierSharp.Common.Particles {
    using System.Collections.Generic;

    using OpenTK;

    using Util;

    public enum ParticleBlend { Alpha, Add }

    public enum ParticleType { Facer, PanelX, PanelY, PanelZ }

    public struct ParticleSet {
        public string texture;
        public BoundingBox volume;
        public BoundingBox speed;
        public Vector3 acceleration;
        public BoundingBox size;
        public Vector3 origin;
        public Vector3 rotation;
        public Vector3 spin;
        public ParticleBlend blend;
        public ParticleType panelType;
        public uint fadeIn;
        public uint fadeOut;
        public uint lifespan;
        public uint emitInterval;
        public uint emitCount;
        public uint emitterLifespan;
        public bool gravity;
        public bool wind;
        public bool interpolate;
        public bool zBuffer;
        public List<Color3> colors;
    }
}
