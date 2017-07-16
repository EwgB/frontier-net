namespace FrontierSharp.Textures {
    using Ninject.Modules;

    using Common.Textures;

    public class TexturesModule : NinjectModule {
        private readonly bool useDummy;

        public TexturesModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<ITextures>().To<DummyTextures>();
            } else {
                Bind<ITextures>().To<TexturesImpl>();
            }
        }
    }
}