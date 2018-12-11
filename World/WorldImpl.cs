﻿namespace FrontierSharp.World {
    using System;
    using System.IO;

    using MersenneTwister;
    using NLog;
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Game;
    using Common.Grid;
    using Common.Property;
    using Common.Region;
    using Common.Terraform;
    using Common.Tree;
    using Common.Util;
    using Common.World;

    internal class WorldImpl : IWorld {

        private struct WorldHeader {
            public int Version;
            public int Seed;
            public int WorldGrid;
            public int NoiseBuffer;
            public int TreesTypes;
            public int MapBytes;
        }

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
        private const int TREE_TYPES = 6;

        //The dither map scatters surface data so that grass colorings end up in adjacent regions.
        private const int DITHER_SIZE = (WorldUtils.REGION_SIZE / 2);

        //How much space in a region is spent interpolating between itself and its neighbors.
        private const int BLEND_DISTANCE = (WorldUtils.REGION_SIZE / 4);

        #endregion

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #region Modules

        private IEntropy Entropy { get; }
        private IGame Game { get; }
        private ITerraform Terraform { get; }

        #endregion


        #region Public properties

        public IProperties Properties {
            get { throw new NotImplementedException(); }
        }

        public int MapId { get; private set; }
        public int Seed { get; private set; }
        public bool WindFromWest { get; private set; }
        public int TreeCanopy { get; private set; }
        public bool NorthernHemisphere { get; private set; }

        private float[] noiseF = new float[NOISE_BUFFER];
        public float GetNoiseF(int index) => noiseF[Math.Abs(index % NOISE_BUFFER)];

        private int[] noiseI = new int[NOISE_BUFFER];
        public int GetNoiseI(int index) => noiseI[Math.Abs(index % NOISE_BUFFER)];


        private readonly Region[,] regions = new Region[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];
        public Region GetRegion(int x, int y) => regions[x, y];
        public void SetRegion(int x, int y, Region region) => regions[x, y] = region;

        public Region GetRegionFromPosition(int worldX, int worldY) {
            worldX = Math.Max(worldX, 0);
            worldY = Math.Max(worldY, 0);
            worldX += dithermap[worldX % DITHER_SIZE, worldY % DITHER_SIZE].X;
            worldY += dithermap[worldX % DITHER_SIZE, worldY % DITHER_SIZE].Y;
            worldX /= WorldUtils.REGION_SIZE;
            worldY /= WorldUtils.REGION_SIZE;
            if (worldX >= WorldUtils.WORLD_GRID || worldY >= WorldUtils.WORLD_GRID)
                return regions[0, 0];
            return regions[worldX, worldY];
        }


        #endregion


        #region Private members

        private Random Random { get; set; }

        private int RiverCount { get; set; }
        private int LakeCount { get; set; }

        private readonly ITree[,] trees = new ITree[TREE_TYPES, TREE_TYPES];

        private readonly Coord[,] dithermap = new Coord[DITHER_SIZE, DITHER_SIZE];

        #endregion


        public WorldImpl(IGame game, IEntropy entropy, ITerraform terraform) {
            Game = game;
            Entropy = entropy;
            Terraform = terraform;
        }

        public void Init() {
            //Fill in the dither table - a table of random offsets
            for (var y = 0; y < DITHER_SIZE; y++) {
                for (var x = 0; x < DITHER_SIZE; x++) {
                    dithermap[x, y] = new Coord(
                        Random.Next() % DITHER_SIZE + Random.Next() % DITHER_SIZE,
                        Random.Next() % DITHER_SIZE + Random.Next() % DITHER_SIZE);
                }
            }
        }


        public Cell GetCell(int worldX, int worldY) {
            var detail = Entropy.GetEntropy(worldX, worldY);
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

            Region upperLeftRegion;
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
            result.Elevation = MathUtils.InterpolateQuad(upperLeftElevation, upperRightElevation, bottomLeftElevation,
                bottomRightElevation, blend, left);
            result.Elevation = DoHeightNoBlend(result.Elevation, upperLeftRegion, offset, waterLevel);
            return result;
        }

        public Color3 GetColor(int worldX, int worldY, SurfaceColor c) {
            Vector2 offset;
            Color3 c0, c1, c2, c3, result;

            var x = Math.Max(worldX % DITHER_SIZE, 0);
            var y = Math.Max(worldY % DITHER_SIZE, 0);
            worldX += dithermap[x, y].X;
            worldY += dithermap[x, y].Y;
            offset.X = (float) (worldX % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            offset.Y = (float) (worldY % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE;
            var origin = new Coord(
                worldX / WorldUtils.REGION_SIZE,
                worldY / WorldUtils.REGION_SIZE);
            var r0 = GetRegion(origin.X, origin.Y);
            var r1 = GetRegion(origin.X + 1, origin.Y);
            var r2 = GetRegion(origin.X, origin.Y + 1);
            var r3 = GetRegion(origin.X + 1, origin.Y + 1);

            switch (c) {
            case SurfaceColor.Dirt:
                c0 = r0.ColorDirt;
                c1 = r1.ColorDirt;
                c2 = r2.ColorDirt;
                c3 = r3.ColorDirt;
                break;
            case SurfaceColor.Rock:
                c0 = r0.ColorRock;
                c1 = r1.ColorRock;
                c2 = r2.ColorRock;
                c3 = r3.ColorRock;
                break;
            case SurfaceColor.Sand:
                return new Color3(0.98f, 0.82f, 0.42f);
            case SurfaceColor.Grass:
            default:
                c0 = r0.ColorGrass;
                c1 = r1.ColorGrass;
                c2 = r2.ColorGrass;
                c3 = r3.ColorGrass;
                break;
            }

            result = new Color3(
                MathUtils.InterpolateQuad(c0.R, c1.R, c2.R, c3.R, offset),
                MathUtils.InterpolateQuad(c0.G, c1.G, c2.G, c3.G, offset),
                MathUtils.InterpolateQuad(c0.B, c1.B, c2.B, c3.B, offset));
            return result;
        }

        public ITree GetTree(int id) {
            var m = id % TREE_TYPES;
            var t = (id - m) / TREE_TYPES;
            return trees[m, t];
        }

        public void Generate(int seed) {
            Random = Randoms.Create(seed);
            Seed = seed;

            for (var x = 0; x < NOISE_BUFFER; x++) {
                noiseI[x] = Random.Next();
                noiseF[x] = (float) Random.NextDouble();
            }

            BuildTrees();
            WindFromWest = (Random.Next() % 2 == 0);
            NorthernHemisphere = (Random.Next() % 2 == 0);
            RiverCount = 4 + Random.Next() % 4;
            LakeCount = 1 + Random.Next() % 4;
            Terraform.Prepare();
            Terraform.Oceans();
            Terraform.Coast();
            Terraform.Climate();
            Terraform.Rivers(RiverCount);
            Terraform.Lakes(LakeCount);
            Terraform.Climate(); //Do climate a second time now that rivers are in
            Terraform.Zones();
            Terraform.Climate(); //Now again, since we have added climate-modifying features (Mountains, etc.)
            Terraform.Fill();
            Terraform.Average();
            Terraform.Flora();
            Terraform.Colors();
            BuildMapTexture();
        }

        public void Load(int seed) {
            var filename = Path.Combine(Game.GameDirectory, "world.sav");

            try {
                using (var reader = new BinaryReader(File.OpenRead(filename))) {
                    // Skip the header
                    var _ = ReadWorldHeader(reader);

                    ReadWorldData(reader);
                }
            } catch(FileNotFoundException) {
                Log.Debug("[Load] Could not open file {0}", filename);
                Generate(seed);
                return;
            }


            Log.Debug("[Load] File '{0}' loaded.", filename);

            BuildTrees();
            BuildMapTexture();
        }

        private static WorldHeader ReadWorldHeader(BinaryReader reader) {
            WorldHeader header;
            header.Version = reader.ReadInt32();
            header.Seed = reader.ReadInt32();
            header.WorldGrid = reader.ReadInt32();
            header.NoiseBuffer = reader.ReadInt32();
            header.TreesTypes = reader.ReadInt32();
            header.MapBytes = reader.ReadInt32();
            return header;
        }

        private void ReadWorldData(BinaryReader reader) {
            Seed = reader.ReadInt32();
            WindFromWest = reader.ReadBoolean();
            NorthernHemisphere = reader.ReadBoolean();
            RiverCount = reader.ReadInt32();
            LakeCount = reader.ReadInt32();

            // Read float noise. Convert bytes to float array.
            var bytes = reader.ReadBytes(NOISE_BUFFER * sizeof(float));
            noiseF = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(noiseF, 0, bytes, 0, bytes.Length);

            // Read int noise. Convert bytes to int array.
            bytes = reader.ReadBytes(NOISE_BUFFER * sizeof(int));
            noiseI = new int[bytes.Length / sizeof(int)];
            Buffer.BlockCopy(noiseI, 0, bytes, 0, bytes.Length);

            // Read region data.
            for (var x = 0; x < WorldUtils.WORLD_GRID ; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID ; y++) {
                    var region = ReadRegion(reader);
                    regions[x, y] = region;
                }
            }
        }

        private static Region ReadRegion(BinaryReader reader) {
            var region = new Region {
                Title = reader.ReadString(),
                TreeType = reader.ReadInt32(),
                ShapeFlags = (RegionFlags) reader.ReadInt32(),
                Climate = (ClimateType) reader.ReadInt32(),
                GridPosition = new Coord(reader.ReadInt32(), reader.ReadInt32()),
                MountainHeight = reader.ReadInt32(),
                RiverId = reader.ReadInt32(),
                RiverSegment = reader.ReadInt32(),
                TreeThreshold = reader.ReadSingle(),
                RiverWidth = reader.ReadSingle(),
                GeoScale = reader.ReadSingle(),
                GeoWater = reader.ReadSingle(),
                GeoDetail = reader.ReadSingle(),
                GeoBias = reader.ReadSingle(),
                Temperature = reader.ReadSingle(),
                Moisture = reader.ReadSingle(),
                CliffThreshold = reader.ReadSingle(),
                ColorMap = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                ColorRock = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                ColorDirt = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                ColorGrass = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                ColorAtmosphere = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                HasFlowers = reader.ReadBoolean()
            };

            for (var i = 0; i < Region.FLOWERS; i++) {
                var flower = new Flower {
                    Color = new Color3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                    Shape = reader.ReadInt32()
                };
                region.Flowers[i] = flower;
            }

            return region;
        }

        public void Save() {
            //WorldHeader header;

            return;
            // TODO: Why was this all non-functional?
            var filename = $"{Game.GameDirectory}world.sav";
            try { } catch (FileNotFoundException e) {
                Log.Debug("[Save] Could not open file {0}", filename);
            }
            //if (!(f = fopen(filename, "wb")))
            //{
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
            Log.Debug("[Save] File '{0}' saved.", filename);
        }

        public void Update() { /* Do nothing */ }


        #region Functions that are used when generating elevation data

        /// <summary>
        /// This modifies the passed elevation value AFTER region cross-fading is complete,
        /// for things that should not be mimicked by neighbors. (Like rivers.)
        /// </summary>
        private static float DoHeightNoBlend(float elevationValue, Region region, Vector2 offset, float waterLevel) {
            if (!region.ShapeFlags.HasFlag(RegionFlags.RiverAny))
                return elevationValue;

            //if this river is strictly north / south
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverNS) && !region.ShapeFlags.HasFlag(RegionFlags.RiverW)) {
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
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverW) && !region.ShapeFlags.HasFlag(RegionFlags.RiverNS)) {
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
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverNW) && !region.ShapeFlags.HasFlag(RegionFlags.RiverSE))
                offset.X = offset.Y = offset.Length;

            offset = ComputeNewOffset(region, offset);

            Vector2 cen;
            cen.X = Math.Abs((offset.X - 0.5f) * 2);
            cen.Y = Math.Abs((offset.Y - 0.5f) * 2);
            var strength = cen.Length;
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverN) && offset.Y < 0.5f)
                strength = Math.Min(strength, cen.X);
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverS) && offset.Y >= 0.5f)
                strength = Math.Min(strength, cen.X);
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverW) && offset.X < 0.5f)
                strength = Math.Min(strength, cen.Y);
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverE) && offset.X >= 0.5f)
                strength = Math.Min(strength, cen.Y);
            if (strength < (region.RiverWidth / 2)) {
                strength *= 1 / (region.RiverWidth / 2);
                var delta = (elevationValue - waterLevel) + 4 * region.RiverWidth;
                elevationValue -= (delta) * (1 - strength);
            }

            return elevationValue;
        }

        private static Vector2 ComputeNewOffset(Region region, Vector2 offset) {
            Vector2 newOffset;
            if (region.ShapeFlags.HasFlag(RegionFlags.RiverSE) && !region.ShapeFlags.HasFlag(RegionFlags.RiverNW)) {
                newOffset.X = 1 - offset.X;
                newOffset.Y = 1 - offset.Y;
                newOffset.X = newOffset.Y = newOffset.Length;
                offset = newOffset;
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.RiverNE) && !region.ShapeFlags.HasFlag(RegionFlags.RiverSW)) {
                newOffset.X = 1 - offset.X;
                newOffset.Y = offset.Y;
                newOffset.X = newOffset.Y = newOffset.Length;
                offset = newOffset;
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.RiverSW) && !region.ShapeFlags.HasFlag(RegionFlags.RiverNE)) {
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
        private static float DoHeight(Region region, Vector2 offset, float waterLevel, float detail, float bias) {
            //Modify the detail values before they are applied

            if (region.ShapeFlags.HasFlag(RegionFlags.Crater)) {
                detail = Math.Max(detail, 0.5f);
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.Tiered)) {
                if (detail < 0.2f)
                    detail += 0.2f;
                else if (detail < 0.5f)
                    detail -= 0.2f;
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.Crack)) {
                if (detail > 0.2f && detail < 0.3f)
                    detail = 0;
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.Sinkhole)) {
                var x = Math.Abs(offset.X - 0.5f);
                var y = Math.Abs(offset.Y - 0.5f);
                if (detail > Math.Max(x, y))
                    detail /= 4;
            }

            //Soften up the banks of a river 
            if ((region.ShapeFlags & RegionFlags.RiverAny) == RegionFlags.RiverAny) {
                var centerX = Math.Abs((offset.X - 0.5f) * 2);
                var centerY = Math.Abs((offset.Y - 0.5f) * 2);
                var strength = Math.Max(Math.Min(centerX, centerY), 0.1f);
                detail *= strength;
            }


            //Apply the values!
            var val = waterLevel + detail * region.GeoDetail + bias;
            if (region.Climate == ClimateType.Swamp) {
                val -= region.GeoDetail / 2;
                val = Math.Max(val, region.GeoWater - 0.5f);
            }

            //Modify the final value.
            if (region.ShapeFlags.HasFlag(RegionFlags.Mesas)) {
                var x = Math.Abs(offset.X - 0.5f) / 5;
                var y = Math.Abs(offset.Y - 0.5f) / 5;
                if ((detail + 0.01f) < (x + y)) {
                    val += 5;
                }
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.CanyonNS)) {
                var x = Math.Abs(offset.X - 0.5f) * 2;
                if (x + detail < 0.5f)
                    val -= Math.Min(region.GeoDetail, 10);
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.Beach) && val < region.CliffThreshold && val > 0) {
                val /= region.CliffThreshold;
                val *= val;
                val *= region.CliffThreshold;
                val += 0.2f;
            }

            if (region.ShapeFlags.HasFlag(RegionFlags.BeachCliff) && val < region.CliffThreshold && val > -0.1f) {
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
            for (var m = 0; m < TREE_TYPES; m++) {
                int t;
                for (t = 0; t < TREE_TYPES; t++) {
                    bool isCanopy;
                    if ((m == TREE_TYPES / 2) && (t == TREE_TYPES / 2)) {
                        isCanopy = true;
                        TreeCanopy = m + t * TREE_TYPES;
                    } else
                        isCanopy = false;

                    trees[m, t].Create(isCanopy, (float) m / TREE_TYPES, (float) t / TREE_TYPES, rotator++);
                }
            }
        }

        private void BuildMapTexture() {
            if (MapId == 0) {
                GL.GenTextures(1, out int mapId);
                MapId = mapId;
            }

            GL.BindTexture(TextureTarget.Texture2D, MapId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (float) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (float) TextureMagFilter.Nearest);

            var buffer = new byte[WorldUtils.WORLD_GRID * WorldUtils.WORLD_GRID * 3];

            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    //Flip it vertically, because the OpenGL texture coord system is retarded.
                    var yy = (WorldUtils.WORLD_GRID - 1) - y;
                    var region = regions[x, yy];
                    var bufferIndex = (x + y * WorldUtils.WORLD_GRID) * 3;
                    buffer[bufferIndex] = (byte) (region.ColorMap.R * 255);
                    buffer[bufferIndex + 1] = (byte) (region.ColorMap.G * 255);
                    buffer[bufferIndex + 2] = (byte) (region.ColorMap.B * 255);
                }
            }

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, WorldUtils.WORLD_GRID,
                WorldUtils.WORLD_GRID, 0, PixelFormat.Rgb,
                PixelType.UnsignedByte, buffer);
        }

        private float GetBiasLevel(int worldX, int worldY) {
            worldX += WorldUtils.REGION_HALF;
            worldY += WorldUtils.REGION_HALF;
            var origin = new Coord(
                MathHelper.Clamp(worldX / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1),
                MathHelper.Clamp(worldY / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1));
            var offset = new Vector2(
                (float) ((worldX) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE,
                (float) ((worldY) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE);
            var upperLeft = GetRegion(origin.X, origin.Y);
            var upperRight = GetRegion(origin.X + 1, origin.Y);
            var bottomLeft = GetRegion(origin.X, origin.Y + 1);
            var bottomRight = GetRegion(origin.X + 1, origin.Y + 1);
            return MathUtils.InterpolateQuad(upperLeft.GeoBias, upperRight.GeoBias, bottomLeft.GeoBias,
                bottomRight.GeoBias, offset,
                ((origin.X + origin.Y) % 2) == 0);
        }

        public float GetWaterLevel(int worldX, int worldY) {
            worldX += WorldUtils.REGION_HALF;
            worldY += WorldUtils.REGION_HALF;
            var origin = new Coord(
                MathHelper.Clamp(worldX / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1),
                MathHelper.Clamp(worldY / WorldUtils.REGION_SIZE, 0, WorldUtils.WORLD_GRID - 1));
            var offset = new Vector2(
                (float) ((worldX) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE,
                (float) ((worldY) % WorldUtils.REGION_SIZE) / WorldUtils.REGION_SIZE);
            var upperLeft = GetRegion(origin.X, origin.Y);
            var upperRight = GetRegion(origin.X + 1, origin.Y);
            var bottomLeft = GetRegion(origin.X, origin.Y + 1);
            var bottomRight = GetRegion(origin.X + 1, origin.Y + 1);
            return MathUtils.InterpolateQuad(upperLeft.GeoWater, upperRight.GeoWater, bottomLeft.GeoWater,
                bottomRight.GeoWater, offset,
                ((origin.X + origin.Y) % 2) == 0);
        }

        public float GetWaterLevel(Vector2 coord) => GetWaterLevel((int) coord.X, (int) coord.Y);

        public int GetTreeType(float moisture, float temperature) {
            var m = (int) (moisture * TREE_TYPES);
            var t = (int) (temperature * TREE_TYPES);
            m = MathHelper.Clamp(m, 0, TREE_TYPES - 1);
            t = MathHelper.Clamp(t, 0, TREE_TYPES - 1);
            return m + t * TREE_TYPES;

        }

    }

    #endregion
}

/*

#define FILE_VERSION      1


string WorldLocationName(int worldX, int worldY)
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

void WorldRegionSet(int index_x, int index_y, IRegion val)
{
    this.map[index_x, index_y] = val;
}

World* WorldPtr()
{
    return &planet;
}

void WorldTexturePurge()
{
    for (var m = 0; m < TREE_TYPES; m++)
    {
        for (var t = 0; t < TREE_TYPES; t++)
        {
            this.trees[m, t].TexturePurge();
        }
    }
    BuildMapTexture();
}

string WorldDirectionFromAngle(float angle)
{
    string direction = "North";
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