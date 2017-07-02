namespace FrontierSharp.Common {
    using OpenTK;

    using Property;

    public interface IWorld : IHasProperties, IModule {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
    }
}
