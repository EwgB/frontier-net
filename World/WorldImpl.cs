namespace FrontierSharp.World {
    using System;

    using Common.Grid;

    using OpenTK;

    using Common.Property;
    using Common.Region;
    using Common.Terraform;
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

        //The dither map scatters surface data so that grass colorings end up in adjacent regions.
        private const int DITHER_SIZE = (WorldUtils.REGION_SIZE / 2);
        
        //How much space in a region is spent interpolating between itself and its neighbors.
        private const int BLEND_DISTANCE = (WorldUtils.REGION_SIZE / 4);
        
        #endregion


        #region Modules

        private readonly ITerraform terraform;

        #endregion


        #region Public properties

        public IProperties Properties {
            get { throw new NotImplementedException(); }
        }

        public uint MapId { get; private set; }
        public uint Seed { get; private set; }
        public bool WindFromWest { get; private set; }
        public uint TreeCanopy { get; private set; }

        #endregion


        #region Private members

        private Random random;

        private bool northernHemisphere;
        private int riverCount;
        private int lakeCount;

        private readonly IRegion[,] map = new IRegion[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];

        private readonly ITree[,] trees = new ITree[TREE_TYPES, TREE_TYPES];

        private readonly double[] noiseF = new double[NOISE_BUFFER];
        private readonly int[] noiseI = new int[NOISE_BUFFER];

        private readonly Coord[,] dithermap = new Coord[DITHER_SIZE, DITHER_SIZE];

        #endregion


        public WorldImpl(ITerraform terraform) {
            this.terraform = terraform;
        }

        public void Init() {
            //Fill in the dither table - a table of random offsets
            for (var y = 0; y < DITHER_SIZE; y++) {
                for (var x = 0; x < DITHER_SIZE; x++) {
                    this.dithermap[x, y] = new Coord(
                        this.random.Next() % DITHER_SIZE + this.random.Next() % DITHER_SIZE,
                        this.random.Next() % DITHER_SIZE + this.random.Next() % DITHER_SIZE);
                }
            }
        }


        public ITree GetTree(uint id) {
            var m = id % TREE_TYPES;
            var t = (id - m) / TREE_TYPES;
            return this.trees[m, t];
        }

        public void Generate(uint seed) {
            this.random = Randoms.Create((int) seed);
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
            this.terraform.Prepare();
            this.terraform.Oceans();
            this.terraform.Coast();
            this.terraform.Climate();
            this.terraform.Rivers(this.riverCount);
            this.terraform.Lakes(this.lakeCount);
            this.terraform.Climate(); //Do climate a second time now that rivers are in
            this.terraform.Zones();
            this.terraform.Climate(); //Now again, since we have added climate-modifying features (Mountains, etc.)
            this.terraform.Fill();
            this.terraform.Average();
            this.terraform.Flora();
            this.terraform.Colors();
            BuildMapTexture();
        }

        public IRegion GetRegion(int x, int y) {
            throw new NotImplementedException();
        }

        public IRegion GetRegionFromPosition(int worldX, int worldY) {
            throw new NotImplementedException();
        }

        public Cell GetCell(int worldX, int worldY) {
            float detail = Entropy(worldX, worldY);
            var bias = GetBiasLevel(worldX, worldY);
            var waterLevel = GetWaterLevel(worldX, worldY);
            var origin = new Coord(
                MathHelper.Clamp(worldX / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1),
                MathHelper.Clamp(worldY / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1));
            
            //Get our offset from the region origin as a pair of scalars.
            var blend = new Vector2(
                (float) (worldX % BLEND_DISTANCE) / BLEND_DISTANCE,
                (float) (worldY % BLEND_DISTANCE) / BLEND_DISTANCE);
            var left = ((origin.X + origin.Y) % 2) == 0;
            var offset = new Vector2(
                (float) ((worldX) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE,
                (float) ((worldY) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE);

            var result = new Cell {
                Detail = detail,
                WaterLevel = waterLevel
            };

            var upperLeft = new Coord(origin);
            var bottomRight = new Coord(
                (worldX + BLEND_DISTANCE) / WorldUtils.REGION_SIZE,
                (worldY + BLEND_DISTANCE) / WorldUtils.REGION_SIZE);

            IRegion upperLeftRegion;
            if (upperLeft == bottomRight) {
                upperLeftRegion = GetRegion(upperLeft.X, upperLeft.Y);
                result.Elevation = DoHeight(upperLeftRegion, offset, waterLevel, detail, bias);
                result.Elevation = DoHeightNoBlend(result.Elevation, upperLeftRegion, offset, waterLevel);
                return result;
            }

            upperLeftRegion = GetRegion(upperLeft.X, upperLeft.Y);
            var upperRightRegion = GetRegion(bottomRight.X, upperLeft.Y);
            var bottomLeftRegion = GetRegion(upperLeft.X, bottomRight.Y);
            var bottomRightRegion = GetRegion(bottomRight.X, bottomRight.Y);

            var upperLeftElevation = DoHeight(upperLeftRegion, offset, waterLevel, detail, bias);
            var upperRightElevation = DoHeight(upperRightRegion, offset, waterLevel, detail, bias);
            var bottomLeftElevation = DoHeight(bottomLeftRegion, offset, waterLevel, detail, bias);
            var bottomRightElevation = DoHeight(bottomRightRegion, offset, waterLevel, detail, bias);
            result.Elevation = MathUtils.InterpolateQuad(upperLeftElevation, upperRightElevation, bottomLeftElevation, bottomRightElevation, blend, left);
            result.Elevation = DoHeightNoBlend(result.Elevation, upperLeftRegion, offset, waterLevel);
            return result;
        }

        public Color3 GetColor(int worldX, int worldY, SurfaceColors c) {
            return Color3.Cornsilk;
            // TODO: convert
            /*
            Coord origin;
            int x, y;
            Vector2 offset;
            GLrgba c0, c1, c2, c3, result;
            IRegion r0, r1, r2, r3;

            x = Math.Max(worldX % DITHER_SIZE, 0);
            y = Math.Max(worldY % DITHER_SIZE, 0);
            worldX += dithermap[x, y].X;
            worldY += dithermap[x, y].Y;
            offset.X = (float)(worldX % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            offset.Y = (float)(worldY % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            origin.X = worldX / WorldUtils.REGION_SIZE;
            origin.Y = worldY / WorldUtils.REGION_SIZE;
            r0 = GetRegion(origin.X, origin.Y);
            r1 = GetRegion(origin.X + 1, origin.Y);
            r2 = GetRegion(origin.X, origin.Y + 1);
            r3 = GetRegion(origin.X + 1, origin.Y + 1);
            switch (c)
            {
            case SURFACE_COLOR_DIRT:
                c0 = r0.color_dirt;
                c1 = r1.color_dirt;
                c2 = r2.color_dirt;
                c3 = r3.color_dirt;
                break;
            case SURFACE_COLOR_ROCK:
                c0 = r0.color_rock;
                c1 = r1.color_rock;
                c2 = r2.color_rock;
                c3 = r3.color_rock;
                break;
            case SURFACE_COLOR_SAND:
                return GL.Rgba(0.98f, 0.82f, 0.42f);
            default:
            case SURFACE_COLOR_GRASS:
                c0 = r0.color_grass;
                c1 = r1.color_grass;
                c2 = r2.color_grass;
                c3 = r3.color_grass;
                break;
            }
            result.red = MathUtils.InterpolateQuad(c0.red, c1.red, c2.red, c3.red, offset);
            result.green = MathUtils.InterpolateQuad(c0.green, c1.green, c2.green, c3.green, offset);
            result.blue = MathUtils.InterpolateQuad(c0.blue, c1.blue, c2.blue, c3.blue, offset);
            return result;
            */
        }

        public float GetWaterLevel(Vector2 coord) {
            throw new NotImplementedException();
        }

        public float GetWaterLevel(float x, float y) {
            throw new NotImplementedException();
        }

        public void Load(uint seed) {
            //FILE* f;
            //char filename[256];
            //WHeader header;


            //sprintf(filename, "%sworld.sav", GameDirectory());
            //if (!(f = fopen(filename, "rb")))
            //{
            //    ConsoleLog("WorldLoad: Could not open file %s", filename);
            //    WorldGenerate(seed_in);
            //    return;
            //}
            //fread(&header, sizeof(header), 1, f);
            //fread(&planet, sizeof(planet), 1, f);
            //fclose(f);
            //ConsoleLog("WorldLoad: '%s' loaded.", filename);
            //BuildTrees();
            //BuildMapTexture();
        }

        public void Save() {
            //FILE* f;
            //char filename[256];
            //WHeader header;

            //return;
            //sprintf(filename, "%sworld.sav", GameDirectory());
            //if (!(f = fopen(filename, "wb")))
            //{
            //    ConsoleLog("WorldSave: Could not open file %s", filename);
            //    return;
            //}
            //header.version = FILE_VERSION;
            //header.seed = this.Seed;
            //header.WorldUtils.WORLD_GRID = WorldUtils.WORLD_GRID;
            //header.noise_buffer = NOISE_BUFFER;
            //header.map_bytes = sizeof(planet);
            //header.this.trees_types = TREE_TYPES;
            //fwrite(&header, sizeof(header), 1, f);
            //fwrite(&planet, sizeof(planet), 1, f);
            //fclose(f);
            //ConsoleLog("WorldSave: '%s' saved.", filename);
        }

        public void Update() {
            /* Do nothing */
        }


        #region Functions that are used when generating elevation data

        /// <summary>
        /// This modifies the passed elevation value AFTER region cross-fading is complete,
        /// for things that should not be mimicked by neighbors. (Like rivers.)
        /// </summary>
        private float DoHeightNoBlend(float elevationValue, IRegion region, Vector2 offset, float waterLevel) {
            if (!region.ShapeFlags.HasFlag(RegionFlag.RiverAny))
                return elevationValue;

            //if this river is strictly north / south
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverNS) && !region.ShapeFlags.HasFlag(RegionFlag.RiverW)) {
                //This makes the river bend side-to-side
                switch ((region.GridPosition.X + region.GridPosition.Y) % 6) {
                case 0:
                case 1:
                    offset.X += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.25f;
                    break;
                case 2:
                case 3:
                    offset.X += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.Y * 180))) * 0.1f;
                    break;
                case 4:
                case 5:
                    offset.X += (float) Math.Sin(offset.Y * 360 * MathUtils.DEGREES_TO_RADIANS) * 0.1f;
                    break;
                }
            }

            //if this river is strictly east / west
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverW) && !region.ShapeFlags.HasFlag(RegionFlag.RiverNS)) {
                //This makes the river bend side-to-side
                switch ((region.GridPosition.X + region.GridPosition.Y) % 4) {
                case 0:
                case 1:
                    offset.Y += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.25f;
                    break;
                case 2:
                case 3:
                    offset.Y += (float) Math.Abs(Math.Sin(MathHelper.DegreesToRadians(offset.X * 180))) * 0.1f;
                    break;
                }
            }

            //if this river curves around a bend
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverNW) && !region.ShapeFlags.HasFlag(RegionFlag.RiverSE))
                offset.X = offset.Y = offset.Length;

            offset = ComputeNewOffset(region, offset);

            Vector2 cen;
            cen.X = Math.Abs((offset.X - 0.5f) * 2);
            cen.Y = Math.Abs((offset.Y - 0.5f) * 2);
            var strength = cen.Length;
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverN) && offset.Y < 0.5f)
                strength = Math.Min(strength, cen.X);
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverS) && offset.Y >= 0.5f)
                strength = Math.Min(strength, cen.X);
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverW) && offset.X < 0.5f)
                strength = Math.Min(strength, cen.Y);
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverE) && offset.X >= 0.5f)
                strength = Math.Min(strength, cen.Y);
            if (strength < (region.RiverWidth / 2)) {
                strength *= 1 / (region.RiverWidth / 2);
                var delta = (elevationValue - waterLevel) + 4 * region.RiverWidth;
                elevationValue -= (delta) * (1 - strength);
            }

            return elevationValue;
        }

        private static Vector2 ComputeNewOffset(IRegion region, Vector2 offset) {
            Vector2 newOffset;
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverSE) && !region.ShapeFlags.HasFlag(RegionFlag.RiverNW)) {
                newOffset.X = 1 - offset.X;
                newOffset.Y = 1 - offset.Y;
                newOffset.X = newOffset.Y = newOffset.Length;
                offset = newOffset;
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.RiverNE) && !region.ShapeFlags.HasFlag(RegionFlag.RiverSW)) {
                newOffset.X = 1 - offset.X;
                newOffset.Y = offset.Y;
                newOffset.X = newOffset.Y = newOffset.Length;
                offset = newOffset;
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.RiverSW) && !region.ShapeFlags.HasFlag(RegionFlag.RiverNE)) {
                newOffset.X = offset.X;
                newOffset.Y = 1 - offset.Y;
                newOffset.X = newOffset.Y = newOffset.Length;
                offset = newOffset;
            }

            return offset;
        }

        /// <summary>
        /// This takes the given properties and generates a sinGL.e unit of elevation data,
        /// according to the local region rules.
        /// </summary>
        /// <param name="region">region</param>
        /// <param name="offset"></param>
        /// <param name="waterLevel">the water level</param>
        /// <param name="detail">the height of the rolling hills</param>
        /// <param name="bias">direct height added on to detail</param>
        /// <returns></returns>
        private float DoHeight(IRegion region, Vector2 offset, float waterLevel, float detail, float bias) {
            //Modify the detail values before they are applied

            if (region.ShapeFlags.HasFlag(RegionFlag.Crater)) {
                detail = Math.Max(detail, 0.5f);
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.Tiered)) {
                if (detail < 0.2f)
                    detail += 0.2f;
                else if (detail < 0.5f)
                    detail -= 0.2f;
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.Crack)) {
                if (detail > 0.2f && detail < 0.3f)
                    detail = 0;
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.Sinkhole)) {
                var x = Math.Abs(offset.X - 0.5f);
                var y = Math.Abs(offset.Y - 0.5f);
                if (detail > Math.Max(x, y))
                    detail /= 4;
            }

            //Soften up the banks of a river 
            if ((region.ShapeFlags & RegionFlag.RiverAny) == RegionFlag.RiverAny) {
                var centerX = Math.Abs((offset.X - 0.5f) * 2);
                var centerY = Math.Abs((offset.Y - 0.5f) * 2);
                var strength = Math.Max(Math.Min(centerX, centerY), 0.1f);
                detail *= strength;
            }


            //Apply the values!
            var val = waterLevel + detail * region.GeoDetail + bias;
            if (region.Climate == ClimateTypes.Swamp) {
                val -= region.GeoDetail / 2;
                val = Math.Max(val, region.GeoWater - 0.5f);
            }

            //Modify the final value.
            if (region.ShapeFlags.HasFlag(RegionFlag.Mesas)) {
                var x = Math.Abs(offset.X - 0.5f) / 5;
                var y = Math.Abs(offset.Y - 0.5f) / 5;
                if ((detail + 0.01f) < (x + y)) {
                    val += 5;
                }
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.CanyonNS)) {
                var x = Math.Abs(offset.X - 0.5f) * 2;
                if (x + detail < 0.5f)
                    val -= Math.Min(region.GeoDetail, 10);
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.Beach) && val < region.CliffThreshold && val > 0) {
                val /= region.CliffThreshold;
                val *= val;
                val *= region.CliffThreshold;
                val += 0.2f;
            }

            if (region.ShapeFlags.HasFlag(RegionFlag.BeachCliff) && val < region.CliffThreshold && val > -0.1f) {
                val -= Math.Min(region.CliffThreshold, 10);
            }

            //if a point dips below the water table, make sure it's not too close to the water,
            //to avoid uGL.y z-fighting
            //if (val < bias)
            //val = Math.Min (val, bias - 2.5f);
            return val;
        }

        private void BuildTrees() {
            var rotator = 0;
            for (uint m = 0; m < TREE_TYPES; m++) {
                uint t;
                for (t = 0; t < TREE_TYPES; t++) {
                    bool isCanopy;
                    if ((m == TREE_TYPES / 2) && (t == TREE_TYPES / 2)) {
                        isCanopy = true;
                        this.TreeCanopy = m + t * TREE_TYPES;
                    } else
                        isCanopy = false;

                    this.trees[m, t].Create(isCanopy, (float) m / TREE_TYPES, (float) t / TREE_TYPES, rotator++);
                }
            }
        }

        private void BuildMapTexture() {
            if (this.MapId == 0) {
                GL.GenTextures(1, out uint mapId);
                this.MapId = mapId;
            }

            GL.BindTexture(TextureTarget.Texture2D, this.MapId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float) TextureMagFilter.Nearest);

            var buffer = new byte[WorldUtils.WORLD_GRID * WorldUtils.WORLD_GRID * 3];

            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    //Flip it vertically, because the OpenGL texture coord system is retarded.
                    var yy = (WorldUtils.WORLD_GRID - 1) - y;
                    var r = this.map[x, yy];
                    var bufferIndex = (x + y * WorldUtils.WORLD_GRID) * 3;
                    buffer[bufferIndex] = (byte) (r.ColorMap.R * 255);
                    buffer[bufferIndex + 1] = (byte) (r.ColorMap.G * 255);
                    buffer[bufferIndex + 2] = (byte) (r.ColorMap.B * 255);
                }
            }

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID, 0, PixelFormat.Rgb,
                PixelType.UnsignedByte, buffer);
        }

        private float GetBiasLevel(int worldX, int worldY) {
            return 0;
            //TODO: convert
            /*
            Coord origin;
            Vector2 offset;
            IRegion rul, rur, rbl, rbr; //Four corners: upper left, upper right, etc.

            worldX += REGION_HALF;
            worldY += REGION_HALF;
            origin.X = worldX / WorldUtils.REGION_SIZE;
            origin.Y = worldY / WorldUtils.REGION_SIZE;
            origin.X = MathHelper.Clamp(origin.X, 0, WorldUtils.WORLD_GRID - 1);
            origin.Y = MathHelper.Clamp(origin.Y, 0, WorldUtils.WORLD_GRID - 1);
            offset.X = (float) ((worldX) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            offset.Y = (float) ((worldY) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            rul = GetRegion(origin.X, origin.Y);
            rur = GetRegion(origin.X + 1, origin.Y);
            rbl = GetRegion(origin.X, origin.Y + 1);
            rbr = GetRegion(origin.X + 1, origin.Y + 1);
            return MathUtils.InterpolateQuad(rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset,
                ((origin.X + origin.Y) % 2) == 0);
            */
        }

        private float GetWaterLevel(int worldX, int worldY) {
            return 0;
            //TODO: convert
            /*
            Coord origin;
            Vector2 offset;
            IRegion rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.

            worldX += REGION_HALF;
            worldY += REGION_HALF;
            origin.X = worldX / WorldUtils.REGION_SIZE;
            origin.Y = worldY / WorldUtils.REGION_SIZE;
            origin.X = MathHelper.Clamp(origin.X, 0, WorldUtils.WORLD_GRID - 1);
            origin.Y = MathHelper.Clamp(origin.Y, 0, WorldUtils.WORLD_GRID - 1);
            offset.X = (float)((worldX) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            offset.Y = (float)((worldY) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            rul = GetRegion(origin.X, origin.Y);
            rur = GetRegion(origin.X + 1, origin.Y);
            rbl = GetRegion(origin.X, origin.Y + 1);
            rbr = GetRegion(origin.X + 1, origin.Y + 1);
            return MathUtils.InterpolateQuad(rul.GeoWater, rur.GeoWater, rbl.GeoWater, rbr.GeoWater, offset, ((origin.X + origin.Y) % 2) == 0);
            */
        }
    }

    #endregion
}

/*

#define FILE_VERSION      1

struct WHeader
{
    int version;
    uint seed;
    int WorldUtils.WORLD_GRID;
    int noise_buffer;
    int this.trees_types;
    int map_bytes;
};

uint WorldTreeType(float moisture, float temperature)
{

    int m, t;

    m = (int)(moisture * TREE_TYPES);
    t = (int)(temperature * TREE_TYPES);
    m = MathHelper.Clamp(m, 0, TREE_TYPES - 1);
    t = MathHelper.Clamp(t, 0, TREE_TYPES - 1);
    return m + t * TREE_TYPES;

}

char* WorldLocationName(int worldX, int worldY)
{

    static char result[20];
    char lat[20];
    char lng[20];

    worldX /= WorldUtils.REGION_SIZE;
    worldY /= WorldUtils.REGION_SIZE;
    worldX -= WorldUtils.WORLD_GRID_CENTER;
    worldY -= WorldUtils.WORLD_GRID_CENTER;
    if (!worldX && !worldY)
        return "Equatorial meridian";
    if (worldX == 0)
        strcpy(lng, "meridian");
    else if (worldX < 0)
        sprintf(lng, "%d west", Math.Abs(worldX));
    else
        sprintf(lng, "%d east", worldX);
    if (worldY == 0)
        strcpy(lat, "Equator");
    else if (worldY < 0)
        sprintf(lat, "%d north", Math.Abs(worldY));
    else
        sprintf(lat, "%d south", worldY);
    sprintf(result, "%s, %s", lat, lng);
    return result;

}

float WorldnoiseF(int index)
{

    index = Math.Abs(index % NOISE_BUFFER);
    return this.noiseF[index];

}

uint WorldnoiseI(int index)
{

    index = Math.Abs(index % NOISE_BUFFER);
    return this.noiseI[index];

}

IRegion GetRegion(int index_x, int index_y)
{

    return this.map[index_x, index_y];

}

void WorldRegionSet(int index_x, int index_y, IRegion val)
{

    this.map[index_x, index_y] = val;

}

IRegion WorldRegionFromPosition(int worldX, int worldY)
{

    worldX = Math.Max(worldX, 0);
    worldY = Math.Max(worldY, 0);
    worldX += dithermap[worldX % DITHER_SIZE, worldY % DITHER_SIZE].X;
    worldY += dithermap[worldX % DITHER_SIZE, worldY % DITHER_SIZE].Y;
    worldX /= WorldUtils.REGION_SIZE;
    worldY /= WorldUtils.REGION_SIZE;
    if (worldX >= WorldUtils.WORLD_GRID || worldY >= WorldUtils.WORLD_GRID)
        return this.map[0, 0];
    return this.map[worldX, worldY];

}

IRegion WorldRegionFromPosition(float worldX, float worldY)
{

    return WorldRegionFromPosition((int)worldX, (int)worldY);

}

uint WorldMap()
{

    return MapId;

}

World* WorldPtr()
{

    return &planet;

}

void WorldTexturePurge()
{

    uint m, t;

    for (m = 0; m < TREE_TYPES; m++)
    {
        for (t = 0; t < TREE_TYPES; t++)
        {
            this.trees[m, t].TexturePurge();
        }
    }
    BuildMapTexture();

}

char* WorldDirectionFromAngle(float angle)
{

    char* direction;

    direction = "North";
    if (angle < 22.5f)
        direction = "North";
    else if (angle < 67.5f)
        direction = "Northwest";
    else if (angle < 112.5f)
        direction = "West";
    else if (angle < 157.5f)
        direction = "Southwest";
    else if (angle < 202.5f)
        direction = "South";
    else if (angle < 247.5f)
        direction = "Southeast";
    else if (angle < 292.5f)
        direction = "East";
    else if (angle < 337.5f)
        direction = "Northeast";
    return direction;

}

 */