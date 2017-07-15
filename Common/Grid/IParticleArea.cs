namespace FrontierSharp.Common.Grid {
    /// <summary>
    /// This is a IGridData subtype, concerned with filling out the world with
    /// appropriate particle effects. You can use IParticles directly to create 
    /// localized one-off effects, but this is where the large persistant effects
    /// are managed.
    /// </summary>
    public interface IParticleArea : IGridData {
    }
}
