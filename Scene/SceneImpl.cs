namespace FrontierSharp.Scene {
    using System.Collections.Generic;

    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Avatar;
    using Common.Game;
    using Common.Grid;
    using Common.Particles;
    using Common.Property;
    using Common.Scene;
    using Common.Shaders;
    using Common.Textures;
    using System;

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

        public IProperties Properties => this.SceneProperties;
        public ISceneProperties SceneProperties { get; } = new SceneProperties();

        public float VisibleRange => (TERRAIN_GRID / 2f) * GridUtils.TERRAIN_SIZE;

        #endregion

        #region Memeber variables

        private readonly GridManager gmTerrain;
        private readonly List<ITerrain> ilTerrain = new List<ITerrain>();
        private readonly GridManager gmForest;
        private readonly List<IForest> ilForest = new List<IForest>();
        private readonly GridManager gmGrass;
        private readonly List<IGrass> ilGrass;
        private readonly GridManager gmBrush;
        private readonly List<IBrush> ilBrush = new List<IBrush>();
        private readonly GridManager gmParticle;
        private readonly List<IParticleArea> ilParticle = new List<IParticleArea>();

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

            this.gmTerrain = new GridManager(avatar);
            this.gmForest = new GridManager(avatar);
            this.gmGrass = new GridManager(avatar);
            this.gmBrush = new GridManager(avatar);
            this.gmParticle = new GridManager(avatar);
        }

        public void Init() {
            // Do nothing
        }

        public void Render() {
            if (!this.game.IsRunning)
                return;
            if (!this.SceneProperties.RenderTextured)
                GL.Disable(EnableCap.Texture2D);
            else
                GL.Enable(EnableCap.Texture2D);
            this.sky.Render();
            GL.Disable(EnableCap.CullFace);
            this.shaders.SelectShader(VShaderTypes.Trees);
            this.shaders.SelectShader(FShaderTypes.Green);
            GL.Color3(1, 1, 1);
            GL.Disable(EnableCap.Texture2D);
            this.gmForest.Render();
            GL.Enable(EnableCap.CullFace);
            this.shaders.SelectShader(VShaderTypes.Normal);
            GL.Color3(1, 1, 1);
            this.gmTerrain.Render();
            this.water.Render();
            GL.BindTexture(TextureTarget.Texture2D, this.textures.TextureIdFromName("grass3.png"));
            this.shaders.SelectShader(VShaderTypes.Grass);
            GL.ColorMask(false, false, false, false);
            this.gmGrass.Render();
            this.gmBrush.Render();
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            this.gmGrass.Render();
            this.gmBrush.Render();
            this.shaders.SelectShader(VShaderTypes.Normal);
            this.avatar.Render();
            this.shaders.SelectShader(FShaderTypes.None);
            this.shaders.SelectShader(VShaderTypes.None);
            this.particles.Render();
            this.gmParticle.Render();
        }

        public void RenderDebug() {
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Color3(1, 1, 1);
            this.gmForest.Render();
            this.gmTerrain.Render();
            this.gmGrass.Render();
            this.gmBrush.Render();
            GL.Color3(1, 1, 1);
            this.avatar.Render();
            this.water.Render();
        }

        private byte updateType;
        public void Update(double stopAt) {
            if (!this.game.IsRunning)
                return;
            //We don't want any grid to starve the others, so we rotate the order of priority.
            this.updateType = (byte)((this.updateType + 1) % 4);
            switch (this.updateType) {
                case 0:
                    this.gmTerrain.Update(stopAt);
                    break;
                case 1:
                    this.gmGrass.Update(stopAt);
                    break;
                case 2:
                    this.gmForest.Update(stopAt);
                    break;
                case 3:
                    this.gmBrush.Update(stopAt);
                    break;
            }
            //any time left over goes to the losers...
            this.gmParticle.Update(stopAt);
            this.gmTerrain.Update(stopAt);
            this.gmGrass.Update(stopAt);
            this.gmForest.Update(stopAt);
            this.gmBrush.Update(stopAt);
            this.text.Print($"Scene: {this.gmTerrain.ItemsReadyCount} of {this.gmTerrain.ItemsViewableCount} terrains ready");
        }

        public void Clear() {
            this.ilGrass.Clear();
            this.ilBrush.Clear();
            this.ilForest.Clear();
            this.ilTerrain.Clear();
            this.gmGrass.Clear();
            this.gmBrush.Clear();
            this.gmForest.Clear();
            this.gmTerrain.Clear();
        }

        public void Generate() {
            /* TODO
            Vector3 camera;
            Coord current;

            SceneClear();
            WaterBuild();
            camera = AvatarPosition();
            current.x = (int)(camera.x) / GRASS_SIZE;

            ilGrass.clear();
            ilGrass.resize(GRASS_GRID * GRASS_GRID);
            gmGrass.Init(ilGrass[0], GRASS_GRID, GRASS_SIZE);

            ilForest.clear();
            ilForest.resize(FOREST_GRID * FOREST_GRID);
            gmForest.Init(ilForest[0], FOREST_GRID, FOREST_SIZE);

            ilTerrain.clear();
            ilTerrain.resize(TERRAIN_GRID * TERRAIN_GRID);
            gmTerrain.Init(ilTerrain[0], TERRAIN_GRID, TERRAIN_SIZE);

            ilBrush.clear();
            ilBrush.resize(BRUSH_GRID * BRUSH_GRID);
            gmBrush.Init(ilBrush[0], BRUSH_GRID, BRUSH_SIZE);
            */
        }

        public void Progress(out int ready, out int total) {
            ready = this.gmTerrain.ItemsReadyCount;
            total = Math.Min(this.gmTerrain.ItemsViewableCount, 3);
        }

        public void RestartProgress() {
            // TODO
            //gmTerrain.RestartProgress();
        }
    }
}

/*

static int              cached;
static int              texture_bytes;
static int              texture_bytes_counter;
static int              polygons;
static int              polygons_counter;


void SceneTexturePurge() {

    SceneClear();
    ilGrass.clear();
    ilGrass.resize(GRASS_GRID * GRASS_GRID);
    gmGrass.Init(&ilGrass[0], GRASS_GRID, GRASS_SIZE);

    ilForest.clear();
    ilForest.resize(FOREST_GRID * FOREST_GRID);
    gmForest.Init(&ilForest[0], FOREST_GRID, FOREST_SIZE);

    ilTerrain.clear();
    ilTerrain.resize(TERRAIN_GRID * TERRAIN_GRID);
    gmTerrain.Init(&ilTerrain[0], TERRAIN_GRID, TERRAIN_SIZE);

    ilBrush.clear();
    ilBrush.resize(BRUSH_GRID * BRUSH_GRID);
    gmBrush.Init(&ilBrush[0], BRUSH_GRID, BRUSH_SIZE);

    ilParticle.clear();
    ilParticle.resize(PARTICLE_GRID * PARTICLE_GRID);
    gm_particle.Init(&ilParticle[0], PARTICLE_GRID, PARTICLE_AREA_SIZE);

}

CTerrain* SceneTerrainGet(int x, int y) {

    uint i;
    Coord gp;

    for (i = 0; i < ilTerrain.size(); i++) {
        gp = ilTerrain[i].GridPosition();
        if (gp.x == x && gp.y == y)
            return &ilTerrain[i];
    }
    return NULL;

}
*/