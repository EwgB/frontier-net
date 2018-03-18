namespace FrontierSharp.Common.World {
    public static class WorldUtils {
        public const int REGION_SIZE = 128;
        public const int REGION_HALF = (REGION_SIZE / 2);
        public const int WORLD_GRID = 256;
        public const int WORLD_GRID_EDGE = (WORLD_GRID + 1);
        public const int WORLD_GRID_CENTER = (WORLD_GRID / 2);
        public const int WORLD_SIZE_METERS = (REGION_SIZE * WORLD_GRID);
        public const float GRAVITY = 9.5f;
        /// <summary>
        /// This is used to scale the z value of normals. Lower numbers make
        /// the normals more extreme, exaggerate the lighting.
        /// </summary>
        public const float NORMAL_SCALING = 0.6f;
        public const float FREEZING = 0.32f;
    }
}
