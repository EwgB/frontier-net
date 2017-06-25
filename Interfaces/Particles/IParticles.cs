namespace FrontierSharp.Interfaces.Particles {
    using Property;

    public interface IParticles : IHasProperties, IModule {
        IParticlesProperties ParticlesProperties { get; }
    }
}
