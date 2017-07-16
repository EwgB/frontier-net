namespace FrontierSharp.Shaders {
    using Ninject.Modules;

    using Common.Shaders;

    public class ShadersModule : NinjectModule {
        public override void Load() {
            Bind<IShaders>().To<DummyShaders>();
        }
    }
}