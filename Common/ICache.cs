namespace FrontierSharp.Common {
    using System.Collections.Generic;

    using OpenTK;

    using Grid;
    using Util;

    public interface ICache : ITimeCapped {
        void Purge();
        void RenderDebug();
        void UpdatePage(int world_x, int world_y, long stopAt);

        //Look up individual cell data

        float GetDetail(int world_x, int world_y);
        bool GetDump(List<string> args);
        float GetElevation(int world_x, int world_y);
        float GetElevation(float x, float y);
        Vector3 GetNormal(int world_x, int world_y);
        bool GetPointAvailable(int world_x, int world_y);
        Vector3 GetPosition(int world_x, int world_y);
        bool GetSize(List<string> args);
        SurfaceTypes GetSurface(int world_x, int world_y);
        Color3 GetSurfaceColor(int world_x, int world_y);
        uint GetTree(int world_x, int world_y);
    }
}
