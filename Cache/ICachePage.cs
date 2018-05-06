namespace FrontierSharp.Cache {
    using Common.Grid;
    using Common.Util;

    using OpenTK;

    public interface ICachePage {
        bool IsExpired { get; }
        bool IsReady();
        float GetElevation(int x, int y);
        float GetDetail(int x, int y);
        int GetTree(int x, int y);
        Vector3 GetPosition(int x, int y);
        Vector3 GetNormal(int x, int y);
        Color3 GetColor(int x, int y);
        SurfaceTypes GetSurface(int x, int y);
        void Render();
        void Build(double stopAt);
        void Save();
        void Load(int originX, int originY);
    }
}