namespace FrontierSharp.Scene {
    using System.Collections.Generic;

    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Avatar;
    using Common.Grid;
    using Common.Particles;
    using Common.Property;
    using Common.Scene;
    using Common.Shaders;
    using Common.Terrain;
    using Common.Textures;

    public class SceneImpl : IScene {

        #region Constants
        private const int BRUSH_GRID = 7;
        private const int FOREST_GRID = 7;
        private const int GRASS_GRID = 5;
        private const int TERRAIN_GRID = 9;
        private const int PARTICLE_GRID = 3;
        #endregion

        #region Modules

        private readonly IAvatar avatar;
        private readonly IGame game;
        private readonly IParticles particles;
        private readonly IShaders shaders;
        private readonly ISky sky;
        private readonly IText text;
        private readonly ITextures textures;
        private readonly IWater water;

        #endregion

        #region Properties

        private readonly ISceneProperties properties = new SceneProperties();
        public IProperties Properties { get { return this.properties; } }
        public ISceneProperties SceneProperties { get { return this.properties; } }

        public float VisibleRange { get { return (TERRAIN_GRID / 2f) * GridUtils.TERRAIN_SIZE; } }

        #endregion

        #region Memeber variables

        private IGridManager gm_terrain;
        private List<ITerrain> il_terrain;
        private IGridManager gm_forest;
        private List<IForest> il_forest;
        private IGridManager gm_grass;
        private List<IGrass> il_grass;
        private IGridManager gm_brush;
        private List<IBrush> il_brush;
        private IGridManager gm_particle;
        private List<IParticleArea> il_particle;

        #endregion

        public SceneImpl(
                IAvatar avatar,
                IGame game,
                IParticles particles,
                IShaders shaders,
                ISky sky,
                IText text,
                ITextures textures,
                IWater water) {
            this.avatar = avatar;
            this.game = game;
            this.particles = particles;
            this.shaders = shaders;
            this.sky = sky;
            this.text = text;
            this.textures = textures;
            this.water = water;
        }

        public void Init() {
            // Do nothing
        }

        public void Render() {
            if (!this.game.IsRunning)
                return;
            if (!this.properties.RenderTextured)
                GL.Disable(EnableCap.Texture2D);
            else
                GL.Enable(EnableCap.Texture2D);
            this.sky.Render();
            GL.Disable(EnableCap.CullFace);
            this.shaders.SelectShader(VShaderTypes.Trees);
            this.shaders.SelectShader(FShaderTypes.Green);
            GL.Color3(1, 1, 1);
            GL.Disable(EnableCap.Texture2D);
            gm_forest.Render();
            GL.Enable(EnableCap.CullFace);
            this.shaders.SelectShader(VShaderTypes.Normal);
            GL.Color3(1, 1, 1);
            gm_terrain.Render();
            this.water.Render();
            GL.BindTexture(TextureTarget.Texture2D, this.textures.TextureIdFromName("grass3.png"));
            this.shaders.SelectShader(VShaderTypes.Grass);
            GL.ColorMask(false, false, false, false);
            gm_grass.Render();
            gm_brush.Render();
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            gm_grass.Render();
            gm_brush.Render();
            this.shaders.SelectShader(VShaderTypes.Normal);
            this.avatar.Render();
            this.shaders.SelectShader(FShaderTypes.None);
            this.shaders.SelectShader(VShaderTypes.None);
            this.particles.Render();
            gm_particle.Render();
        }

        public void RenderDebug() {
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Color3(1, 1, 1);
            gm_forest.Render();
            gm_terrain.Render();
            gm_grass.Render();
            gm_brush.Render();
            GL.Color3(1, 1, 1);
            this.avatar.Render();
            this.water.Render();
        }

        private byte updateType = 0;
        public void Update(double stopAt) {
            if (!this.game.IsRunning)
                return;
            //We don't want any grid to starve the others, so we rotate the order of priority.
            updateType = (byte)((updateType + 1) % 4);
            switch (updateType) {
                case 0:
                    gm_terrain.Update(stopAt);
                    break;
                case 1:
                    gm_grass.Update(stopAt);
                    break;
                case 2:
                    gm_forest.Update(stopAt);
                    break;
                case 3:
                    gm_brush.Update(stopAt);
                    break;
            }
            //any time left over goes to the losers...
            gm_particle.Update(stopAt);
            gm_terrain.Update(stopAt);
            gm_grass.Update(stopAt);
            gm_forest.Update(stopAt);
            gm_brush.Update(stopAt);
            this.text.Print(string.Format( "Scene: {0} of {0} terrains ready", gm_terrain.ItemsReadyCount, gm_terrain.ItemsViewableCount));
        }
    }
}

/*

static int              cached;
static int              texture_bytes;
static int              texture_bytes_counter;
static int              polygons;
static int              polygons_counter;



void SceneClear() {

    il_grass.clear();
    il_brush.clear();
    il_forest.clear();
    il_terrain.clear();
    gm_grass.Clear();
    gm_brush.Clear();
    gm_forest.Clear();
    gm_terrain.Clear();

}

void SceneGenerate() {

    GLvector camera;
    GLcoord current;

    SceneClear();
    WaterBuild();
    camera = AvatarPosition();
    current.x = (int)(camera.x) / GRASS_SIZE;

    il_grass.clear();
    il_grass.resize(GRASS_GRID * GRASS_GRID);
    gm_grass.Init(&il_grass[0], GRASS_GRID, GRASS_SIZE);

    il_forest.clear();
    il_forest.resize(FOREST_GRID * FOREST_GRID);
    gm_forest.Init(&il_forest[0], FOREST_GRID, FOREST_SIZE);

    il_terrain.clear();
    il_terrain.resize(TERRAIN_GRID * TERRAIN_GRID);
    gm_terrain.Init(&il_terrain[0], TERRAIN_GRID, TERRAIN_SIZE);

    il_brush.clear();
    il_brush.resize(BRUSH_GRID * BRUSH_GRID);
    gm_brush.Init(&il_brush[0], BRUSH_GRID, BRUSH_SIZE);

}


void SceneTexturePurge() {

    SceneClear();
    il_grass.clear();
    il_grass.resize(GRASS_GRID * GRASS_GRID);
    gm_grass.Init(&il_grass[0], GRASS_GRID, GRASS_SIZE);

    il_forest.clear();
    il_forest.resize(FOREST_GRID * FOREST_GRID);
    gm_forest.Init(&il_forest[0], FOREST_GRID, FOREST_SIZE);

    il_terrain.clear();
    il_terrain.resize(TERRAIN_GRID * TERRAIN_GRID);
    gm_terrain.Init(&il_terrain[0], TERRAIN_GRID, TERRAIN_SIZE);

    il_brush.clear();
    il_brush.resize(BRUSH_GRID * BRUSH_GRID);
    gm_brush.Init(&il_brush[0], BRUSH_GRID, BRUSH_SIZE);

    il_particle.clear();
    il_particle.resize(PARTICLE_GRID * PARTICLE_GRID);
    gm_particle.Init(&il_particle[0], PARTICLE_GRID, PARTICLE_AREA_SIZE);

}

CTerrain* SceneTerrainGet(int x, int y) {

    uint i;
    GLcoord gp;

    for (i = 0; i < il_terrain.size(); i++) {
        gp = il_terrain[i].GridPosition();
        if (gp.x == x && gp.y == y)
            return &il_terrain[i];
    }
    return NULL;

}

//This is called to restart the terrain grid manager. After the terrains are built,
//we need to pass over them again so they can do their stitching.
void SceneRestartProgress() {

    gm_terrain.RestartProgress();

}


void SceneProgress(uint* ready, uint* total) {

    *ready = gm_terrain.ItemsReady();
    *total = min(gm_terrain.ItemsViewable(), 3);

}
*/