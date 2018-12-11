﻿namespace FrontierSharp.Cache {
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common.Game;
    using Common.Grid;
    using Common.Property;
    using Common.Region;
    using Common.Util;
    using Common.World;

    /// <summary>
    ///   This class is used to generate and cache cachePages of world texture data.
    ///   The cachePages are generated by combining the topographical data(elevations)
    ///   with the region data(modifying the evevation to make the different land
    ///   formations) and then is used to generate the table of surface data, which
    ///   describes how to paint the textures for the given area.
    /// </summary>
    internal sealed class CachePage : ICachePage {

        private enum Stages {
            Begin,
            Position,
            Normal,
            Surface1,
            Surface2,
            Color,
            Trees,
            Save,
            Done
        }

        private struct PageCell {
            public SurfaceTypes Surface;
            public float WaterLevel;
            public float Elevation;
            public float Detail;
            public Color3 Color;
            public Vector3 Normal;
            public int TreeId;
        }

        [Serializable]
        private class CachePageData {
            public readonly PageCell[,] Cells = new PageCell[PAGE_SIZE, PAGE_SIZE];
        }
 
        #region Constants

        public const int PAGE_SIZE = 128;
        private const int TREE_SPACING = 8; //Power of 2, how far apart trees should be. (Roughly)
        private const int TREE_MAP = (PAGE_SIZE / TREE_SPACING);

        private static readonly IFormatter Formatter = new BinaryFormatter();
        private static readonly TimeSpan ExpireInterval = TimeSpan.FromMilliseconds(30000);
        private static readonly TimeSpan SaveInterval = TimeSpan.FromMilliseconds(1000);

        #endregion


        #region Modules

        private IGame Game { get; }
        private IProperties Properties { get; }
        private IWorld World { get; }

        #endregion


        #region Members and properties

        private CachePageData data = new CachePageData();

        private Coord origin;
        private Coord walk;
        private TimeSpan lastTouched;
        private Stages stage = Stages.Begin;
        private TimeSpan saveCooldown = new TimeSpan(0);
        private BoundingBox boundingBox = new BoundingBox();

        public bool IsExpired => (lastTouched + ExpireInterval) < Game.GameProperties.GameTime;
        private string GetPageFileName(Coord p) => Path.Combine(Game.GameDirectory, $"cache{p.X}-{p.Y}.pag");

        #endregion


        public CachePage(IGame game, IProperties properties, IWorld world) {
            Game = game;
            Properties = properties;
            World = world;
        }

        //    bool Ready();


        #region Getters

        public float GetElevation(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).Elevation;
        }

        public float GetDetail(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).Detail;
        }

        public int GetTree(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).TreeId;
        }

        public Vector3 GetPosition(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return new Vector3(
                x + origin.X * PAGE_SIZE,
                y + origin.Y * PAGE_SIZE,
                GetPageCell(x, y).Elevation);
        }

        public Vector3 GetNormal(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).Normal;
        }

        public Color3 GetColor(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).Color;
        }

        public SurfaceTypes GetSurface(int x, int y) {
            lastTouched = Game.GameProperties.GameTime;
            return GetPageCell(x, y).Surface;
        }

        public bool IsReady() {
            lastTouched = Game.GameProperties.GameTime;
            return stage == Stages.Done;
        }

        #endregion


        #region Public methods

        public void Render() {
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            var elapsed = Game.GameProperties.GameTime - lastTouched;
            var n = MathHelper.Clamp(elapsed.TotalMilliseconds / ExpireInterval.TotalMilliseconds, 0, 1);
            GL.Color3(n, 1 - n, 0);
            boundingBox.Render();
        }

        public void Build(double stopAt) {
            while (stage != Stages.Done && Game.GameProperties.GameTime.TotalMilliseconds < stopAt) {
                switch (stage) {
                case Stages.Begin:
                    stage++;
                    break;
                case Stages.Position:
                    DoPosition();
                    break;
                case Stages.Normal:
                    DoNormal();
                    break;
                case Stages.Surface1:
                case Stages.Surface2:
                    DoSurface();
                    break;
                case Stages.Color:
                    DoColor();
                    break;
                case Stages.Trees:
                    DoTrees();
                    break;
                case Stages.Save:
                    Save();
                    return;
                }
            }
        }

        public void Save() {
            if (!Properties.GetProperty<bool>("cache.active").Value) {
                stage++;
                return;
            }

            var now = Game.GameProperties.GameTime;
            if (now < saveCooldown || stage < Stages.Save)
                return;
            if (stage == Stages.Save)
                stage++;
            using (var stream = File.Open(GetPageFileName(origin), FileMode.Create))
                Formatter.Serialize(stream, data);
            saveCooldown = now + SaveInterval;
        }

        public void Load(int originX, int originY) {
            origin = new Coord(originX, originY);
            stage = Stages.Begin;
            boundingBox.Clear();

            var path = GetPageFileName(origin);
            if (File.Exists(path))
                using (var stream = File.Open(path, FileMode.Open)) {
                    data = (CachePageData) Formatter.Deserialize(stream);
                }

            walk = new Coord();
            lastTouched = Game.GameProperties.GameTime;
        }

        #endregion


        #region Private methods

        private PageCell GetPageCell(int x, int y) => data.Cells[x % PAGE_SIZE, y % PAGE_SIZE];

        private void DoPosition() {
            var worldX = (origin.X * PAGE_SIZE + walk.X);
            var worldY = (origin.Y * PAGE_SIZE + walk.Y);
            var c = World.GetCell(worldX, worldY);
            data.Cells[walk.X, walk.Y].Elevation = c.Elevation;
            data.Cells[walk.X, walk.Y].Detail = c.Detail;
            data.Cells[walk.X, walk.Y].WaterLevel = c.WaterLevel;
            data.Cells[walk.X, walk.Y].TreeId = 0;
            boundingBox.ContainPoint(GetPosition(worldX, worldY));
            walk = walk.Walk(PAGE_SIZE, out var rolledOver);
            if (rolledOver)
                stage++;
        }

        private void DoColor() {
            var worldX = (origin.X * PAGE_SIZE + walk.X);
            var worldY = (origin.Y * PAGE_SIZE + walk.Y);
            var cell = data.Cells[walk.X, walk.Y];
            if (cell.Surface == SurfaceTypes.Grass || cell.Surface == SurfaceTypes.GrassEdge)
                cell.Color = World.GetColor(worldX, worldY, SurfaceColor.Grass);
            else if (cell.Surface == SurfaceTypes.Dirt ||
                     cell.Surface == SurfaceTypes.DirtDark ||
                     cell.Surface == SurfaceTypes.Forest)
                cell.Color = World.GetColor(worldX, worldY, SurfaceColor.Dirt);
            else if (cell.Surface == SurfaceTypes.Sand || cell.Surface == SurfaceTypes.SandDark)
                cell.Color = World.GetColor(worldX, worldY, SurfaceColor.Sand);
            else if (cell.Surface == SurfaceTypes.Snow)
                cell.Color = Color3.White;
            else
                cell.Color = World.GetColor(worldX, worldY, SurfaceColor.Rock);

            walk = walk.Walk(PAGE_SIZE, out var rolledOver);
            if (rolledOver)
                stage++;
        }

        private void DoNormal() {
            var worldX = (float) (origin.X + walk.X);
            var worldY = (float) (origin.Y + walk.Y);

            Vector3 normalX;
            if (walk.X < 1 || walk.X >= PAGE_SIZE - 1)
                normalX = new Vector3(-1, 0, 0);
            else
                normalX = new Vector3(worldX - 1, worldY, data.Cells[walk.X - 1, walk.Y].Elevation) -
                          new Vector3(worldX + 1, worldY, data.Cells[walk.X + 1, walk.Y].Elevation);

            Vector3 normalY;
            if (walk.Y < 1 || walk.Y >= PAGE_SIZE - 1)
                normalY = new Vector3(0, -1, 0);
            else
                normalY = new Vector3(worldX, worldY - 1, data.Cells[walk.X, walk.Y - 1].Elevation) -
                          new Vector3(worldX, worldY, data.Cells[walk.X, walk.Y + 1].Elevation);

            var normal = Vector3.Cross(normalX, normalY);
            normal.Z *= WorldUtils.NORMAL_SCALING;
            normal.Normalize();
            data.Cells[walk.X, walk.Y].Normal = normal;

            walk = walk.Walk(PAGE_SIZE, out var rolledOver);
            if (rolledOver)
                stage++;
        }

        private void DoTrees() {
            var region = World.GetRegionFromPosition(
                origin.X * PAGE_SIZE + walk.X,
                origin.Y * PAGE_SIZE + walk.Y);

            var tree = World.GetTree(region.TreeType);
            var best = tree.GrowsHigh ? -99999.9f : 99999.9f;

            var plant = new Coord();
            var valid = false;
            for (var x = 0; x < TREE_SPACING - 2; x++) {
                for (var y = 0; y < TREE_SPACING - 2; y++) {
                    var cell = data.Cells[walk.X * TREE_SPACING + x, walk.Y * TREE_SPACING + y];
                    if (cell.Surface != SurfaceTypes.Grass && cell.Surface != SurfaceTypes.Snow && cell.Surface != SurfaceTypes.Forest)
                        continue;
                    //Don't spawn trees that might touch water. Looks odd.
                    if (cell.Elevation < cell.WaterLevel + 1.2f)
                        continue;
                    if (tree.GrowsHigh && (cell.Detail + region.TreeThreshold) > 1 && cell.Elevation > best) {
                        plant = new Coord(
                            walk.X * TREE_SPACING + x,
                            walk.Y * TREE_SPACING + y);
                        best = cell.Elevation;
                        valid = true;
                    }

                    if (!tree.GrowsHigh && (cell.Detail - region.TreeThreshold) < 0 && cell.Elevation < best) {
                        plant = new Coord(
                            walk.X * TREE_SPACING + x,
                            walk.Y * TREE_SPACING + y);
                        best = cell.Elevation;
                        valid = true;
                    }
                }
            }

            if (valid) {
                data.Cells[plant.X, plant.Y].TreeId = region.TreeType;
            }

            walk = walk.Walk(TREE_MAP, out var rolledOver);
            if (rolledOver)
                stage++;
        }

        private void DoSurface() {
            var worldpos = new Coord(
                origin.X * PAGE_SIZE + walk.X,
                origin.Y * PAGE_SIZE + walk.Y);
            var region = World.GetRegionFromPosition(worldpos.X, worldpos.Y);
            PageCell c = data.Cells[walk.X, walk.Y];

            if (stage == Stages.Surface1) {
                //Get the elevation of our neighbors
                float low;
                var high = low = c.Elevation;
                for (var xx = -2; xx <= 2; xx++) {
                    var neighborX = walk.X + xx;
                    if (neighborX < 0 || neighborX >= PAGE_SIZE)
                        continue;
                    for (var yy = -2; yy <= 2; yy++) {
                        var neighborY = walk.Y + yy;
                        if (neighborY < 0 || neighborY >= PAGE_SIZE)
                            continue;
                        high = Math.Max(high, data.Cells[neighborX, neighborY].Elevation);
                        low = Math.Min(low, data.Cells[neighborX, neighborY].Elevation);
                    }
                }

                var delta = high - low;
                //Default surface. If the climate can support life, default to grass.
                if (region.Temperature > 0.1f && region.Moisture > 0.1f)
                    c.Surface = SurfaceTypes.Grass;
                else //Too cold or dry
                    c.Surface = SurfaceTypes.Rock;
                if (region.Climate == ClimateType.Desert)
                    c.Surface = SurfaceTypes.Sand;
                //Sand is only for coastal regions
                if (low <= 2 && (region.Climate == ClimateType.Coast))
                    c.Surface = SurfaceTypes.Sand;
                if (low <= 2 && (region.Climate == ClimateType.Ocean))
                    c.Surface = SurfaceTypes.Sand;
                //Forests are for... forests?
                if (c.Detail < 0.75f && c.Detail > 0.25f && (region.Climate == ClimateType.Forest))
                    c.Surface = SurfaceTypes.Forest;
                if (delta >= region.Moisture * 6)
                    c.Surface = SurfaceTypes.Dirt;
                if (low <= region.GeoWater && region.Climate != ClimateType.Swamp)
                    c.Surface = SurfaceTypes.Dirt;
                if (low <= region.GeoWater && region.Climate != ClimateType.Swamp)
                    c.Surface = SurfaceTypes.DirtDark;
                //The colder it is, the more surface becomes snow, beginning at the lowest points.
                if (region.Temperature < WorldUtils.FREEZING) {
                    var fade = region.Temperature / WorldUtils.FREEZING;
                    if ((1 - c.Detail) > fade)
                        c.Surface = SurfaceTypes.Snow;
                }

                if (low <= 2.5f && (region.Climate == ClimateType.Ocean))
                    c.Surface = SurfaceTypes.Sand;
                if (low <= 2.5f && (region.Climate == ClimateType.Coast))
                    c.Surface = SurfaceTypes.Sand;
                //dirt touched by water is dark
                if (region.Climate != ClimateType.Swamp) {
                    if (c.Surface == SurfaceTypes.Sand && low <= 0)
                        c.Surface = SurfaceTypes.SandDark;
                    if (low <= c.WaterLevel)
                        c.Surface = SurfaceTypes.DirtDark;
                }

                if (delta > 4 && region.Temperature > 0)
                    c.Surface = SurfaceTypes.Rock;
                if ((region.Climate == ClimateType.Desert) && c.Surface != SurfaceTypes.Rock)
                    c.Surface = SurfaceTypes.Sand;
            } else {
                if (c.Surface == SurfaceTypes.Grass && walk.X > 0 && walk.X < PAGE_SIZE - 1 && walk.Y > 0 &&
                    walk.Y < PAGE_SIZE - 1) {
                    var allGrass = true;
                    for (var xx = -1; xx <= 1; xx++) {
                        if (!allGrass)
                            break;
                        for (var yy = -1; yy <= 1; yy++) {
                            if (data.Cells[walk.X + xx, walk.Y + yy].Surface != SurfaceTypes.Grass &&
                                data.Cells[walk.X + xx, walk.Y + yy].Surface != SurfaceTypes.GrassEdge) {
                                allGrass = false;
                                break;
                            }
                        }
                    }

                    if (!allGrass)
                        c.Surface = SurfaceTypes.GrassEdge;
                }
            }

            walk = walk.Walk(PAGE_SIZE, out var rolledOver);
            if (rolledOver)
                stage++;
        }

        #endregion
    }
}
