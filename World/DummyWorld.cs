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

        public bool WindFromWest { get; set; }

        public int MapId => 0;

        public DummyWorld(IRegion region) {
            this.region = region;
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Generate(int seed) { /* Do nothing */ }
        public void Save() { /* Do nothing */ }
        public void Load(int seed) { /* Do nothing */ }

        public float GetWaterLevel(Vector2 coord) => 0;
        public float GetWaterLevel(float x, float y) => 0;

        private readonly ITree tree;
        public ITree GetTree(uint id) => this.tree;


        private readonly IRegion region;
        public IRegion GetRegion(int x, int y) => this.region;
        public IRegion GetRegionFromPosition(int worldX, int worldY) => this.region;

        private readonly Cell cell;
        public Cell GetCell(int worldX, int worldY) => this.cell;

        public Color3 GetColor(int worldX, int worldY, SurfaceColors c) => Color3.Magenta;
    }
}