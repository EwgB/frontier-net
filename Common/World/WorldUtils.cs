﻿namespace FrontierSharp.Common.World {
    public static class WorldUtils {
        public static int REGION_SIZE = 128;
        public static int REGION_HALF = (REGION_SIZE / 2);
        public static int WORLD_GRID = 256;
        public static int WORLD_GRID_EDGE = (WORLD_GRID + 1);
        public static int WORLD_GRID_CENTER = (WORLD_GRID / 2);
        public static int WORLD_SIZE_METERS = (REGION_SIZE * WORLD_GRID);
        public static float GRAVITY = 9.5f;
    }
}
