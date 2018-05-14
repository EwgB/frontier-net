namespace FrontierSharp.Terraform {
    using System;
    using System.Collections.Generic;

    using MersenneTwister;
    using OpenTK;

    using Common;
    using Common.Grid;
    using Common.Region;
    using Common.Terraform;
    using Common.Util;
    using Common.World;

    internal class TerraformImpl : ITerraform {

        #region Constants

        private const float MIN_TEMP = 0;
        private const float MAX_TEMP = 1;

        /// <summary> This affects the mapping of the coastline.  Higher = busier, more repetitive coast. </summary>
        private const float FREQUENCY = 1;

        /// <summary> The number of regions around the edge which should be ocean. </summary>
        private const float OCEAN_BUFFER = WorldUtils.WORLD_GRID / 10f;

        private static readonly Coord North = new Coord(0, -1);
        private static readonly Coord South = new Coord(0, 1);
        private static readonly Coord East = new Coord(1, 0);
        private static readonly Coord West = new Coord(-1, 0);
        private static readonly Coord[] Directions = {North, South, East, West};

        private const string DIR_NAME_NORTH = "Northern";
        private const string DIR_NAME_SOUTH = "Southern";
        private const string DIR_NAME_EAST = "Eastern";
        private const string DIR_NAME_WEST = "Western";

        private const float TEMP_COLD = 0.45f;
        private const float TEMP_TEMPERATE = 0.6f;
        private const float TEMP_HOT = 0.9f;

        private static readonly Color3[] FlowerPalette = {
            Color3.White, Color3.White, Color3.White,
            new Color3(1, 0.3f, 0.3f), new Color3(1, 0.3f, 0.3f), //red
            Color3.Yellow, Color3.Yellow,
            new Color3(0.7f, 0.3f, 1), // Violet
            new Color3(1, 0.5f, 1), // Pink #1
            new Color3(1, 0.5f, 0.8f), // Pink #2
            new Color3(1, 0, 0.5f), //Maroon
        };

        #endregion


        #region Modules

        private IEntropy Entropy { get; }
        private Random Random { get; }
        private IRegionFactory RegionFactory { get; }
        private IWorld World { get; }

        #endregion


        public TerraformImpl(IEntropy entropy, IRegionFactory regionFactory, IWorld world) {
            this.Entropy = entropy;
            this.Random = Randoms.Create();
            this.RegionFactory = regionFactory;
            this.World = world;
        }

        public void Init() { /* Do nothing */ }
        public void Update() { /* Do nothing */ }

        public void Average() {
            var temp = new float[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];
            var moist = new float[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];
            var elev = new float[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];
            var sm = new float[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];
            var bias = new float[WorldUtils.WORLD_GRID, WorldUtils.WORLD_GRID];

            const int radius = 2;

            //Blur some of the attributes
            for (var passes = 0; passes < 2; passes++) {

                for (var x = radius; x < WorldUtils.WORLD_GRID - radius; x++) {
                    for (var y = radius; y < WorldUtils.WORLD_GRID - radius; y++) {
                        temp[x, y] = 0;
                        moist[x, y] = 0;
                        elev[x, y] = 0;
                        sm[x, y] = 0;
                        bias[x, y] = 0;
                        var count = 0;
                        for (var xx = -radius; xx <= radius; xx++) {
                            for (var yy = -radius; yy <= radius; yy++) {
                                var region = this.World.GetRegion(x + xx, y + yy);
                                temp[x, y] += region.Temperature;
                                moist[x, y] += region.Moisture;
                                elev[x, y] += region.GeoWater;
                                sm[x, y] += region.GeoDetail;
                                bias[x, y] += region.GeoBias;
                                count++;
                            }
                        }

                        temp[x, y] /= count;
                        moist[x, y] /= count;
                        elev[x, y] /= count;
                        sm[x, y] /= count;
                        bias[x, y] /= count;
                    }
                }

                //Put the blurred values back into our table
                for (var x = radius; x < WorldUtils.WORLD_GRID - radius; x++) {
                    for (var y = radius; y < WorldUtils.WORLD_GRID - radius; y++) {
                        var region = this.World.GetRegion(x, y);
                        //Rivers can get wetter through this process, but not drier.
                        if (region.Climate == ClimateType.River)
                            region.Moisture = Math.Max(region.Moisture, moist[x, y]);
                        else if (region.Climate != ClimateType.Ocean)
                            region.Moisture = moist[x, y]; //No matter how arid it is, the OCEANS STAY WET!
                        if (!region.ShapeFlags.HasFlag(RegionFlags.NoBlend)) {
                            region.GeoDetail = sm[x, y];
                            region.GeoBias = bias[x, y];
                        }

                        this.World.SetRegion(x, y, region);
                    }
                }
            }
        }

        public void Climate() {
            var rainfall = 1f;
            var walk = new Coord();
            bool rolledOver;
            do {
                //Wind (and thus rainfall) come from west.
                var x = (this.World.WindFromWest) ? walk.X : (WorldUtils.WORLD_GRID - 1) - walk.X;
                var y = walk.Y;
                var region = this.World.GetRegion(x, y);

                //============   TEMPERATURE ===================//
                //The north 25% is max cold.  The south 25% is all tropical
                //On a southern hemisphere map, this is reversed.
                float temp;
                if (this.World.NorthernHemisphere)
                    temp = (y - ((float) WorldUtils.WORLD_GRID / 4)) / WorldUtils.WORLD_GRID_CENTER;
                else
                    temp = ((WorldUtils.WORLD_GRID - y) - ((float) WorldUtils.WORLD_GRID / 4)) /
                           WorldUtils.WORLD_GRID_CENTER;
                //Mountains are cooler at the top
                if (region.MountainHeight != 0)
                    temp -= region.MountainHeight * 0.15f;
                //We add a slight bit of heat to the center of the map, to
                //round off climate boundaries.
                var fromCenter = new Vector2(x - WorldUtils.WORLD_GRID_CENTER, x - WorldUtils.WORLD_GRID_CENTER);
                var distance = fromCenter.Length / WorldUtils.WORLD_GRID_CENTER;
                temp += distance * 0.2f;
                temp = MathHelper.Clamp(temp, MIN_TEMP, MAX_TEMP);

                //============  RAINFALL ===================//
                //Oceans are ALWAYS WET.
                if (region.Climate == ClimateType.Ocean)
                    rainfall = 1;
                var rainLoss = 0f;
                //We lose rainfall as we move inland.
                if (region.Climate != ClimateType.Ocean &&
                    region.Climate != ClimateType.Coast &&
                    region.Climate != ClimateType.Lake)
                    rainLoss = 1f / WorldUtils.WORLD_GRID_CENTER;
                //We lose rainfall more slowly as it gets colder.
                if (temp < 0.5f)
                    rainLoss *= temp;
                rainfall -= rainLoss;
                //Mountains block rainfall
                if (region.Climate == ClimateType.Mountain)
                    rainfall -= 0.1f * region.MountainHeight;
                region.Moisture = Math.Max(rainfall, 0);
                //Rivers always give some moisture
                if (region.Climate == ClimateType.River || region.Climate == ClimateType.RiverBank) {
                    region.Moisture = Math.Max(region.Moisture, 0.75f);
                    rainfall += 0.05f;
                    rainfall = Math.Min(rainfall, 1);
                }

                //oceans have a moderating effect on climate
                if (region.Climate == ClimateType.Ocean)
                    temp = (temp + 0.5f) / 2;
                region.Temperature = temp;
                //r.Moisture = Math.Min (1, r.Moisture + this.World.GetNoiseF (walk.X + walk.Y * WorldUtils.WORLD_GRID) * 0.1f);
                //r.Temperature = Math.Min (1, r.Temperature + this.World.GetNoiseF (walk.X + walk.Y * WorldUtils.WORLD_GRID) * 0.1f);
                this.World.SetRegion(x, y, region);
                walk = walk.Walk(WorldUtils.WORLD_GRID, out rolledOver);
            } while (!rolledOver);
        }

        public void Coast() {
            const int cliffGrid = WorldUtils.WORLD_GRID / 8;

            var queue = new List<Coord>();
            //now define the coast 
            for (var pass = 0; pass < 2; pass++) {
                queue.Clear();
                IRegion region;
                for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                    for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                        region = this.World.GetRegion(x, y);
                        //Skip already assigned places
                        if (region.Climate != ClimateType.Invalid)
                            continue;
                        //One the second pass, we add beach adjoining the beach we added on the previous step
                        var isCoast =
                            (pass == 0) && IsClimatePresent(x, y, 1, ClimateType.Ocean) ||
                            (pass != 0) && IsClimatePresent(x, y, 1, ClimateType.Coast);
                        if (isCoast)
                            queue.Add(new Coord(x, y));
                    }
                }

                //Now we're done scanning the map. Run through our list and make the new regions.
                foreach (var current in queue) {
                    region = this.World.GetRegion(current.X, current.Y);
                    var isCliff = (((current.X / cliffGrid) + (current.Y / cliffGrid)) % 2) != 0;
                    region.Title = GetDirectionName(current.X, current.Y) + ((pass == 0) ? " beach" : "coast");

                    //beaches are low and partially submerged
                    region.GeoDetail = 5 + this.Entropy.GetEntropy(current.X, current.Y) * 10;
                    if (pass == 0) {
                        region.GeoBias = -region.GeoDetail * 0.5f;
                        if (isCliff)
                            region.ShapeFlags |= RegionFlags.BeachCliff;
                        else
                            region.ShapeFlags |= RegionFlags.Beach;
                    } else
                        region.GeoBias = 0;

                    region.CliffThreshold = region.GeoDetail * 0.25f;
                    region.Moisture = 1;
                    region.GeoWater = 0;
                    region.ShapeFlags |= RegionFlags.NoBlend;
                    region.Climate = ClimateType.Coast;
                    this.World.SetRegion(current.X, current.Y, region);
                }
            }
        }

        public void Colors() {
            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    var region = this.World.GetRegion(x, y);
                    region.ColorGrass = GenerateColor(SurfaceColor.Grass, region.Moisture, region.Temperature,
                        (int) (region.GridPosition.X + region.GridPosition.Y * WorldUtils.WORLD_GRID));
                    region.ColorDirt = GenerateColor(SurfaceColor.Dirt, region.Moisture, region.Temperature,
                        (int) (region.GridPosition.X + region.GridPosition.Y * WorldUtils.WORLD_GRID));
                    region.ColorRock = GenerateColor(SurfaceColor.Rock, region.Moisture, region.Temperature,
                        (int) (region.GridPosition.X + region.GridPosition.Y * WorldUtils.WORLD_GRID));

                    //"atmosphere" is the overall color of the lighting & fog. 
                    var warmAir = new Color3(0, 0.2f, 1);
                    var coldAir = new Color3(0.7f, 0.9f, 1);

                    //Only set the atmosphere color if it wasn't set elsewhere
                    if (region.ColorAtmosphere.Equals(Color3.Black))
                        region.ColorAtmosphere = ColorUtils.Interpolate(coldAir, warmAir, region.Temperature);

                    //Color the map
                    switch (region.Climate) {
                    case ClimateType.Mountain:
                        var val = 0.2f + region.MountainHeight / 4;
                        region.ColorMap = new Color3(val, val, val).Normalize();
                        break;
                    case ClimateType.Desert:
                        region.ColorMap = new Color3(0.9f, 0.7f, 0.4f);
                        break;
                    case ClimateType.Coast:
                        region.ColorMap = region.ShapeFlags.HasFlag(RegionFlags.BeachCliff)
                            ? new Color3(0.3f, 0.3f, 0.3f)
                            : new Color3(0.9f, 0.7f, 0.4f);
                        break;
                    case ClimateType.Ocean:
                        region.ColorMap = new Color3(0, 1 + region.GeoScale * 2, 1 + region.GeoScale).Clamp();
                        break;
                    case ClimateType.River:
                    case ClimateType.Lake:
                        region.ColorMap = new Color3(0, 0, 0.6f);
                        break;
                    case ClimateType.RiverBank:
                        region.ColorMap = region.ColorDirt;
                        break;
                    case ClimateType.Field:
                        region.ColorMap = region.ColorGrass + new Color3(0.7f, 0.5f, 0.6f).Normalize();
                        break;
                    case ClimateType.Plains:
                        region.ColorMap = region.ColorGrass + new Color3(0.5f, 0.5f, 0.5f).Normalize();
                        break;
                    case ClimateType.Forest:
                        region.ColorMap = region.ColorGrass + new Color3(0, 0.3f, 0);
                        region.ColorMap *= 0.5f;
                        break;
                    case ClimateType.Swamp:
                        region.ColorGrass *= 0.5f;
                        region.ColorMap = region.ColorGrass * 0.5f;
                        break;
                    case ClimateType.Rocky:
                        //region.ColorMap = (region.ColorGrass * 0.8f + region.ColorRock * 0.2f).Normalize();
                        region.ColorMap = region.ColorRock;
                        break;
                    case ClimateType.Canyon:
                        region.ColorMap = region.ColorRock * 0.3f;
                        break;
                    default:
                        region.ColorMap = region.ColorGrass;
                        break;
                    }

                    if (region.GeoScale >= 0)
                        region.ColorMap *= (region.GeoScale * 0.5f + 0.5f);
                    //if (r.GeoScale >= 0)
                    //r.ColorMap = glRgbaUnique (r.TreeType);
                    //r.ColorMap = r.ColorAtmosphere;
                    this.World.SetRegion(x, y, region);
                }
            }
        }

        public void Fill() {
            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    var region = this.World.GetRegion(x, y);

                    //See if this is already ocean
                    if (region.Climate != ClimateType.Invalid)
                        continue;
                    region.Title = "???";
                    region.GeoWater = region.GeoScale * 10;
                    region.GeoDetail = 20;

                    //Have them trend more hilly in dry areas
                    var rand = this.Random.Next() % 8;
                    if (region.Moisture > 0.3f && region.Temperature > 0.5f) {
                        region.HasFlowers = this.Random.Next() % 4 == 0;
                        var shape = this.Random.Next();
                        var color = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
                        for (var i = 0; i < region.Flowers.Length; i++) {
                            region.Flowers[i].Color = color;
                            region.Flowers[i].Shape = shape;
                            if ((this.Random.Next() % 15) == 0) {
                                shape = this.Random.Next();
                                color = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
                            }
                        }
                    }

                    switch (rand) {
                    case 0:
                        region.ShapeFlags |= RegionFlags.Mesas;
                        region.Title = "Mesas";
                        break;
                    case 1:
                        region.Title = "Craters";
                        region.ShapeFlags |= RegionFlags.Crater;
                        break;
                    case 2:
                        region.Title = "TEST";
                        region.ShapeFlags |= RegionFlags.Test;
                        break;
                    case 3:
                        region.Title = "Sinkhole";
                        region.ShapeFlags |= RegionFlags.Sinkhole;
                        break;
                    case 4:
                        region.Title = "Crack";
                        region.ShapeFlags |= RegionFlags.Crack;
                        break;
                    case 5:
                        region.Title = "Tiered";
                        region.ShapeFlags |= RegionFlags.Tiered;
                        break;
                    case 6:
                        region.Title = "Wasteland";
                        break;
                    default:
                        region.Title = "Grasslands";
                        break;
                    }

                    this.World.SetRegion(x, y, region);
                }
            }
        }

        public void Flora() {
            var walk = new Coord();
            bool rolledOver;
            do {
                var region = this.World.GetRegion(walk.X, walk.Y);
                region.TreeType = this.World.GetTreeType(region.Moisture, region.Temperature);
                if (region.Climate == ClimateType.Forest)
                    region.TreeType = this.World.TreeCanopy;
                this.World.SetRegion(walk.X, walk.Y, region);
                walk = walk.Walk(WorldUtils.WORLD_GRID, out rolledOver);
            } while (!rolledOver);
        }

        public void Lakes(int count) {
            var lakes = 0;
            var cycles = 0;
            const int range = WorldUtils.WORLD_GRID_CENTER / 4;
            while (lakes < count && cycles < 100) {
                //Pick a random spot in the middle of the map
                var x = WorldUtils.WORLD_GRID_CENTER + (this.World.GetNoiseI(cycles) % range) - range / 2;
                var y = WorldUtils.WORLD_GRID_CENTER + (this.World.GetNoiseI(cycles * 2) % range) - range / 2;
                //Now push that point away from the middle
                var shove = GetMapSide(x, y) * range;
                if (TryLake(x + shove.X, y + shove.Y, lakes))
                    lakes++;
                cycles++;
            }
        }

        public void Oceans() {
            //define the oceans at the edge of the World
            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    var region = this.World.GetRegion(x, y);
                    var isOcean = (region.GeoScale <= 0);
                    if (x == 0 || y == 0 || x == WorldUtils.WORLD_GRID - 1 || y == WorldUtils.WORLD_GRID - 1)
                        isOcean = true;
                    if (isOcean) {
                        region.GeoBias = -10;
                        region.GeoDetail = 0.3f;
                        region.Moisture = 1;
                        region.GeoWater = 0;
                        region.ShapeFlags = RegionFlags.NoBlend;
                        region.ColorAtmosphere = new Color3(0.7f, 0.7f, 1);
                        region.Climate = ClimateType.Ocean;
                        region.Title = $"{GetDirectionName(x, y)} Ocean";
                        this.World.SetRegion(x, y, region);
                    }
                }
            }
        }

        public void Prepare() {
            //Set some defaults
            var offset = new Coord(this.Random.Next() % 1024, this.Random.Next() % 1024);
            for (var x = 0; x < WorldUtils.WORLD_GRID; x++) {
                for (var y = 0; y < WorldUtils.WORLD_GRID; y++) {
                    var region = this.RegionFactory.GetRegion();
                    region.Title = "NOTHING";
                    region.GeoBias = region.GeoDetail = 0;
                    region.MountainHeight = 0;
                    region.GridPosition = new Vector2(x, y);
                    region.TreeThreshold = 0.15f;
                    var fromCenter = new Coord(
                        Math.Abs(x - WorldUtils.WORLD_GRID_CENTER),
                        Math.Abs(y - WorldUtils.WORLD_GRID_CENTER));

                    //Geo scale is a number from -1 to 1. -1 is lowest ocean. 0 is sea level. 
                    //+1 is highest elevation on the island. This is used to guide other derived numbers.
                    region.GeoScale = new Vector2(fromCenter.X, fromCenter.Y).Length;
                    region.GeoScale /= (WorldUtils.WORLD_GRID_CENTER - OCEAN_BUFFER);
                    //Create a steep drop around the edge of the World
                    if (region.GeoScale > 1)
                        region.GeoScale = 1 + (region.GeoScale - 1) * 4;
                    region.GeoScale = 1 - region.GeoScale;
                    region.GeoScale += (this.Entropy.GetEntropy((x + offset.X), (y + offset.Y)) - 0.5f);
                    region.GeoScale += (this.Entropy.GetEntropy((x + offset.X) * FREQUENCY, (y + offset.Y) * FREQUENCY) - 0.2f);
                    region.GeoScale = MathHelper.Clamp(region.GeoScale, -1, 1);
                    if (region.GeoScale > 0)
                        region.GeoWater = 1 + region.GeoScale * 16;
                    region.ColorAtmosphere = new Color3(0, 0, 0);
                    region.GeoBias = 0;
                    region.GeoDetail = 0;
                    region.ColorMap = Color3.Black;
                    region.Climate = ClimateType.Invalid;
                    this.World.SetRegion(x, y, region);
                }
            }
        }

        public void Rivers(int count) {
            var rivers = 0;
            var cycles = 0;
            const int range = WorldUtils.WORLD_GRID_CENTER / 3;
            while (rivers < count && cycles < 100) {
                var x = WorldUtils.WORLD_GRID_CENTER + (this.Random.Next() % range) - range / 2;
                var y = WorldUtils.WORLD_GRID_CENTER + (this.Random.Next() % range) - range / 2;
                if (TryRiver(x, y, rivers))
                    rivers++;
                cycles++;
            }
        }

        public void Zones() {
            var climates = new List<ClimateType>();
            var walk = new Coord();
            var spinner = 0;

            bool rolledOver;
            do {
                var x = walk.X;
                var y = walk.Y;
                var radius = 2 + this.World.GetNoiseI(10 + walk.X + walk.Y * WorldUtils.WORLD_GRID) % 9;
                if (IsFree(x, y, radius)) {
                    var region = this.World.GetRegion(x, y);
                    climates.Clear();
                    //swamps only appear in wet areas that aren't cold.
                    if (region.Moisture > 0.8f && region.Temperature > 0.5f)
                        climates.Add(ClimateType.Swamp);
                    //mountains only appear in the middle
                    if (Math.Abs(x - WorldUtils.WORLD_GRID_CENTER) < 10 && radius > 1)
                        climates.Add(ClimateType.Mountain);
                    //Deserts are HOT and DRY. Duh.
                    if (region.Temperature > TEMP_HOT && region.Moisture < 0.05f && radius > 1)
                        climates.Add(ClimateType.Desert);
                    //fields should be not too hot or cold.
                    if (region.Temperature > TEMP_TEMPERATE && region.Temperature < TEMP_HOT &&
                        region.Moisture > 0.5f && radius == 1)
                        climates.Add(ClimateType.Field);
                    if (region.Temperature > TEMP_TEMPERATE && region.Temperature < TEMP_HOT &&
                        region.Moisture > 0.25f && radius > 1)
                        climates.Add(ClimateType.Plains);
                    //Rocky wastelands favor cold areas
                    if (region.Temperature < TEMP_TEMPERATE)
                        climates.Add(ClimateType.Rocky);
                    if (radius > 1 && this.World.GetNoiseI(spinner++) % 10 == 0)
                        climates.Add(ClimateType.Canyon);
                    if (region.Temperature > TEMP_TEMPERATE && region.Temperature < TEMP_HOT && region.Moisture > 0.5f)
                        climates.Add(ClimateType.Forest);
                    if (climates.Count == 0) {
                        walk.Walk(WorldUtils.WORLD_GRID, out rolledOver);
                        continue;
                    }

                    var climateType = climates[this.Random.Next() % climates.Count];
                    switch (climateType) {
                    case ClimateType.Rocky:
                        DoRocky(x, y, radius);
                        break;
                    case ClimateType.Mountain:
                        DoMountain(x, y, radius);
                        break;
                    case ClimateType.Canyon:
                        DoCanyon(x, y, radius);
                        break;
                    case ClimateType.Swamp:
                        DoSwamp(x, y, radius);
                        break;
                    case ClimateType.Field:
                        DoField(x, y, radius);
                        break;
                    case ClimateType.Desert:
                        DoDesert(x, y, radius);
                        break;
                    case ClimateType.Plains:
                        DoPlains(x, y, radius);
                        break;
                    case ClimateType.Forest:
                        DoForest(x, y, radius);
                        break;
                    }
                }

                walk.Walk(WorldUtils.WORLD_GRID, out rolledOver);
            } while (!rolledOver);
        }

        public Color3 GenerateColor(SurfaceColor color, float moisture, float temperature, int seed) {
            switch (color) {
            case SurfaceColor.Grass:
                return GenerateGrassColor(moisture, temperature, seed);
            case SurfaceColor.Dirt:
                return GenerateDirtColor(moisture, temperature, seed);
            case SurfaceColor.Rock:
                //Devise a rock color
                var fade = MathUtils.Scalar(temperature, WorldUtils.FREEZING, 1);

                //Warm rock is red
                var warmRock = new Color3(1, 1 - (float) this.Random.NextDouble() * 0.6f,
                    1 - (float) this.Random.NextDouble() * 0.6f);

                //Cold rock is white or blue
                var val = 1 - (float) this.Random.NextDouble() * 0.4f;
                var coldRock = new Color3(1, val, val);
                return ColorUtils.Interpolate(coldRock, warmRock, fade);
            }

            //Shouldn't happen. Returns magenta to flag the problem.
            return Color3.Magenta;
        }

        private Color3 GenerateGrassColor(float moisture, float temperature, int seed) {
            var wetGrass = new Color3(
                this.World.GetNoiseF(seed++) * 0.3f,
                0.4f + this.World.GetNoiseF(seed++) * 0.6f,
                this.World.GetNoiseF(seed++) * 0.3f);

            //Dry grass is mostly reds and oranges
            var dryGrass = new Color3(
                0.7f + this.World.GetNoiseF(seed++) * 0.3f,
                0.5f + this.World.GetNoiseF(seed++) * 0.5f,
                0 + this.World.GetNoiseF(seed++) * 0.3f);

            //Dead grass is pale beige
            var deadGrass = new Color3(0.7f, 0.6f, 0.5f) * (0.7f + this.World.GetNoiseF(seed++) * 0.3f);
            Color3 warmGrass;
            float fade;
            if (moisture < 0.5f) {
                fade = moisture * 2;
                warmGrass = ColorUtils.Interpolate(deadGrass, dryGrass, fade);
            } else {
                fade = (moisture - 0.5f) * 2;
                warmGrass = ColorUtils.Interpolate(dryGrass, wetGrass, fade);
            }

            //cold grass is pale and a little blue
            var coldGrass = new Color3(
                0.5f + this.World.GetNoiseF(seed++) * 0.2f,
                0.8f + this.World.GetNoiseF(seed++) * 0.2f,
                0.7f + this.World.GetNoiseF(seed) * 0.2f);

            return (temperature < TEMP_COLD)
                ? ColorUtils.Interpolate(coldGrass, warmGrass, temperature / TEMP_COLD)
                : warmGrass;
        }

        private Color3 GenerateDirtColor(float moisture, float temperature, int seed) {
            //Devise a random but plausible dirt color
            //Dry dirts are mostly reds, oranges, and browns
            var red = 0.4f + this.World.GetNoiseF(seed++) * 0.6f;
            //var green = Math.Min(0.4f + this.World.GetNoiseF(seed++) * 0.6f, red);
            var green = 0.1f + this.World.GetNoiseF(seed++) * 0.5f;
            var blue = Math.Min(0.2f + this.World.GetNoiseF(seed++) * 0.4f, green);
            var dryDirt = new Color3(red, green, blue);

            //wet dirt is various browns
            var fade = this.World.GetNoiseF(seed++) * 0.6f;
            var wetDirt = new Color3(
                0.2f + fade,
                0.1f + fade + this.World.GetNoiseF(seed) * 0.1f,
                fade / 2);

            //cold dirt is pale
            var coldDirt = ColorUtils.Interpolate(wetDirt, new Color3(0.7f, 0.7f, 0.7f), 0.5f);
            //warm dirt us a fade from wet to dry
            var warmDirt = ColorUtils.Interpolate(dryDirt, wetDirt, moisture);
            fade = MathUtils.Scalar(temperature, WorldUtils.FREEZING, 1);
            return ColorUtils.Interpolate(coldDirt, warmDirt, fade);
        }


        #region Functions to place individual climates

        ///<summary> Place a canyon </summary>
        private void DoCanyon(int x, int y, int radius) {
            for (var yy = -radius; yy <= radius; yy++) {
                var region = this.World.GetRegion(x, yy + y);
                var step = Math.Abs(yy) / (float) radius;
                step = 1 - step;
                region.Title = "Canyon";
                region.Climate = ClimateType.Canyon;
                region.GeoDetail = 5 + step * 25;
                //r.GeoDetail = 1;
                region.ShapeFlags |= RegionFlags.CanyonNS | RegionFlags.NoBlend;
                this.World.SetRegion(x, y + yy, region);
            }
        }

        ///<summary> Place a desert </summary>
        private void DoDesert(int x, int y, int size) {
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Desert";
                    region.Climate = ClimateType.Desert;
                    region.ColorAtmosphere = new Color3(0.6f, 0.3f, 0.1f);
                    region.GeoDetail = 8;
                    region.GeoBias = 4;
                    region.TreeThreshold = 0;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Place a field of flowers </summary>
        private void DoField(int x, int y, int size) {
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Field";
                    region.Climate = ClimateType.Field;
                    AddFlowers(region, 4);
                    region.ColorAtmosphere = new Color3(0.8f, 0.7f, 0.2f);
                    region.GeoDetail = 8;
                    region.ShapeFlags |= RegionFlags.NoBlend;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Place a forest </summary>
        private void DoForest(int x, int y, int size) {
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Forest";
                    region.Climate = ClimateType.Forest;
                    region.ColorAtmosphere = new Color3(0, 0, 0.5f);
                    region.GeoDetail = 8;
                    region.TreeThreshold = 0.66f;
                    //r.ShapeFlags |= RegionFlags.NoBlend;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Place one mountain </summary>
        private void DoMountain(int x, int y, int size) {
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + x, yy + y);
                    var step = (Math.Max(Math.Abs(xx), Math.Abs(yy)));
                    if (step == 0) {
                        region.Title = "Mountain Summit";
                    } else if (step == size)
                        region.Title = "Mountain Foothills";
                    else {
                        region.Title = "Mountain";
                    }

                    region.MountainHeight = 1 + (size - step);
                    region.GeoDetail = 13 + region.MountainHeight * 7;
                    region.GeoBias = (this.World.GetNoiseF(xx + yy) * 0.5f + region.MountainHeight) * WorldUtils.REGION_SIZE / 2;
                    region.ShapeFlags = RegionFlags.NoBlend;
                    region.Climate = ClimateType.Mountain;
                    this.World.SetRegion(xx + x, yy + y, region);
                }
            }
        }

        ///<summary> Place some plains </summary>
        private void DoPlains(int x, int y, int size) {
            var region = this.World.GetRegion(x, y);
            var water = region.GeoWater;
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Plains";
                    region.Climate = ClimateType.Plains;
                    region.ColorAtmosphere = new Color3(0.9f, 0.9f, 0.6f);
                    region.GeoWater = water;
                    region.GeoBias = 8;
                    region.Moisture = 1;
                    region.TreeThreshold = 0.1f + this.World.GetNoiseF(x + xx + (y + yy) * WorldUtils.WORLD_GRID) * 0.2f;
                    region.GeoDetail = 1.5f + this.World.GetNoiseF(x + xx + (y + yy) * WorldUtils.WORLD_GRID) * 2;
                    AddFlowers(region, 8);
                    region.ShapeFlags |= RegionFlags.NoBlend;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Place a rocky wasteland </summary>
        private void DoRocky(int x, int y, int size) {
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Rocky Wasteland";
                    region.GeoDetail = 40;
                    //r.ShapeFlags = RegionFlags.NoBlend;
                    region.Climate = ClimateType.Rocky;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Place a swamp </summary>
        private void DoSwamp(int x, int y, int size) {
            var region = this.World.GetRegion(x, y);
            var water = region.GeoWater;
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    region = this.World.GetRegion(xx + x, yy + y);
                    region.Title = "Swamp";
                    region.Climate = ClimateType.Swamp;
                    region.ColorAtmosphere = new Color3(0.4f, 1, 0.6f);
                    region.GeoWater = water;
                    region.Moisture = 1;
                    region.GeoDetail = 8;
                    region.HasFlowers = false;
                    region.ShapeFlags |= RegionFlags.NoBlend;
                    this.World.SetRegion(x + xx, y + yy, region);
                }
            }
        }

        ///<summary> Try to place a lake </summary>
        private bool TryLake(int tryX, int tryY, int id) {
            //if (!IsFree (try_x, try_y, size)) 
            //return false;

            const int size = 4;

            //Find the lowest water level in our lake
            var waterLevel = 9999.9f;
            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var region = this.World.GetRegion(xx + tryX, yy + tryY);
                    if (region.Climate != ClimateType.Invalid && region.Climate != ClimateType.River &&
                        region.Climate != ClimateType.RiverBank)
                        return false;
                    if (region.Moisture < 0.5f)
                        return false;
                    waterLevel = Math.Min(waterLevel, region.GeoWater);
                }
            }

            for (var xx = -size; xx <= size; xx++) {
                for (var yy = -size; yy <= size; yy++) {
                    var toCenter = new Vector2(xx, yy);
                    var depth = toCenter.Length;
                    if (depth >= size)
                        continue;
                    depth = size - depth;
                    var region = this.World.GetRegion(xx + tryX, yy + tryY);
                    region.Title = $"Lake{id}";
                    region.GeoWater = waterLevel;
                    region.GeoDetail = 2;
                    region.GeoBias = -4 * depth;
                    region.Climate = ClimateType.Lake;
                    region.ShapeFlags |= RegionFlags.NoBlend;
                    this.World.SetRegion(xx + tryX, yy + tryY, region);
                }
            }

            return true;
        }

        ///<summary> Try to place a river </summary>
        private bool TryRiver(int startX, int startY, int id) {
            var path = new List<Coord>();

            var x = startX;
            var y = startY;
            var lastMove = new Coord();
            while (true) {
                var region = this.World.GetRegion(x, y);
                //If we run into the ocean, then we're done.
                if (region.Climate == ClimateType.Ocean)
                    break;
                if (region.Climate == ClimateType.Mountain)
                    return false;
                //If we run into a river, we've become a tributary.
                if (region.Climate == ClimateType.River) {
                    //don't become a tributary at the start of a river. Looks odd.
                    if (region.RiverSegment < 7)
                        return false;
                    break;
                }

                var lowest = region.GeoWater;
                var toCoast = GetMapSide(x, y);
                //lowest = 999.9f;
                var selected = new Coord();
                foreach (var direction in Directions) {
                    var neighbor = this.World.GetRegion(x + direction.X, y + direction.Y);
                    //Don't reverse course into ourselves
                    if (lastMove == (direction * -1))
                        continue;
                    //ALWAYS go for the ocean, if available
                    if (neighbor.Climate == ClimateType.Ocean) {
                        selected = direction;
                        lowest = neighbor.GeoWater;
                    }

                    //Don't head directly AWAY from the coast
                    if (direction == toCoast * -1)
                        continue;
                    //Go whichever way is lowest
                    if (neighbor.GeoWater < lowest) {
                        selected = direction;
                        lowest = neighbor.GeoWater;
                    }

                    //this.World.SetRegion (x + DIRECTIONS[d].X, y + DIRECTIONS[d].Y, neighbor);
                }

                //If everthing around us is above us, we can't flow downhill
                if (selected.X == 0 && selected.Y == 0) //Let's just head for the edge of the map
                    selected = toCoast;
                lastMove = selected;
                x += selected.X;
                y += selected.Y;
                path.Add(selected);
            }

            //If the river is too short, ditch it.
            if (path.Count < (WorldUtils.WORLD_GRID / 4))
                return false;
            //The river is good. Place it.
            x = startX;
            y = startY;
            var waterStrength = 0.03f;
            var waterLevel = this.World.GetRegion(x, y).GeoWater;
            for (var d = 0; d < path.Count; d++) {
                var region = this.World.GetRegion(x, y);
                if (d == 0)
                    region.Title = $"River{id}-Source";
                else if (d == path.Count - 1)
                    region.Title = $"River{id}-Mouth";
                else
                    region.Title = $"River{id}-{d}";

                //A river should attain full strength after crossing 1/4 of the map
                waterStrength += (1 / ((float) WorldUtils.WORLD_GRID / 4));
                waterStrength = Math.Min(waterStrength, 1);
                region.ShapeFlags |= RegionFlags.NoBlend;
                region.RiverId = id;
                region.Moisture = Math.Max(region.Moisture, 0.5f);
                region.RiverSegment = d;
                //Rivers get flatter as they go, travel from rocky streams to wide river plains
                region.GeoDetail = 28 - waterStrength * 20;
                region.RiverWidth = Math.Min(waterStrength, 1);
                region.Climate = ClimateType.River;
                waterLevel = Math.Min(region.GeoWater, waterLevel);
                //We need to flatten out this space, as well as all of its neighbors.
                region.GeoWater = waterLevel;
                IRegion neighbor;
                for (var xx = x - 1; xx <= x + 1; xx++) {
                    for (var yy = y - 1; yy <= y + 1; yy++) {
                        neighbor = this.World.GetRegion(xx, yy);
                        if (neighbor.Climate != ClimateType.Invalid)
                            continue;
                        if (xx == 0 && yy == 0)
                            continue;
                        neighbor.GeoWater = Math.Min(neighbor.GeoWater, waterLevel);
                        neighbor.GeoBias = region.GeoBias;
                        neighbor.GeoDetail = region.GeoDetail;
                        neighbor.Climate = ClimateType.RiverBank;
                        neighbor.ShapeFlags |= RegionFlags.NoBlend;
                        neighbor.Title = $"River{id}-Banks";
                        this.World.SetRegion(xx, yy, neighbor);
                    }
                }

                var selected = path[d];
                //neighbor = &continent[x + selected.X, y + selected.Y];
                neighbor = this.World.GetRegion(x + selected.X, y + selected.Y);
                if (selected.Y == -1) {
                    //we're moving north
                    neighbor.ShapeFlags |= RegionFlags.RiverS;
                    region.ShapeFlags |= RegionFlags.RiverN;
                } else if (selected.Y == 1) {
                    //we're moving south
                    neighbor.ShapeFlags |= RegionFlags.RiverN;
                    region.ShapeFlags |= RegionFlags.RiverS;
                }

                if (selected.X == -1) {
                    //we're moving west
                    neighbor.ShapeFlags |= RegionFlags.RiverE;
                    region.ShapeFlags |= RegionFlags.RiverW;
                } else if (selected.X == 1) {
                    //we're moving east
                    neighbor.ShapeFlags |= RegionFlags.RiverW;
                    region.ShapeFlags |= RegionFlags.RiverE;
                }

                this.World.SetRegion(x, y, region);
                this.World.SetRegion(x + selected.X, y + selected.Y, neighbor);
                x += selected.X;
                y += selected.Y;
            }

            return true;
        }

        #endregion


        #region Helper functions

        ///<summary> Test the given area and see if it contains the given climate. </summary>
        private bool IsClimatePresent(int x, int y, int radius, ClimateType climate) {
            var start = new Coord(Math.Max(x - radius, 0), Math.Max(y - radius, 0));
            var end = new Coord(Math.Min(x + radius, WorldUtils.WORLD_GRID - 1),
                Math.Min(y + radius, WorldUtils.WORLD_GRID - 1));

            for (var xx = start.X; xx <= end.X; xx++)
                for (var yy = start.Y; yy <= end.Y; yy++)
                    if (this.World.GetRegion(xx, yy).Climate == climate)
                        return true;

            return false;
        }

        ///<summary> In general, what part of the map is this coordinate in? </summary>
        private static string GetDirectionName(int x, int y) {
            var fromCenter = new Coord(
                Math.Abs(x - WorldUtils.WORLD_GRID_CENTER),
                Math.Abs(y - WorldUtils.WORLD_GRID_CENTER));

            return (fromCenter.X < fromCenter.Y)
                ? ((y < WorldUtils.WORLD_GRID_CENTER) ? DIR_NAME_NORTH : DIR_NAME_SOUTH)
                : ((x < WorldUtils.WORLD_GRID_CENTER) ? DIR_NAME_EAST : DIR_NAME_WEST);
        }

        ///<summary> In general, what part of the map is this coordinate in? </summary>
        private static Coord GetMapSide(int x, int y) {
            var fromCenter = new Coord(
                Math.Abs(x - WorldUtils.WORLD_GRID_CENTER),
                Math.Abs(y - WorldUtils.WORLD_GRID_CENTER));
            return fromCenter.X < fromCenter.Y
                ? (y < WorldUtils.WORLD_GRID_CENTER ? North : South)
                : (x < WorldUtils.WORLD_GRID_CENTER ? West : East);
        }

        ///<summary> Check the regions around the given one, see if they are unused. </summary>
        private bool IsFree(int x, int y, int radius) {
            for (var xx = -radius; xx <= radius; xx++) {
                for (var yy = -radius; yy <= radius; yy++) {
                    if (x + xx < 0 || x + xx >= WorldUtils.WORLD_GRID)
                        return false;
                    if (y + yy < 0 || y + yy >= WorldUtils.WORLD_GRID)
                        return false;
                    var region = this.World.GetRegion(x + xx, y + yy);
                    if (region.Climate != ClimateType.Invalid)
                        return false;
                }
            }

            return true;
        }

        ///<summary> Gives a 1 in 'odds' chance of adding flowers to the given region. </summary>
        private void AddFlowers(IRegion region, int odds) {
            region.HasFlowers = this.Random.Next() % odds == 0;
            var shape = this.Random.Next();
            var c = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
            for (var i = 0; i < region.Flowers.Length; i++) {
                region.Flowers[i].Color = c;
                region.Flowers[i].Shape = shape;
                if ((this.Random.Next() % 15) == 0) {
                    shape = this.Random.Next();
                    c = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
                }
            }
        }

        #endregion
    }
}
