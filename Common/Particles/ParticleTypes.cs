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
        public int FadeIn;
        public int FadeOut;
        public int Lifespan;
        public int EmitInterval;
        public int EmitCount;
        public int EmitterLifespan;
        public bool Gravity;
        public bool Wind;
        public bool Interpolate;
        public bool ZBuffer;
        public List<Color3> Colors;
    }
}
