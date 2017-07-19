namespace FrontierSharp.Renderer {
    using Ninject.Modules;

    using Common.Renderer;

    public class RendererModule : NinjectModule {
        private readonly bool useDummy;

        public RendererModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<IRenderer>().To<DummyRenderer>().InSingletonScope();
            } else {
                Bind<IRenderer>().To<RendererImpl>().InSingletonScope();
            }
        }
    }
}