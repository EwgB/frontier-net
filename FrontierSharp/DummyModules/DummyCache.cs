namespace FrontierSharp.DummyModules {
    using System;
    using System.Collections.Generic;

    using OpenTK;

    using Common;
    using Common.Grid;
    using Common.Util;

    internal class DummyCache : ICache {
        public float GetDetail(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public bool GetDump(List<string> args) {
            throw new NotImplementedException();
        }

        public float GetElevation(float x, float y) {
            throw new NotImplementedException();
        }

        public float GetElevation(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Vector3 GetNormal(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public bool GetPointAvailable(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Vector3 GetPosition(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public bool GetSize(List<string> args) {
            throw new NotImplementedException();
        }

        public SurfaceTypes GetSurface(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Color3 GetSurfaceColor(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public uint GetTree(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public void Purge() { /* Do nothing */ }

        public void RenderDebug() { /* Do nothing */ }

        public void Update(double stopAt) { /* Do nothing */ }

        public void UpdatePage(int worldX, int worldY, long stopAt) { /* Do nothing */ }
    }
}
