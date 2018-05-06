namespace FrontierSharp.Terraform {
    using System;
    using System.Collections.Generic;

    using MersenneTwister;

    using OpenTK;

    using Common.Grid;
    using Common.Region;
    using Common.Terraform;
    using Common.Util;
    using Common.World;

    internal class TerraformImpl : ITerraform {

        #region Constants

        private const float MIN_TEMP = 0;
        private const float MAX_TEMP = 1;

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

        private Random Random { get; }
        private IWorld World { get; }

        #endregion


        public TerraformImpl(IWorld world) {
            this.Random = Randoms.Create();
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
                if (region.Climate != ClimateType.Ocean && region.Climate != ClimateType.Coast && region.Climate != ClimateType.Lake)
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
                    region.GeoDetail = 5 + Entropy(current.X, current.Y) * 10;
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
                        region.ColorMap = region.ShapeFlags.HasFlag(RegionFlags.BeachCliff) ?
                            new Color3(0.3f, 0.3f, 0.3f) :
                            new Color3(0.9f, 0.7f, 0.4f);
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
                        var shape = (int) this.Random.Next();
                        var color = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
                        for (var i = 0; i < region.Flowers.Length; i++) {
                            region.Flowers[i].Color = color;
                            region.Flowers[i].Shape = shape;
                            if ((this.Random.Next() % 15) == 0) {
                                shape = (int) this.Random.Next();
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
            // TODO: convert
            /*
            int lakes;
            int cycles;
            int x, y;
            int range;
            Coord shove;

            lakes = 0;
            cycles = 0;
            range = WorldUtils.WORLD_GRID_CENTER / 4;
            while (lakes < count && cycles < 100)
            {
                //Pick a random spot in the middle of the map
                x = WorldUtils.WORLD_GRID_CENTER + (WorldNoisei(cycles) % range) - range / 2;
                y = WorldUtils.WORLD_GRID_CENTER + (WorldNoisei(cycles * 2) % range) - range / 2;
                //Now push that point away from the middle
                shove = get_map_side(x, y);
                shove *= range;
                if (try_lake(x + shove.X, y + shove.Y, lakes))
                    lakes++;
                cycles++;
            }
            */
        }

        public void Oceans() {
            // TODO: convert
            /*
            int x, y;
            IRegion r;
            bool is_ocean;

            //define the oceans at the edge of the World
            for (x = 0; x < WorldUtils.WORLD_GRID; x++)
            {
                for (y = 0; y < WorldUtils.WORLD_GRID; y++)
                {
                    r = World.GetRegion(x, y);
                    is_ocean = false;
                    if (r.GeoScale <= 0)
                        is_ocean = true;
                    if (x == 0 || y == 0 || x == WorldUtils.WORLD_GRID - 1 || y == WorldUtils.WORLD_GRID - 1)
                        is_ocean = true;
                    if (is_ocean)
                    {
                        r.GeoBias = -10;
                        r.GeoDetail = 0.3f;
                        r.Moisture = 1;
                        r.GeoWater = 0;
                        r.ShapeFlags = RegionFlags.NoBlend;
                        r.ColorAtmosphere = new Color3(0.7f, 0.7f, 1);
                        r.Climate = ClimateType.Ocean;
                        sprintf(r.title, "%s Ocean", GetDirectionName(x, y));
                        this.World.SetRegion(x, y, r);
                    }
                }
            }
            */
        }

        public void Prepare() {
            // TODO: convert
            /*
            int x, y;
            IRegion r;
            Coord from_center;
            Coord offset;

            //Set some defaults
            offset.X = this.Random.Next() % 1024;
            offset.Y = this.Random.Next() % 1024;
            for (x = 0; x < WorldUtils.WORLD_GRID; x++)
            {
                for (y = 0; y < WorldUtils.WORLD_GRID; y++)
                {
                    memset(&r, 0, sizeof(IRegion));
                    sprintf(r.title, "NOTHING");
                    r.GeoBias = r.GeoDetail = 0;
                    r.MountainHeight = 0;
                    r.GridPosition.X = x;
                    r.GridPosition.Y = y;
                    r.tree_threshold = 0.15f;
                    from_center.X = Math.Abs(x - WorldUtils.WORLD_GRID_CENTER);
                    from_center.Y = Math.Abs(y - WorldUtils.WORLD_GRID_CENTER);
                    //Geo scale is a number from -1 to 1. -1 is lowest ocean. 0 is sea level. 
                    //+1 is highest elevation on the island. This is used to guide other derived numbers.
                    r.GeoScale = glVectorLength(glVector((float)from_center.X, (float)from_center.Y));
                    r.GeoScale /= (WorldUtils.WORLD_GRID_CENTER - OCEAN_BUFFER);
                    //Create a steep drop around the edge of the World
                    if (r.GeoScale > 1)
                        r.GeoScale = 1 + (r.GeoScale - 1) * 4;
                    r.GeoScale = 1 - r.GeoScale;
                    r.GeoScale += (Entropy((x + offset.X), (y + offset.Y)) - 0.5f);
                    r.GeoScale += (Entropy((x + offset.X) * FREQUENCY, (y + offset.Y) * FREQUENCY) - 0.2f);
                    r.GeoScale = clamp(r.GeoScale, -1, 1);
                    if (r.GeoScale > 0)
                        r.GeoWater = 1 + r.GeoScale * 16;
                    r.ColorAtmosphere = new Color3(0, 0, 0);
                    r.GeoBias = 0;
                    r.GeoDetail = 0;
                    r.ColorMap = new Color3(0);
                    r.Climate = ClimateType.Invalid;
                    this.World.SetRegion(x, y, r);
                }
            }
            */
        }

        public void Rivers(int count) {
            // TODO: convert
            /*
            int rivers;
            int cycles;
            int x, y;
            int range;

            rivers = 0;
            cycles = 0;
            range = WorldUtils.WORLD_GRID_CENTER / 3;
            while (rivers < count && cycles < 100)
            {
                x = WorldUtils.WORLD_GRID_CENTER + (this.Random.Next() % range) - range / 2;
                y = WorldUtils.WORLD_GRID_CENTER + (this.Random.Next() % range) - range / 2;
                if (try_river(x, y, rivers))
                    rivers++;
                cycles++;
            }
            */
        }

        public void Zones() {
            // TODO: convert
            /*
            int x, y;
            vector<Climate> climates;
            IRegion r;
            int radius;
            Climate c;
            Coord walk;
            int spinner;

            walk.Clear();
            spinner = 0;
            do
            {
                x = walk.X;
                y = walk.Y;// + WorldNoisei (walk.X + walk.Y * WorldUtils.WORLD_GRID) % 4;
                radius = 2 + WorldNoisei(10 + walk.X + walk.Y * WorldUtils.WORLD_GRID) % 9;
                if (is_free(x, y, radius))
                {
                    r = World.GetRegion(x, y);
                    climates.clear();
                    //swamps only appear in wet areas that aren't cold.
                    if (r.Moisture > 0.8f && r.Temperature > 0.5f)
                        climates.push_back(ClimateType.Swamp);
                    //mountains only appear in the middle
                    if (Math.Abs(x - WorldUtils.WORLD_GRID_CENTER) < 10 && radius > 1)
                        climates.push_back(ClimateType.Mountain);
                    //Deserts are HOT and DRY. Duh.
                    if (r.Temperature > TEMP_HOT && r.Moisture < 0.05f && radius > 1)
                        climates.push_back(ClimateType.Desert);
                    //fields should be not too hot or cold.
                    if (r.Temperature > TEMP_TEMPERATE && r.Temperature < TEMP_HOT && r.Moisture > 0.5f && radius == 1)
                        climates.push_back(ClimateType.Field);
                    if (r.Temperature > TEMP_TEMPERATE && r.Temperature < TEMP_HOT && r.Moisture > 0.25f && radius > 1)
                        climates.push_back(ClimateType.Plains);
                    //Rocky wastelands favor cold areas
                    if (r.Temperature < TEMP_TEMPERATE)
                        climates.push_back(ClimateType.Rocky);
                    if (radius > 1 && !(WorldNoisei(spinner++) % 10))
                        climates.push_back(ClimateType.Canyon);
                    if (r.Temperature > TEMP_TEMPERATE && r.Temperature < TEMP_HOT && r.Moisture > 0.5f)
                        climates.push_back(ClimateType.Forest);
                    if (climates.empty())
                    {
                        walk.Walk(WorldUtils.WORLD_GRID);
                        continue;
                    }
                    c = climates[this.Random.Next() % climates.size()];
                    switch (c)
                    {
                        case ClimateType.Rocky:
                            do_rocky(x, y, radius);
                            break;
                        case ClimateType.Mountain:
                            do_mountain(x, y, radius);
                            break;
                        case ClimateType.Canyon:
                            do_canyon(x, y, radius);
                            break;
                        case ClimateType.Swamp:
                            do_swamp(x, y, radius);
                            break;
                        case ClimateType.Field:
                            do_field(x, y, radius);
                            break;
                        case ClimateType.Desert:
                            do_desert(x, y, radius);
                            break;
                        case ClimateType.Plains:
                            do_plains(x, y, radius);
                            break;
                        case ClimateType.Forest:
                            do_forest(x, y, radius);
                            break;
                    }
                }
            } while (!walk.Walk(WorldUtils.WORLD_GRID));
            */
        }

        public Color3 GenerateColor(SurfaceColor color, float moisture, float temperature, int seed) {
            switch (color) {
            case SurfaceColor.Grass:
                return GenerateGrassColor(moisture, temperature, seed);
            case SurfaceColor.Dirt:
                return GenerateDirtColor(moisture, temperature, seed);
            case SurfaceColor.Rock:
                //Devise a rock color
                float fade = MathUtils.Scalar(temperature, WorldUtils.FREEZING, 1);
                
                //Warm rock is red
                var warmRock = new Color3(1, 1 - (float) this.Random.NextDouble() * 0.6f, 1 - (float) this.Random.NextDouble() * 0.6f);
                
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

            return (temperature < TEMP_COLD) ?
                ColorUtils.Interpolate(coldGrass, warmGrass, temperature / TEMP_COLD) :
                warmGrass;
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


        #region Helper functions

        /// <summary> Test the given area and see if it contains the given climate. </summary>
        private bool IsClimatePresent(int x, int y, int radius, ClimateType climate) {
            var start = new Coord(Math.Max(x - radius, 0), Math.Max(y - radius, 0));
            var end = new Coord(Math.Min(x + radius, WorldUtils.WORLD_GRID - 1), Math.Min(y + radius, WorldUtils.WORLD_GRID - 1));

            for (var xx = start.X; xx <= end.X; xx++)
                for (var yy = start.Y; yy <= end.Y; yy++)
                    if (this.World.GetRegion(xx, yy).Climate == climate)
                        return true;

            return false;
        }

        //In general, what part of the map is this coordinate in?
        private static string GetDirectionName(int x, int y) {
            var fromCenter = new Coord(
                Math.Abs(x - WorldUtils.WORLD_GRID_CENTER),
                Math.Abs(y - WorldUtils.WORLD_GRID_CENTER));

            return (fromCenter.X < fromCenter.Y)
                ? ((y < WorldUtils.WORLD_GRID_CENTER) ? DIR_NAME_NORTH : DIR_NAME_SOUTH)
                : ((x < WorldUtils.WORLD_GRID_CENTER) ? DIR_NAME_EAST : DIR_NAME_WEST);
        }

        #endregion
    }
}
/*-----------------------------------------------------------------------------

  From Terraform.cpp


//The number of regions around the edge which should be ocean.
private const float OCEAN_BUFFER      (WorldUtils.WORLD_GRID / 10) 
//This affects the mapping of the coastline.  Higher = busier, more repetitive coast.
private const float FREQUENCY         1 

static Coord direction[] = {
  0, -1, // North
  0, 1,  // South
  1, 0,  // East
 -1, 0   // West
};


//--- Helper functions ---//


//In general, what part of the map is this coordinate in?
Coord get_map_side(int x, int y)
{

    Coord from_center;

    from_center.X = Math.Abs(x - WorldUtils.WORLD_GRID_CENTER);
    from_center.Y = Math.Abs(y - WorldUtils.WORLD_GRID_CENTER);
    if (from_center.X < from_center.Y)
    {
        if (y < WorldUtils.WORLD_GRID_CENTER)
            return direction[NORTH];
        else
            return direction[SOUTH];
    }
    if (x < WorldUtils.WORLD_GRID_CENTER)
        return direction[WEST];
    return direction[EAST];

}

//check the regions around the given one, see if they are unused
static bool is_free(int x, int y, int radius)
{

    int xx, yy;
    IRegion r;

    for (xx = -radius; xx <= radius; xx++)
    {
        for (yy = -radius; yy <= radius; yy++)
        {
            if (x + xx < 0 || x + xx >= WorldUtils.WORLD_GRID)
                return false;
            if (y + yy < 0 || y + yy >= WorldUtils.WORLD_GRID)
                return false;
            r = World.GetRegion(x + xx, y + yy);
            if (r.Climate != ClimateType.Invalid)
                return false;
        }
    }
    return true;

}


//look around the map and find an unused area of the desired size
static bool find_plot(int radius, Coord* result)
{

    int cycles;
    Coord test;

    cycles = 0;
    while (cycles < 20)
    {
        cycles++;
        test.X = this.Random.Next() % WorldUtils.WORLD_GRID;
        test.Y = this.Random.Next() % WorldUtils.WORLD_GRID;
        if (is_free(test.X, test.Y, radius))
        {
            *result = test;
            return true;
        }
    }
    //couldn't find a spot. Map is full, or just bad dice rolls. 
    return false;

}

//Gives a 1 in 'odds' chance of adding flowers to the given region
void add_flowers(IRegion* r, int odds)
{

    Color3 c;
    int shape;

    r.has_flowers = this.Random.Next() % odds == 0;
    shape = this.Random.Next();
    c = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
    for (int i = 0; i < FLOWERS; i++)
    {
        r.ColorFlowers[i] = c;
        r.FlowerShape[i] = shape;
        if ((this.Random.Next() % 15) == 0)
        {
            shape = this.Random.Next();
            c = FlowerPalette[this.Random.Next() % FlowerPalette.Length];
        }
    }


}

//--- Functions to place individual climates ---//

//Place one mountain
static void do_mountain(int x, int y, int mtn_size)
{

    int step;
    IRegion r;
    int xx, yy;

    for (xx = -mtn_size; xx <= mtn_size; xx++)
    {
        for (yy = -mtn_size; yy <= mtn_size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            step = (Math.Max(Math.Abs(xx), Math.Abs(yy)));
            if (step == 0)
            {
                sprintf(r.title, "Mountain Summit");
            } else if (step == mtn_size)
                sprintf(r.title, "Mountain Foothills");
            else
            {
                sprintf(r.title, "Mountain");
            }
            r.MountainHeight = 1 + (mtn_size - step);
            r.GeoDetail = 13 + r.MountainHeight * 7;
            r.GeoBias = (this.World.GetNoiseF(xx + yy) * 0.5f + (float)r.MountainHeight) * REGION_SIZE / 2;
            r.ShapeFlags = RegionFlags.NoBlend;
            r.Climate = ClimateType.Mountain;
            this.World.SetRegion(xx + x, yy + y, r);
        }
    }

}

//Place a rocky wasteland
static void do_rocky(int x, int y, int size)
{

    IRegion r;
    int xx, yy;

    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Rocky Wasteland");
            r.GeoDetail = 40;
            //r.ShapeFlags = RegionFlags.NoBlend;
            r.Climate = ClimateType.Rocky;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}


//Place some plains
static void do_plains(int x, int y, int size)
{

    IRegion r;
    int xx, yy;
    float water;

    r = World.GetRegion(x, y);
    water = r.GeoWater;
    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Plains");
            r.Climate = ClimateType.Plains;
            r.ColorAtmosphere = new Color3(0.9f, 0.9f, 0.6f);
            r.GeoWater = water;
            r.GeoBias = 8;
            r.Moisture = 1;
            r.tree_threshold = 0.1f + this.World.GetNoiseF(x + xx + (y + yy) * WorldUtils.WORLD_GRID) * 0.2f;
            r.GeoDetail = 1.5f + this.World.GetNoiseF(x + xx + (y + yy) * WorldUtils.WORLD_GRID) * 2;
            add_flowers(&r, 8);
            r.ShapeFlags |= RegionFlags.NoBlend;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}


//Place a swamp
static void do_swamp(int x, int y, int size)
{

    IRegion r;
    int xx, yy;
    float water;

    r = World.GetRegion(x, y);
    water = r.GeoWater;
    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Swamp");
            r.Climate = ClimateType.Swamp;
            r.ColorAtmosphere = new Color3(0.4f, 1, 0.6f);
            r.GeoWater = water;
            r.Moisture = 1;
            r.GeoDetail = 8;
            r.has_flowers = false;
            r.ShapeFlags |= RegionFlags.NoBlend;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}

//Place a field of flowers
static void do_field(int x, int y, int size)
{

    IRegion r;
    int xx, yy;

    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Field");
            r.Climate = ClimateType.Field;
            add_flowers(&r, 4);
            r.ColorAtmosphere = new Color3(0.8f, 0.7f, 0.2f);
            r.GeoDetail = 8;
            r.ShapeFlags |= RegionFlags.NoBlend;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}


//Place a forest
static void do_forest(int x, int y, int size)
{

    IRegion r;
    int xx, yy;

    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Forest");
            r.Climate = ClimateType.Forest;
            r.ColorAtmosphere = new Color3(0, 0, 0.5f);
            r.GeoDetail = 8;
            r.tree_threshold = 0.66f;
            //r.ShapeFlags |= RegionFlags.NoBlend;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}


//Place a desert
static void do_desert(int x, int y, int size)
{

    IRegion r;
    int xx, yy;

    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + x, yy + y);
            sprintf(r.title, "Desert");
            r.Climate = ClimateType.Desert;
            r.ColorAtmosphere = new Color3(0.6f, 0.3f, 0.1f);
            r.GeoDetail = 8;
            r.GeoBias = 4;
            r.tree_threshold = 0;
            this.World.SetRegion(x + xx, y + yy, r);
        }
    }

}



static void do_canyon(int x, int y, int radius)
{

    IRegion r;
    int yy;
    float step;

    for (yy = -radius; yy <= radius; yy++)
    {
        r = World.GetRegion(x, yy + y);
        step = (float)Math.Abs(yy) / (float)radius;
        step = 1 - step;
        sprintf(r.title, "Canyon");
        r.Climate = ClimateType.Canyon;
        r.GeoDetail = 5 + step * 25;
        //r.GeoDetail = 1;
        r.ShapeFlags |= RegionFlags.CANYON_NS | RegionFlags.NoBlend;
        this.World.SetRegion(x, y + yy, r);
    }

}

static bool try_lake(int try_x, int try_y, int id)
{

    IRegion r;
    int xx, yy;
    int size;
    float depth;
    float water_level;
    Vector2 to_center;

    size = 4;
    //if (!is_free (try_x, try_y, size)) 
    //return false;
    //Find the lowest water level in our lake
    water_level = 9999.9f;
    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            r = World.GetRegion(xx + try_x, yy + try_y);
            if (r.Climate != ClimateType.Invalid && r.Climate != ClimateType.River && r.Climate != ClimateType.RiverBank)
                return false;
            if (r.Moisture < 0.5f)
                return false;
            water_level = Math.Min(water_level, r.GeoWater);
        }
    }
    for (xx = -size; xx <= size; xx++)
    {
        for (yy = -size; yy <= size; yy++)
        {
            to_center = glVector((float)xx, (float)yy);
            depth = to_center.Length();
            if (depth >= (float)size)
                continue;
            depth = (float)size - depth;
            r = World.GetRegion(xx + try_x, yy + try_y);
            sprintf(r.title, "Lake%d", id);
            r.GeoWater = water_level;
            r.GeoDetail = 2;
            r.GeoBias = -4 * depth;
            r.Climate = ClimateType.Lake;
            r.ShapeFlags |= RegionFlags.NoBlend;
            this.World.SetRegion(xx + try_x, yy + try_y, r);
        }
    }
    return true;

}

static bool try_river(int start_x, int start_y, int id)
{

    IRegion r;
    IRegion neighbor;
    vector<Coord> path;
    Coord selected;
    Coord last_move;
    Coord to_coast;
    int x, y;
    int xx, yy;
    int d;
    float lowest;
    float water_level;
    float water_strength;

    x = start_x;
    y = start_y;
    while (1)
    {
        r = World.GetRegion(x, y);
        //If we run into the ocean, then we're done.
        if (r.Climate == ClimateType.Ocean)
            break;
        if (r.Climate == ClimateType.Mountain)
            return false;
        //If we run into a river, we've become a tributary.
        if (r.Climate == ClimateType.River)
        {
            //don't become a tributary at the start of a river. Looks odd.
            if (r.river_segment < 7)
                return false;
            break;
        }
        lowest = r.GeoWater;
        to_coast = get_map_side(x, y);
        //lowest = 999.9f;
        selected.Clear();
        for (d = 0; d < 4; d++)
        {
            neighbor = World.GetRegion(x + direction[d].X, y + direction[d].Y);
            //Don't reverse course into ourselves
            if (last_move == (direction[d] * -1))
                continue;
            //ALWAYS go for the ocean, if available
            if (neighbor.Climate == ClimateType.Ocean)
            {
                selected = direction[d];
                lowest = neighbor.GeoWater;
            }
            //Don't head directly AWAY from the coast
            if (direction[d] == to_coast * -1)
                continue;
            //Go whichever way is lowest
            if (neighbor.GeoWater < lowest)
            {
                selected = direction[d];
                lowest = neighbor.GeoWater;
            }
            //this.World.SetRegion (x + direction[d].X, y + direction[d].Y, neighbor);
        }
        //If everthing around us is above us, we can't flow downhill
        if (!selected.X && !selected.Y) //Let's just head for the edge of the map
            selected = to_coast;
        last_move = selected;
        x += selected.X;
        y += selected.Y;
        path.push_back(selected);
    }
    //If the river is too short, ditch it.
    if (path.size() < (WorldUtils.WORLD_GRID / 4))
        return false;
    //The river is good. Place it.
    x = start_x;
    y = start_y;
    water_strength = 0.03f;
    water_level = World.GetRegion(x, y).GeoWater;
    for (d = 0; d < path.size(); d++)
    {
        r = World.GetRegion(x, y);
        if (!d)
            sprintf(r.title, "River%d-Source", id);
        else if (d == path.size() - 1)
            sprintf(r.title, "River%d-Mouth", id);
        else
            sprintf(r.title, "River%d-%d", id, d);
        //A river should attain full strength after crossing 1/4 of the map
        water_strength += (1 / ((float)WorldUtils.WORLD_GRID / 4));
        water_strength = Math.Min(water_strength, 1);
        r.ShapeFlags |= RegionFlags.NoBlend;
        r.river_id = id;
        r.Moisture = Math.Max(r.Moisture, 0.5f);
        r.river_segment = d;
        //Rivers get flatter as they go, travel from rocky streams to wide river plains
        r.GeoDetail = 28 - water_strength * 20;
        r.river_width = Math.Min(water_strength, 1);
        r.Climate = ClimateType.River;
        water_level = Math.Min(r.GeoWater, water_level);
        //We need to flatten out this space, as well as all of its neighbors.
        r.GeoWater = water_level;
        for (xx = x - 1; xx <= x + 1; xx++)
        {
            for (yy = y - 1; yy <= y + 1; yy++)
            {
                neighbor = World.GetRegion(xx, yy);
                if (neighbor.Climate != ClimateType.Invalid)
                    continue;
                if (!xx && !yy)
                    continue;
                neighbor.GeoWater = Math.Min(neighbor.GeoWater, water_level);
                neighbor.GeoBias = r.GeoBias;
                neighbor.GeoDetail = r.GeoDetail;
                neighbor.Climate = ClimateType.RiverBank;
                neighbor.ShapeFlags |= RegionFlags.NoBlend;
                sprintf(neighbor.title, "River%d-Banks", id);
                this.World.SetRegion(xx, yy, neighbor);
            }
        }
        selected = path[d];
        //neighbor = &continent[x + selected.X, y + selected.Y];
        neighbor = World.GetRegion(x + selected.X, y + selected.Y);
        if (selected.Y == -1)
        {//we're moving north
            neighbor.ShapeFlags |= RegionFlags.RIVERS;
            r.ShapeFlags |= RegionFlags.RIVERN;
        }
        if (selected.Y == 1)
        {//we're moving south
            neighbor.ShapeFlags |= RegionFlags.RIVERN;
            r.ShapeFlags |= RegionFlags.RIVERS;
        }
        if (selected.X == -1)
        {//we're moving west
            neighbor.ShapeFlags |= RegionFlags.RIVERE;
            r.ShapeFlags |= RegionFlags.RIVERW;
        }
        if (selected.X == 1)
        {//we're moving east
            neighbor.ShapeFlags |= RegionFlags.RIVERW;
            r.ShapeFlags |= RegionFlags.RIVERE;
        }
        this.World.SetRegion(x, y, r);
        this.World.SetRegion(x + selected.X, y + selected.Y, neighbor);
        x += selected.X;
        y += selected.Y;
    }
    return true;

}
*/