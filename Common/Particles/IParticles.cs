namespace FrontierSharp.Common.Particles {
    using Property;

    public interface IParticles : IModule, IHasProperties, IRenderable {
        IParticlesProperties ParticlesProperties { get; }
    }
}
