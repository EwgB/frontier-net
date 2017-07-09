namespace FrontierSharp.Common.World {
    public static class WorldUtils {
        public const int REGION_SIZE = 128;
        public const int REGION_HALF = (REGION_SIZE / 2);
        public const int WORLD_GRID = 256;
        public const int WORLD_GRID_EDGE = (WORLD_GRID + 1);
        public const int WORLD_GRID_CENTER = (WORLD_GRID / 2);
        public const int WORLD_SIZE_METERS = (REGION_SIZE * WORLD_GRID);
        public const float GRAVITY = 9.5f;
    }
}
