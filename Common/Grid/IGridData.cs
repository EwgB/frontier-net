namespace FrontierSharp.Common.Grid {
    using Util;

    /// <summary>Anything to be managed should implement this.</summary>
    public interface IGridData : ITimeCapped, IRenderable {
        Coord GridPosition { get; }

        /*
          virtual bool      Ready () { return true; };
          virtual void      Render () {};
          virtual void      Set (int grid_x, int grid_y, int grid_distance) {};
          virtual void      Update (long stop) {};
          virtual void      Invalidate () {}; 
          virtual unsigned  Sizeof () { return sizeof (this); }; 
        */
    }
}
