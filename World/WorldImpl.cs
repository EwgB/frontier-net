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


        public WorldImpl(ITerraform terraform) {
            this.terraform = terraform;
        }


        public ITree GetTree(uint id) {
            throw new NotImplementedException();
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
            if (region.ShapeFlags.HasFlag(RegionFlag.RiverAny)) {
                Vector2 cen;
                Vector2 newOff;

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

                if (region.ShapeFlags.HasFlag(RegionFlag.RiverSE) && !region.ShapeFlags.HasFlag(RegionFlag.RiverNW)) {
                    newOff.X = 1 - offset.X;
                    newOff.Y = 1 - offset.Y;
                    newOff.X = newOff.Y = newOff.Length;
                    offset = newOff;
                }

                if (region.ShapeFlags.HasFlag(RegionFlag.RiverNE) && !region.ShapeFlags.HasFlag(RegionFlag.RiverSW)) {
                    newOff.X = 1 - offset.X;
                    newOff.Y = offset.Y;
                    newOff.X = newOff.Y = newOff.Length;
                    offset = newOff;
                }

                if (region.ShapeFlags.HasFlag(RegionFlag.RiverSW) && !region.ShapeFlags.HasFlag(RegionFlag.RiverNE)) {
                    newOff.X = offset.X;
                    newOff.Y = 1 - offset.Y;
                    newOff.X = newOff.Y = newOff.Length;
                    offset = newOff;
                }

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
            }

            return elevationValue;
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
                        this.canopy = m + t * TREE_TYPES;
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
    }

    #endregion
}

/*
//The dither map scatters surface data so that grass colorings end up in adjacent regions.
#define DITHER_SIZE       (REGION_SIZE / 2)
//How much space in a region is spent interpolating between itself and its neighbors.
#define BLEND_DISTANCE    (REGION_SIZE / 4)

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

private GLcoord dithermap[DITHER_SIZE, DITHER_SIZE];




float WorldWaterLevel(int world_x, int world_y)
{

    GLcoord origin;
    Vector2 offset;
    IRegion rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.

    world_x += REGION_HALF;
    world_y += REGION_HALF;
    origin.X = world_x / REGION_SIZE;
    origin.Y = world_y / REGION_SIZE;
    origin.X = clamp(origin.X, 0, WorldUtils.WORLD_GRID - 1);
    origin.Y = clamp(origin.Y, 0, WorldUtils.WORLD_GRID - 1);
    offset.X = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
    offset.Y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
    rul = WorldIRegionGet(origin.X, origin.Y);
    rur = WorldIRegionGet(origin.X + 1, origin.Y);
    rbl = WorldIRegionGet(origin.X, origin.Y + 1);
    rbr = WorldIRegionGet(origin.X + 1, origin.Y + 1);
    return MathInterpolateQuad(rul.GeoWater, rur.GeoWater, rbl.GeoWater, rbr.GeoWater, offset, ((origin.X + origin.Y) % 2) == 0);

}

float WorldBiasLevel(int world_x, int world_y)
{

    GLcoord origin;
    Vector2 offset;
    IRegion rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.

    world_x += REGION_HALF;
    world_y += REGION_HALF;
    origin.X = world_x / REGION_SIZE;
    origin.Y = world_y / REGION_SIZE;
    origin.X = clamp(origin.X, 0, WorldUtils.WORLD_GRID - 1);
    origin.Y = clamp(origin.Y, 0, WorldUtils.WORLD_GRID - 1);
    offset.X = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
    offset.Y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
    rul = WorldIRegionGet(origin.X, origin.Y);
    rur = WorldIRegionGet(origin.X + 1, origin.Y);
    rbl = WorldIRegionGet(origin.X, origin.Y + 1);
    rbr = WorldIRegionGet(origin.X + 1, origin.Y + 1);
    return MathInterpolateQuad(rul.geo_bias, rur.geo_bias, rbl.geo_bias, rbr.geo_bias, offset, ((origin.X + origin.Y) % 2) == 0);

}

Cell WorldCell(int world_x, int world_y)
{

    float detail;
    float bias;
    IRegion rul, rur, rbl, rbr;//Four corners: upper left, upper right, etc.
    float eul, eur, ebl, ebr;
    float water;
    Vector2 offset;
    GLcoord origin;
    GLcoord ul, br; //Upper left and bottom-right corners
    Vector2 blend;
    bool left;
    Cell result;

    detail = Entropy(world_x, world_y);
    bias = WorldBiasLevel(world_x, world_y);
    water = WorldWaterLevel(world_x, world_y);
    origin.X = world_x / REGION_SIZE;
    origin.Y = world_y / REGION_SIZE;
    origin.X = clamp(origin.X, 0, WorldUtils.WORLD_GRID - 1);
    origin.Y = clamp(origin.Y, 0, WorldUtils.WORLD_GRID - 1);
    //Get our offset from the region origin as a pair of scalars.
    blend.X = (float)(world_x % BLEND_DISTANCE) / BLEND_DISTANCE;
    blend.Y = (float)(world_y % BLEND_DISTANCE) / BLEND_DISTANCE;
    left = ((origin.X + origin.Y) % 2) == 0;
    offset.X = (float)((world_x) % REGION_SIZE) / REGION_SIZE;
    offset.Y = (float)((world_y) % REGION_SIZE) / REGION_SIZE;
    result.detail = detail;
    result.water_level = water;

    ul.X = origin.X;
    ul.Y = origin.Y;
    br.X = (world_x + BLEND_DISTANCE) / REGION_SIZE;
    br.Y = (world_y + BLEND_DISTANCE) / REGION_SIZE;

    if (ul == br)
    {
        rul = WorldIRegionGet(ul.X, ul.Y);
        result.elevation = DoHeight(rul, offset, water, detail, bias);
        result.elevation = DoHeightNoBlend(result.elevation, rul, offset, water);
        return result;
    }
    rul = WorldIRegionGet(ul.X, ul.Y);
    rur = WorldIRegionGet(br.X, ul.Y);
    rbl = WorldIRegionGet(ul.X, br.Y);
    rbr = WorldIRegionGet(br.X, br.Y);

    eul = DoHeight(rul, offset, water, detail, bias);
    eur = DoHeight(rur, offset, water, detail, bias);
    ebl = DoHeight(rbl, offset, water, detail, bias);
    ebr = DoHeight(rbr, offset, water, detail, bias);
    result.elevation = MathInterpolateQuad(eul, eur, ebl, ebr, blend, left);
    result.elevation = DoHeightNoBlend(result.elevation, rul, offset, water);
    return result;

}

uint WorldTreeType(float moisture, float temperature)
{

    int m, t;

    m = (int)(moisture * TREE_TYPES);
    t = (int)(temperature * TREE_TYPES);
    m = clamp(m, 0, TREE_TYPES - 1);
    t = clamp(t, 0, TREE_TYPES - 1);
    return m + t * TREE_TYPES;

}

CTree* WorldTree(uint id)
{

    uint m, t;

    m = id % TREE_TYPES;
    t = (id - m) / TREE_TYPES;
    return &this.trees[m, t];

}

char* WorldLocationName(int world_x, int world_y)
{

    static char result[20];
    char lat[20];
    char lng[20];

    world_x /= REGION_SIZE;
    world_y /= REGION_SIZE;
    world_x -= WorldUtils.WORLD_GRID_CENTER;
    world_y -= WorldUtils.WORLD_GRID_CENTER;
    if (!world_x && !world_y)
        return "Equatorial meridian";
    if (world_x == 0)
        strcpy(lng, "meridian");
    else if (world_x < 0)
        sprintf(lng, "%d west", Math.Abs(world_x));
    else
        sprintf(lng, "%d east", world_x);
    if (world_y == 0)
        strcpy(lat, "Equator");
    else if (world_y < 0)
        sprintf(lat, "%d north", Math.Abs(world_y));
    else
        sprintf(lat, "%d south", world_y);
    sprintf(result, "%s, %s", lat, lng);
    return result;

}

void WorldInit()
{

    int x, y;

    //Fill in the dither table - a table of random offsets
    for (y = 0; y < DITHER_SIZE; y++)
    {
        for (x = 0; x < DITHER_SIZE; x++)
        {
            dithermap[x, y].X = this.random.Next() % DITHER_SIZE + this.random.Next() % DITHER_SIZE;
            dithermap[x, y].Y = this.random.Next() % DITHER_SIZE + this.random.Next() % DITHER_SIZE;
        }
    }

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

IRegion WorldIRegionGet(int index_x, int index_y)
{

    return this.map[index_x, index_y];

}

void WorldIRegionSet(int index_x, int index_y, IRegion val)
{

    this.map[index_x, index_y] = val;

}

IRegion WorldIRegionFromPosition(int world_x, int world_y)
{

    world_x = Math.Max(world_x, 0);
    world_y = Math.Max(world_y, 0);
    world_x += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].X;
    world_y += dithermap[world_x % DITHER_SIZE, world_y % DITHER_SIZE].Y;
    world_x /= REGION_SIZE;
    world_y /= REGION_SIZE;
    if (world_x >= WorldUtils.WORLD_GRID || world_y >= WorldUtils.WORLD_GRID)
        return this.map[0, 0];
    return this.map[world_x, world_y];

}

IRegion WorldIRegionFromPosition(float world_x, float world_y)
{

    return WorldIRegionFromPosition((int)world_x, (int)world_y);

}

GLrgba WorldColorGet(int world_x, int world_y, SurfaceColor c)
{

    GLcoord origin;
    int x, y;
    Vector2 offset;
    GLrgba c0, c1, c2, c3, result;
    IRegion r0, r1, r2, r3;

    x = Math.Max(world_x % DITHER_SIZE, 0);
    y = Math.Max(world_y % DITHER_SIZE, 0);
    world_x += dithermap[x, y].X;
    world_y += dithermap[x, y].Y;
    offset.X = (float)(world_x % REGION_SIZE) / REGION_SIZE;
    offset.Y = (float)(world_y % REGION_SIZE) / REGION_SIZE;
    origin.X = world_x / REGION_SIZE;
    origin.Y = world_y / REGION_SIZE;
    r0 = WorldIRegionGet(origin.X, origin.Y);
    r1 = WorldIRegionGet(origin.X + 1, origin.Y);
    r2 = WorldIRegionGet(origin.X, origin.Y + 1);
    r3 = WorldIRegionGet(origin.X + 1, origin.Y + 1);
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
    result.red = MathInterpolateQuad(c0.red, c1.red, c2.red, c3.red, offset);
    result.green = MathInterpolateQuad(c0.green, c1.green, c2.green, c3.green, offset);
    result.blue = MathInterpolateQuad(c0.blue, c1.blue, c2.blue, c3.blue, offset);
    return result;

}

uint WorldCanopyTree()
{

    return canopy;

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

char* WorldDirectionFromAnGL.e(float anGL.e)
{

    char* direction;

    direction = "North";
    if (anGL.e < 22.5f)
        direction = "North";
    else if (anGL.e < 67.5f)
        direction = "Northwest";
    else if (anGL.e < 112.5f)
        direction = "West";
    else if (anGL.e < 157.5f)
        direction = "Southwest";
    else if (anGL.e < 202.5f)
        direction = "South";
    else if (anGL.e < 247.5f)
        direction = "Southeast";
    else if (anGL.e < 292.5f)
        direction = "East";
    else if (anGL.e < 337.5f)
        direction = "Northeast";
    return direction;

}

 */