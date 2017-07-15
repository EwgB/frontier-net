namespace FrontierSharp.Common.Shaders {
    /// <summary>Loads in fragment and vertex shaders.</summary>
    public interface IShaders : IModule {
        void SelectShader(FShaderTypes fShader);
        void SelectShader(VShaderTypes vShader);
    }
}
