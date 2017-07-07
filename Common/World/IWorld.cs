namespace FrontierSharp.Common.World {
    using Region;
    using OpenTK;

    using Property;

    public interface IWorld : IHasProperties, IModule {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
        float GetWaterLevel(float x, float y);

        IRegion GetRegion(int x, int y);
    }
}
