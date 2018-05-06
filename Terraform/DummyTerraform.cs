namespace FrontierSharp.Terraform {
    using Common.Grid;
    using Common.Terraform;
    using Common.Util;

    internal class DummyTerraform : ITerraform {
        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }
        public void Average() { /* Do nothing */ }
        public void Coast() { /* Do nothing */ }
        public void Colors() { /* Do nothing */ }
        public Color3 GenerateColor(SurfaceColor color, float moisture, float temperature, int seed) => Color3.Magenta;
        public void Climate() { /* Do nothing */ }
        public void Fill() { /* Do nothing */ }
        public void Flora() { /* Do nothing */ }
        public void Lakes(int count) { /* Do nothing */ }
        public void Oceans() { /* Do nothing */ }
        public void Prepare() { /* Do nothing */ }
        public void Rivers(int count) { /* Do nothing */ }
        public void Zones() { /* Do nothing */ }
    }
}
