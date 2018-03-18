namespace FrontierSharp.Cache {
    using System;
    using System.IO;
    using System.Linq;

    using Ninject;

    using NLog;

    using OpenTK;

    using Common;
    using Common.Game;
    using Common.Grid;
    using Common.Util;
    using Common.World;

    using Ninject.Infrastructure.Language;

    internal class CacheImpl : ICache {
        #region Constants

        private const int PAGE_GRID = (WorldUtils.WORLD_SIZE_METERS / CachePage.PAGE_SIZE);

        #endregion


        #region Private members

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly CachePageFactory cachePageFactory;

        private readonly CachePage[,] cachePages = new CachePage[PAGE_GRID, PAGE_GRID];
        private int pageCount;
        private Coord walk = new Coord(0, 0);

        #endregion


        #region Modules

        private IGame Game { get; }

        #endregion


        public CacheImpl(IGame game, CachePageFactory cachePageFactory) {
            this.Game = game;
            this.cachePageFactory = cachePageFactory;
        }


        #region Private functions

        private CachePage LookupPage(int worldX, int worldY) {
            if (worldX < 0 || worldY < 0)
                return null;
            var pageX = PageFromPos(worldX);
            var pageY = PageFromPos(worldY);
            if (pageX < 0 || pageX >= PAGE_GRID || pageY < 0 || pageY >= PAGE_GRID)
                return null;
            return this.cachePages[pageX, pageY];
        }

        private static int PageFromPos(int cell) => cell / CachePage.PAGE_SIZE;

        #endregion


        #region Getters

        public float GetDetail(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetDetail(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ?? 0;
        }

        public float GetElevation(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetElevation(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ?? -99;
        }

        public float GetElevation(float x, float y) {
            var cellX = (int) x;
            var cellY = (int) y;
            var dX = x - cellX;
            var dY = y - cellY;
            var y0 = GetElevation(cellX, cellY);
            var y1 = GetElevation(cellX + 1, cellY);
            var y2 = GetElevation(cellX, cellY + 1);
            var y3 = GetElevation(cellX + 1, cellY + 1);

            float a, b, c;
            if (dX < dY) {
                c = y2 - y0;
                b = y3 - y2;
                a = y0;
            } else {
                c = y3 - y1;
                b = y1 - y0;
                a = y0;
            }

            return (a + b * dX + c * dY);
        }

        public Vector3 GetNormal(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetNormal(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ?? Vector3.UnitZ;
        }

        public bool IsPointAvailable(int worldX, int worldY) {
            worldX = Math.Max(0, worldX);
            worldY = Math.Max(0, worldY);
            var pageX = PageFromPos(worldX);
            var pageY = PageFromPos(worldY);
            if (pageX < 0 || pageX >= PAGE_GRID || pageY < 0 || pageY >= PAGE_GRID)
                return false;
            var p = this.cachePages[pageX, pageY];
            if (p == null) {
                p = this.cachePageFactory.LoadCachePage(pageX, pageY);
                this.cachePages[pageX, pageY] = p;
                this.pageCount++;
            }

            return p.IsReady();
        }

        public Vector3 GetPosition(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetPosition(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ??
                   new Vector3(worldX, worldY, 0);
        }

        public SurfaceTypes GetSurface(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetSurface(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ?? SurfaceTypes.Null;
        }

        public uint GetTree(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetTree(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ?? 0;
        }

        public Color3 GetSurfaceColor(int worldX, int worldY) {
            var p = LookupPage(worldX, worldY);
            return p?.GetColor(worldX % CachePage.PAGE_SIZE, worldY % CachePage.PAGE_SIZE) ??
                   Color3.Magenta; /* So we notice */
        }

        public void PrintSize() {
            var files = Directory
                .EnumerateFiles(this.Game.GameDirectory, "*.pag", SearchOption.TopDirectoryOnly)
                .Select(file => new FileInfo(file))
                .Select(fileInfo => fileInfo.Length)
                .ToList();

            Log.Info("Cache contains {0} files, {1} bytes used.", files.Count, files.Sum());
        }

        #endregion


        public void Purge() {
            for (var y = 0; y < PAGE_GRID; y++) {
                for (var x = 0; x < PAGE_GRID; x++) {
                    if (this.cachePages[x, y] != null) {
                        this.pageCount--;
                        this.cachePageFactory.SaveCachePage(this.cachePages[x, y]);
                        this.cachePages[x, y] = null;
                    }
                }
            }
        }

        public void Dump() {
            Purge();

            Directory
                .EnumerateFiles(this.Game.GameDirectory, "*.pag", SearchOption.TopDirectoryOnly)
                .Select(file => new FileInfo(file))
                .Map(file => {
                    Log.Info("Deleting file {0}...", file.Name);
                    file.Delete();
                });
        }

        public void RenderDebug() {
            for (var y = 0; y < PAGE_GRID; y++) {
                for (var x = 0; x < PAGE_GRID; x++) {
                    this.cachePages[x, y]?.Render();
                }
            }
        }

        public void Update(double stopAt) {
            //TextPrint ("%d cachePages. (%s)", this.pageCount, TextBytes (sizeof (CachePage) * this.pageCount));
            var count = 0;
            //Pass over the table a bit at a time and do garbage collection
            while (count < (PAGE_GRID / 4) && this.Game.GameProperties.GameTime.TotalMilliseconds < stopAt) {
                var page = this.cachePages[this.walk.X, this.walk.Y];
                if (page != null && page.IsExpired) {
                    this.cachePageFactory.SaveCachePage(page);
                    this.cachePages[this.walk.X, this.walk.Y] = null;
                    this.pageCount--;
                }

                count++;
                this.walk.Walk(PAGE_GRID, out var _);
            }
        }

        public void UpdatePage(int worldX, int worldY, double stopAt) {
            var p = LookupPage(worldX, worldY);
            if (p == null)
                return;
            p.Build(stopAt);
        }
    }
}
