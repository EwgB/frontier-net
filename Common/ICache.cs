namespace FrontierSharp.Common {
    using System.Collections.Generic;

    using OpenTK;

    using Grid;
    using Util;

    /// <summary>Generates, stores, and fetches the pages of terrain data.</summary>
    public interface ICache : ITimeCapped {
        void Purge();
        void RenderDebug();
        void UpdatePage(int worldX, int worldY, double stopAt);

        //Look up individual cell data

        float GetDetail(int worldX, int worldY);
        bool GetDump(List<string> args);
        float GetElevation(int worldX, int worldY);
        float GetElevation(float x, float y);
        Vector3 GetNormal(int worldX, int worldY);
        bool IsPointAvailable(int worldX, int worldY);
        Vector3 GetPosition(int worldX, int worldY);
        bool GetSize(List<string> args);
        SurfaceTypes GetSurface(int worldX, int worldY);
        Color3 GetSurfaceColor(int worldX, int worldY);
        uint GetTree(int worldX, int worldY);
    }
}
