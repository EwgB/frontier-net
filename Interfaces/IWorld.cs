namespace FrontierSharp.Interfaces {
    using OpenTK;

    public interface IWorld {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
    }
}
