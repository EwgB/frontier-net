namespace FrontierSharp.Interfaces {
    using OpenTK;

    public interface IWorld : IHasProperties {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
    }
}
