namespace FrontierSharp {
    using Ninject;

    using DummyModules;
    using Interfaces;

    internal class Program {
        private static void Main(string[] args) {
            using (IKernel kernel = new StandardKernel()) {

                // Set up dependecies
                kernel.Bind<IParticles>().To<DummyParticles>();

                using (var frontier = kernel.Get<Frontier>()) {
                    frontier.Run(30.0);
                }
            }
        }
    }
}
