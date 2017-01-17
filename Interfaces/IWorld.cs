namespace FrontierSharp.Interfaces {
    using OpenTK;

    public interface IWorld {
        float GetWaterLevel(Vector2 coord);
    }
}
