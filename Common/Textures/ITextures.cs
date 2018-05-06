namespace FrontierSharp.Common.Textures {
    using System;

    /// <summary>Loads in textures.</summary>
    public interface ITextures : IModule, IDisposable {
        int TextureIdFromName(string name);
    }
}
