namespace FrontierSharp.Interfaces {
    using Property;

    public interface IScene : IHasProperties, IModule {
        /// <summary>How far it is from the center of the terrain grid to the outer edge</summary>
        float VisibleRange { get; }

        void Render();
        void RenderDebug();
    }
}
