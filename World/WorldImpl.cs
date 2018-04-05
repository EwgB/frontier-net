namespace FrontierSharp.World {
    using System;

    using Common.Grid;

    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.Tree;
    using Common.Util;
    using Common.World;

    using MersenneTwister;

    using OpenTK.Graphics.OpenGL;

    internal class WorldImpl : IWorld {

        #region Constants

        /// <summary>
        /// We keep a list of random numbers so we can have deterMath.Ministic "randomness". This is the size of that list.
        /// </summary>
        private const int NOISE_BUFFER = 1024;

        /// <summary>
        /// This is the size of the grid of this.treess.  The total number of this.trees species 
        /// in the world is the square of this value, Math.Minus one. ("this.trees zero" is actually
        /// "no this.treess at all".)
        /// </summary>
        private const uint TREE_TYPES = 6;

        #endregion


        #region Modules



        #endregion


        #region Public properties

        public IProperties Properties {
            get { throw new NotImplementedException(); }
        }

        public int MapId { get; private set; }
        public int Seed { get; private set; }
        public bool WindFromWest { get; private set; }

        #endregion


        #region Private members

        private Random random;

        private bool northernHemisphere;
        private int riverCount;
        private int lakeCount;
        private uint canopy;

        private IRegion[,] map = new IRegion[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];

        private ITree[,] trees = new ITree[TREE_TYPES, TREE_TYPES];

        private double[] noiseF = new double[NOISE_BUFFER];
        private int[] noiseI = new int[NOISE_BUFFER];

        #endregion


        public ITree GetTree(uint id) {
            throw new NotImplementedException();
        }

        public void Generate(int seed) {
            this.random = Randoms.Create(seed);
            this.Seed = seed;

            for (var x = 0; x < NOISE_BUFFER; x++) {
                this.noiseI[x] = this.random.Next();
                this.noiseF[x] = this.random.NextDouble();
            }

            BuildTrees();
            this.WindFromWest = (this.random.Next() % 2 == 0);
            this.northernHemisphere = (this.random.Next() % 2 == 0);
            this.riverCount = 4 + this.random.Next() % 4;
            this.lakeCount = 1 + this.random.Next() % 4;
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
