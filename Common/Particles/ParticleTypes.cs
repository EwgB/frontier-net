namespace FrontierSharp.Common.Particles {
    using System.Collections.Generic;

    using OpenTK;

    using Util;

    public enum ParticleBlend { Alpha, Add }

    public enum ParticleType { Facer, PanelX, PanelY, PanelZ }

    public struct ParticleSet {
        public string Texture;
        public BoundingBox Volume;
        public BoundingBox Speed;
        public Vector3 Acceleration;
        public BoundingBox Size;
        public Vector3 Origin;
        public Vector3 Rotation;
        public Vector3 Spin;
        public ParticleBlend Blend;
        public ParticleType PanelType;
        public uint FadeIn;
        public uint FadeOut;
        public uint Lifespan;
        public uint EmitInterval;
        public uint EmitCount;
        public uint EmitterLifespan;
        public bool Gravity;
        public bool Wind;
        public bool Interpolate;
        public bool ZBuffer;
        public List<Color3> Colors;
    }
}
