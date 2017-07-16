namespace FrontierSharp.Text {
    using Ninject.Modules;

    using Common;

    public class TextModule : NinjectModule {
        public override void Load() {
            Bind<IText>().To<DummyText>();
        }
    }
}