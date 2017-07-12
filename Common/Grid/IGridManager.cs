namespace FrontierSharp.Common.Grid {
    /// <summary>
    /// The grid manager. You need one of these for each type of object you plan to manage.
    /// Do not implement this interface directly. Instead, inherit from the abstract class GridManager.
    /// </summary>
    public interface IGridManager : ITimeCapped, IRenderable {
        uint ItemsReadyCount { get; }
        uint ItemsViewableCount { get; }

        void Init(IGridData items, uint gridSize, uint itemSize);
        void Clear();
        /* From Grid.h
        void RestartProgress () { _list_pos = 0; };
        */
    }
}
