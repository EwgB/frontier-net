namespace FrontierSharp.Cache {
    using OpenTK;

    using Common;
    using Common.Grid;
    using Common.Util;

    internal class DummyCache : ICache {
        public float GetDetail(int worldX, int worldY) => 0;
        public float GetElevation(float x, float y) => 0;
        public float GetElevation(int worldX, int worldY) => 0;
        public Vector3 GetNormal(int worldX, int worldY) => Vector3.UnitY;
        public bool IsPointAvailable(int worldX, int worldY) => false;
        public Vector3 GetPosition(int worldX, int worldY) => Vector3.Zero;
        public SurfaceTypes GetSurface(int worldX, int worldY) => SurfaceTypes.Grass;
        public Color3 GetSurfaceColor(int worldX, int worldY) => Color3.Aquamarine;
        public uint GetTree(int worldX, int worldY) => 0;

        public void Purge() { /* Do nothing */ }
        public void RenderDebug() { /* Do nothing */ }
        public void Update(double stopAt) { /* Do nothing */ }
        public void UpdatePage(int worldX, int worldY, double stopAt) { /* Do nothing */ }
        public void PrintSize() { /* Do nothing */ }
        public void Dump() { /* Do nothing */ }
    }
}
