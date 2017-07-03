namespace FrontierSharp.Common.Particles {
    using System.Collections.Generic;

    using OpenTK;

    using Util;

    public enum ParticleBlend { Alpha, Add }

    public enum ParticleType { Facer, PanelX, PanelY, PanelZ }

    public struct ParticleSet {
        string texture;
        BoundingBox volume;
        BoundingBox speed;
        Vector3 acceleration;
        BoundingBox size;
        Vector3 origin;
        Vector3 rotation;
        Vector3 spin;
        ParticleBlend blend;
        ParticleType panelType;
        uint fadeIn;
        uint fadeOut;
        uint lifespan;
        uint emitInterval;
        uint emitCount;
        uint emitterLifespan;
        bool gravity;
        bool wind;
        bool interpolate;
        bool zBuffer;
        List<Color3> colors;
    }
}
