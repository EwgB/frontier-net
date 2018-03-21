namespace FrontierSharp.World {
    using System;

    using Common.Grid;

    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.Tree;
    using Common.Util;
    using Common.World;

    internal class WorldImpl : IWorld {

        #region Public properties

        public IProperties Properties {
            get {
                throw new NotImplementedException();
            }
        }

        private uint mapId = 0;
        public uint MapId => this.mapId;

        public bool WindFromWest { get; private set; }

        #endregion


        public ITree GetTree(uint id) {
            throw new NotImplementedException();
        }

        public void Generate(uint seed) {
            throw new NotImplementedException();
        }

        public IRegion GetRegion(int x, int y) {
            throw new NotImplementedException();
        }

        public IRegion GetRegionFromPosition(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Cell GetCell(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Color3 GetColor(int worldX, int worldY, SurfaceColors c) {
            throw new NotImplementedException();
        }

        public float GetWaterLevel(Vector2 coord) {
            throw new NotImplementedException();
        }

        public float GetWaterLevel(float x, float y) {
            throw new NotImplementedException();
        }

        public void Init() {
            throw new NotImplementedException();
        }

        public void Load(uint seed) {
            throw new NotImplementedException();
        }

        public void Save() {
            throw new NotImplementedException();
        }

        public void Update() {
            throw new NotImplementedException();
        }
    }
}
