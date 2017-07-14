namespace FrontierSharp.Common.Grid {
    using Util;

    /// <summary>Anything to be managed should implement this.</summary>
    public interface IGridData : ITimeCapped, IRenderable {
        Coord GridPosition { get; }
        bool IsReady { get; }

        void Invalidate();
        
        void Set(Coord gridPosition, int gridDistance);
    }
}
