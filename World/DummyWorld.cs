namespace FrontierSharp.World {
    using Common.Grid;

    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.Tree;
    using Common.Util;
    using Common.World;

    internal class DummyWorld : IWorld {
        public IProperties Properties { get; }

        public int Seed { get; }
        public bool WindFromWest { get; set; }
        public int TreeCanopy { get; }
        public bool NorthernHemisphere { get; }

        public int MapId => 0;

        public float GetNoiseF(int index) => 0;
        public int GetNoiseI(int index) => 0;

        public DummyWorld() {
            this.dummyRegion = new Region();
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }

        public void Generate(int seed) { /* Do nothing */ }
        public void Save() { /* Do nothing */ }
        public void Load(int seed) { /* Do nothing */ }

        public float GetWaterLevel(Vector2 coord) => 0;
        public float GetWaterLevel(int x, int y) => 0;

        private readonly ITree tree;
        public ITree GetTree(int id) => this.tree;
        public int GetTreeType(float moisture, float temperature) => 1;

        private readonly Region dummyRegion;
        public Region GetRegion(int x, int y) => this.dummyRegion;
        public Region GetRegionFromPosition(int worldX, int worldY) => this.dummyRegion;
        public void SetRegion(int x, int y, Region region) { /* Do nothing */ }

        private readonly Cell cell;
        public Cell GetCell(int worldX, int worldY) => this.cell;

        public Color3 GetColor(int worldX, int worldY, SurfaceColor c) => Color3.Magenta;
    }
}