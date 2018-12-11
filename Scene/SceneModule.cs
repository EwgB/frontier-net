namespace FrontierSharp.Scene {
    using Ninject.Modules;

    using Common.Scene;

    public class SceneModule : NinjectModule {
        private readonly bool useDummy;

        public SceneModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<IScene>().To<DummyScene>().InSingletonScope();
            } else {
                Bind<IScene>().To<SceneImpl>().InSingletonScope();
            }
        }
    }
}