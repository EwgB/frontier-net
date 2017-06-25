namespace FrontierSharp.Interfaces {
    using Property;

    public interface IScene : IHasProperties, IModule {
        void Render();
        void RenderDebug();
    }
}
