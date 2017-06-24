namespace FrontierSharp.Interfaces {
    using OpenTK;

    using Property;

    public interface IWorld : IHasProperties {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
    }
}
