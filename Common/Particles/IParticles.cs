namespace FrontierSharp.Common.Particles {
    using Property;

    public interface IParticles : IHasProperties, IModule {
        IParticlesProperties ParticlesProperties { get; }
    }
}
