namespace FrontierSharp.Common.Game {
    using System;

    using Property;

    public interface IGameProperties : IProperties {
        TimeSpan GameTime { get; set; }
        int LastPlayed { get; set; }
    }
}
