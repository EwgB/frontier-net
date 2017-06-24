namespace FrontierSharp.Interfaces {
    using Property;

    public interface IScene : IHasProperties {
        void Render();
        void RenderDebug();
    }
}
