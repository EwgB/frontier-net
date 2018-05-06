namespace FrontierSharp.Textures {
    using Common.Textures;

    internal class DummyTextures : ITextures {
        public void Init() { /*Do nothing*/ }
        public void Update() { /*Do nothing*/ }
        public void Dispose() { /*Do nothing*/ }

        public int TextureIdFromName(string name) => 0;
    }
}
