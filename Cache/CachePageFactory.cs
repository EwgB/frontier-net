namespace FrontierSharp.Cache {
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    using Common.Game;
    using Common.Property;
    using Common.Util;

    internal class CachePageFactory {

        #region Modules

        private IGame Game { get; }
        private IProperties Properties { get; }

        #endregion


        #region Constants

        private readonly TimeSpan saveInterval = TimeSpan.FromMilliseconds(1000);

        private readonly IFormatter formatter = new BinaryFormatter();

        #endregion


        internal CachePageFactory(IGame game, IProperties properties) {
            this.Game = game;
            this.Properties = properties;
        }

        internal void SaveCachePage(CachePage page) {
            if (!this.Properties.GetProperty<bool>("cache.active").Value) {
                page.Stage++;
                return;
            }

            var now = this.Game.GameProperties.GameTime;
            if (now < page.SaveCooldown || page.Stage < CachePage.Stages.Save)
                return;
            if (page.Stage == CachePage.Stages.Save)
                page.Stage++;
            using (var stream = File.Open(GetPageFileName(page.Origin), FileMode.Create))
                this.formatter.Serialize(stream, this);
            page.SaveCooldown = now + this.saveInterval;
        }

        internal CachePage LoadCachePage(int originX, int originY) {
            var origin = new Coord(originX, originY);
            var path = GetPageFileName(origin);
            CachePage page;
            if (!File.Exists(path))
                page = new CachePage(this.Game, this);
            else
                using (var stream = File.Open(path, FileMode.Open)) {
                    page = (CachePage) this.formatter.Deserialize(stream);
                    page.Game = this.Game;
                    page.PageFactory = this;
                }

            page.Origin = new Coord(originX, originY);
            page.LastTouched = this.Game.GameProperties.GameTime;
            return page;
        }

        private string GetPageFileName(Coord p) => Path.Combine(this.Game.GameDirectory, $"cache{p.X}-{p.Y}.pag");

    }
}
