namespace FrontierSharp.Common {
    using Region;
    using OpenTK;

    using Property;

    public interface IWorld : IHasProperties, IModule {

        uint MapId { get; }

        float GetWaterLevel(Vector2 coord);
        IRegion GetRegion(int x, int y);
    }
}
