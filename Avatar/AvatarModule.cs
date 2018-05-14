namespace FrontierSharp.Avatar {
    using Ninject.Modules;

    using Common.Avatar;

    public class AvatarModule : NinjectModule {

        private readonly bool useDummy;

        public AvatarModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<IAvatar>().To<DummyAvatar>().InSingletonScope();
            } else {
                Bind<IAvatar>().To<AvatarImpl>().InSingletonScope();
            }
        }
    }
}
