namespace FrontierSharp.Common.Shaders {
    public interface IShaders : IModule {
        void SelectShader(FShaderTypes fShader);
        void SelectShader(VShaderTypes vShader);
    }
}
