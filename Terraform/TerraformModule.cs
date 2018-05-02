namespace FrontierSharp.Terraform {
    using Ninject.Modules;

    using Common.Terraform;

    public class TerraformModule : NinjectModule {
        private readonly bool useDummy;

        public TerraformModule(bool useDummy) {
            this.useDummy = useDummy;
        }

        public override void Load() {
            if (this.useDummy) {
                Bind<ITerraform>().To<DummyTerraform>().InSingletonScope();
            } else {
                Bind<ITerraform>().To<TerraformImpl>().InSingletonScope();
            }
        }
    }
}