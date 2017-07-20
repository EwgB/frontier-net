namespace FrontierSharp.Animation {
    using OpenTK;

    using Common.Animation;

    internal class DummyFigure : IFigure {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public void Update() { /* Do nothing */ }
        public void Render() { /* Do nothing */ }
        public void RenderSkeleton() { /* Do nothing */ }
        public void Animate(IAnimation animation, float delta) { /* Do nothing */ }
    }
}
