namespace FrontierSharp.Animation {
    using Ninject.Modules;

    using Common.Animation;

    public class AnimationModule : NinjectModule {
        private readonly bool useDummy;

        public AnimationModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (useDummy) {
                Bind<IFigure>().To<DummyFigure>();
            } else {
                Bind<IFigure>().To<Figure>();
            }
        }
    }
}
