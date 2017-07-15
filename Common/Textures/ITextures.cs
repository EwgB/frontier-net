namespace FrontierSharp.Common.Textures {
    using System;

    /// <summary>Loads in textures.</summary>
    public interface ITextures : IModule, IDisposable {
        uint TextureIdFromName(string name);
    }
}
