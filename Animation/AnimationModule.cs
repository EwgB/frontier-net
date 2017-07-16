namespace FrontierSharp.Animation {
    using Ninject.Modules;

    using Common.Animation;

    public class AnimationModule : NinjectModule {
        public override void Load() {
            Bind<IFigure>().To<DummyFigure>();
        }
    }
}
