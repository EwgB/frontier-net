namespace FrontierSharp.Scene {
    using System;
    using System.Collections.Generic;

    using Ninject;
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

    internal class SceneImpl : IScene {

        #region Constants
        private const int BRUSH_GRID = 7;
        private const int FOREST_GRID = 7;
        private const int GRASS_GRID = 5;
        private const int TERRAIN_GRID = 9;
        private const int PARTICLE_GRID = 3;
        #endregion

        #region Modules

        private readonly IKernel kernel;

        private readonly IAvatar avatar;

        private IGame game;
        private IGame Game => this.game ?? (this.game = this.kernel.Get<IGame>());

        private IParticles particles;
        private IParticles Particles => this.particles ?? (this.particles = this.kernel.Get<IParticles>());

        private IShaders shaders;
        private IShaders Shaders => this.shaders ?? (this.shaders = this.kernel.Get<IShaders>());

        private ISky sky;
        private ISky Sky => this.sky ?? (this.sky = this.kernel.Get<ISky>());

        private IText text;
        private IText Text => this.text ?? (this.text = this.kernel.Get<IText>());

        private ITextures textures;
        private ITextures Textures => this.textures ?? (this.textures = this.kernel.Get<ITextures>());

        private IWater water;
        private IWater Water => this.water ?? (this.water = this.kernel.Get<IWater>());

        #endregion

        #region Public properties

        public IProperties Properties => this.SceneProperties;
        public ISceneProperties SceneProperties { get; } = new SceneProperties();

        public float VisibleRange => (TERRAIN_GRID / 2f) * GridUtils.TERRAIN_SIZE;

        #endregion

        #region Private properties

        private readonly GridManager gmTerrain;
        private readonly List<IGridData> ilTerrain = new List<IGridData>();
        private readonly GridManager gmForest;
        private readonly List<IGridData> ilForest = new List<IGridData>();
        private readonly GridManager gmGrass;
        private readonly List<IGridData> ilGrass = new List<IGridData>();
        private readonly GridManager gmBrush;
        private readonly List<IGridData> ilBrush = new List<IGridData>();
        private readonly GridManager gmParticle;
        private readonly List<IParticleArea> ilParticle = new List<IParticleArea>();

        #endregion

        public SceneImpl(IKernel kernel, IAvatar avatar) {
            this.avatar = avatar;
            this.kernel = kernel;

            this.gmTerrain = new GridManager(avatar);
            this.gmForest = new GridManager(avatar);
            this.gmGrass = new GridManager(avatar);
            this.gmBrush = new GridManager(avatar);
            this.gmParticle = new GridManager(avatar);
        }

        public void Init() { /* Do nothing */ }

        public void Render() {
            if (!this.Game.IsRunning)
                return;
            if (!this.SceneProperties.RenderTextured)
                GL.Disable(EnableCap.Texture2D);
            else
                GL.Enable(EnableCap.Texture2D);
            this.Sky.Render();
            GL.Disable(EnableCap.CullFace);
            this.Shaders.SelectShader(VShaderTypes.Trees);
            this.Shaders.SelectShader(FShaderTypes.Green);
            GL.Color3(1, 1, 1);
            GL.Disable(EnableCap.Texture2D);
            this.gmForest.Render();
            GL.Enable(EnableCap.CullFace);
            this.Shaders.SelectShader(VShaderTypes.Normal);
            GL.Color3(1, 1, 1);
            this.gmTerrain.Render();
            this.Water.Render();
            GL.BindTexture(TextureTarget.Texture2D, this.Textures.TextureIdFromName("grass3.png"));
            this.Shaders.SelectShader(VShaderTypes.Grass);
            GL.ColorMask(false, false, false, false);
            this.gmGrass.Render();
            this.gmBrush.Render();
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            this.gmGrass.Render();
            this.gmBrush.Render();
            this.Shaders.SelectShader(VShaderTypes.Normal);
            this.avatar.Render();
            this.Shaders.SelectShader(FShaderTypes.None);
            this.Shaders.SelectShader(VShaderTypes.None);
            this.Particles.Render();
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
            this.Water.Render();
        }

        private byte updateType;
        public void Update(double stopAt) {
            if (!this.Game.IsRunning)
                return;
            // We don't want any grid to starve the others, so we rotate the order of priority.
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
            this.Text.Print($"Scene: {this.gmTerrain.ItemsReadyCount} of {this.gmTerrain.ItemsViewableCount} terrains ready");
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
            Clear();
            this.Water.Build();
            //var camera = this.avatar.Position;
            //var current = new Coord((int)(camera.X / GridUtils.GRASS_SIZE), 0);

            this.ilGrass.Clear();
            this.ilGrass.Capacity = GRASS_GRID * GRASS_GRID;
            this.gmGrass.Init(this.ilGrass, GRASS_GRID, GridUtils.GRASS_SIZE);

            this.ilForest.Clear();
            this.ilForest.Capacity = FOREST_GRID * FOREST_GRID;
            this.gmForest.Init(this.ilForest, FOREST_GRID, GridUtils.FOREST_SIZE);

            this.ilTerrain.Clear();
            this.ilTerrain.Capacity = TERRAIN_GRID * TERRAIN_GRID;
            this.gmTerrain.Init(this.ilTerrain, TERRAIN_GRID, GridUtils.TERRAIN_SIZE);

            this.ilBrush.Clear();
            this.ilBrush.Capacity = BRUSH_GRID * BRUSH_GRID;
            this.gmBrush.Init(this.ilBrush, BRUSH_GRID, GridUtils.BRUSH_SIZE);
        }

        public void Progress(out int ready, out int total) {
            ready = this.gmTerrain.ItemsReadyCount;
            total = Math.Min(this.gmTerrain.ItemsViewableCount, 3);
        }

        public void RestartProgress() {
            this.gmTerrain.RestartProgress();
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
    ilGrass.Clear();
    ilGrass.Resize(GRASS_GRID * GRASS_GRID);
    gmGrass.Init(&ilGrass[0], GRASS_GRID, GRASS_SIZE);

    ilForest.Clear();
    ilForest.Resize(FOREST_GRID * FOREST_GRID);
    gmForest.Init(&ilForest[0], FOREST_GRID, FOREST_SIZE);

    ilTerrain.Clear();
    ilTerrain.Resize(TERRAIN_GRID * TERRAIN_GRID);
    gmTerrain.Init(&ilTerrain[0], TERRAIN_GRID, TERRAIN_SIZE);

    ilBrush.Clear();
    ilBrush.Resize(BRUSH_GRID * BRUSH_GRID);
    gmBrush.Init(&ilBrush[0], BRUSH_GRID, BRUSH_SIZE);

    ilParticle.Clear();
    ilParticle.Resize(PARTICLE_GRID * PARTICLE_GRID);
    gm_particle.Init(&ilParticle[0], PARTICLE_GRID, PARTICLE_AREA_SIZE);

}

CTerrain* SceneTerrainGet(int x, int y) {

    int i;
    Coord gp;

    for (i = 0; i < ilTerrain.size(); i++) {
        gp = ilTerrain[i].GridPosition();
        if (gp.x == x && gp.y == y)
            return &ilTerrain[i];
    }
    return NULL;

}
*/