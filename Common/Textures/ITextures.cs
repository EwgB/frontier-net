namespace FrontierSharp.Common.Textures {
    using System;

    public interface ITextures : IModule, IDisposable {
        uint TextureIdFromName(string name);
    }
}
