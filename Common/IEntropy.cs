namespace FrontierSharp.Common {
    /// <summary>
    ///     Provides a map of erosion-simulated terrain data.
    ///     This map is kept at non-powers-of-2 sizes in order
    ///     to avoid tiling as much as possible.
    /// </summary>
    public interface IEntropy {
        float GetEntropy(float x, float y);
        float GetEntropy(int x, int y);
    }
}
