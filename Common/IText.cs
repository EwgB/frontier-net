namespace FrontierSharp.Common {
    public interface IText : IModule, IRenderable {
        void Print (string format, params object[] args);
        /* From Text.h
        char* TextBytes (int bytes);
        void  TextCreate (int width, int height);
         */
    }
}
