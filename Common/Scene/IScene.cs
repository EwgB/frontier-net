namespace FrontierSharp.Common.Scene {
    using Property;

    /// <summary>
    /// Manages all the various objects that need to be created, rendered,
    /// and deleted at various times.If it gets drawn, and if there's more than
    /// one of it, then it should go here.
    /// </summary>
    public interface IScene : IBaseModule, IHasProperties, ITimeCapped, IRenderable {
        ISceneProperties SceneProperties { get; }

        /// <summary>How far it is from the center of the terrain grid to the outer edge</summary>
        float VisibleRange { get; }

        void RenderDebug();

        void Clear();
        void Generate();
        void Progress(out uint ready, out uint total);

        /// <summary>
        /// This is called to restart the terrain grid manager. After the terrains are built,
        /// we need to pass over them again so they can do their stitching.
        /// </summary>
        void RestartProgress();
    }
}

/* From Scene.h
enum
{
    DEBUG_RENDER_NONE,
    DEBUG_RENDER_MOIST,
    DEBUG_RENDER_TEMP,
    DEBUG_RENDER_UNIQUE,
    DEBUG_RENDER_TYPES
};


void SceneGenerate();
void SceneProgress(unsigned* ready, unsigned* total);
void SceneRestartProgress();
class CTerrain* SceneTerrainGet(int x, int y);
void SceneTexturePurge();
*/