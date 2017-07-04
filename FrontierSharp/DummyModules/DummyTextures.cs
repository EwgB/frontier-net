namespace FrontierSharp.DummyModules {
    using Common;

    internal class DummyTextures : ITextures {
        public void Init() {
            // Do nothing
        }

        public void Update() {
            // Do nothing
        }

        public uint TextureIdFromName(string name) {
            return 0;
        }
    }
}
