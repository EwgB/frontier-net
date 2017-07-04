namespace FrontierSharp.Common {
    using Util;

    public interface ITextures : IModule {
        uint TextureIdFromName(string name);
    }
}
