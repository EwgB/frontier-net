namespace FrontierSharp.Common {
    /// <summary>A (theoretically) cross-platform font-loading system.</summary>
    public interface IText : IModule, IRenderable {
        void Print (string format, params object[] args);
        /* From Text.h
        char* TextBytes (int bytes);
        void  TextCreate (int width, int height);
         */
    }
}
