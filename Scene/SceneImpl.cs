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
        private IGame Game => game ?? (game = kernel.Get<IGame>());

        private IParticles particles;
        private IParticles Particles => particles ?? (particles = kernel.Get<IParticles>());

        private IShaders shaders;
        private IShaders Shaders => shaders ?? (shaders = kernel.Get<IShaders>());

        private ISky sky;
        private ISky Sky => sky ?? (sky = kernel.Get<ISky>());

        private IText text;
        private IText Text => text ?? (text = kernel.Get<IText>());

        private ITextures textures;
        private ITextures Textures => textures ?? (textures = kernel.Get<ITextures>());

        private IWater water;
        private IWater Water => water ?? (water = kernel.Get<IWater>());

        #endregion

        #region Public properties

        public IProperties Properties => SceneProperties;
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

            gmTerrain = new GridManager(avatar);
            gmForest = new GridManager(avatar);
            gmGrass = new GridManager(avatar);
            gmBrush = new GridManager(avatar);
            gmParticle = new GridManager(avatar);
        }

        public void Init() { /* Do nothing */ }

        public void Render() {
            if (!Game.IsRunning)
                return;
            if (!SceneProperties.RenderTextured)
                GL.Disable(EnableCap.Texture2D);
            else
                GL.Enable(EnableCap.Texture2D);
            Sky.Render();
            GL.Disable(EnableCap.CullFace);
            Shaders.SelectShader(VShaderTypes.Trees);
            Shaders.SelectShader(FShaderTypes.Green);
            GL.Color3(1, 1, 1);
            GL.Disable(EnableCap.Texture2D);
            gmForest.Render();
            GL.Enable(EnableCap.CullFace);
            Shaders.SelectShader(VShaderTypes.Normal);
            GL.Color3(1, 1, 1);
            gmTerrain.Render();
            Water.Render();
            GL.BindTexture(TextureTarget.Texture2D, Textures.TextureIdFromName("grass3.png"));
            Shaders.SelectShader(VShaderTypes.Grass);
            GL.ColorMask(false, false, false, false);
            gmGrass.Render();
            gmBrush.Render();
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            gmGrass.Render();
            gmBrush.Render();
            Shaders.SelectShader(VShaderTypes.Normal);
            avatar.Render();
            Shaders.SelectShader(FShaderTypes.None);
            Shaders.SelectShader(VShaderTypes.None);
            Particles.Render();
            gmParticle.Render();
        }

        public void RenderDebug() {
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Color3(1, 1, 1);
            gmForest.Render();
            gmTerrain.Render();
            gmGrass.Render();
            gmBrush.Render();
            GL.Color3(1, 1, 1);
            avatar.Render();
            Water.Render();
        }

        private byte updateType;
        public void Update(double stopAt) {
            if (!Game.IsRunning)
                return;
            // We don't want any grid to starve the others, so we rotate the order of priority.
            updateType = (byte)((updateType + 1) % 4);
            switch (updateType) {
                case 0:
                    gmTerrain.Update(stopAt);
                    break;
                case 1:
                    gmGrass.Update(stopAt);
                    break;
                case 2:
                    gmForest.Update(stopAt);
                    break;
                case 3:
                    gmBrush.Update(stopAt);
                    break;
            }
            //any time left over goes to the losers...
            gmParticle.Update(stopAt);
            gmTerrain.Update(stopAt);
            gmGrass.Update(stopAt);
            gmForest.Update(stopAt);
            gmBrush.Update(stopAt);
            Text.Print($"Scene: {gmTerrain.ItemsReadyCount} of {gmTerrain.ItemsViewableCount} terrains ready");
        }

        public void Clear() {
            ilGrass.Clear();
            ilBrush.Clear();
            ilForest.Clear();
            ilTerrain.Clear();
            gmGrass.Clear();
            gmBrush.Clear();
            gmForest.Clear();
            gmTerrain.Clear();
        }

        public void Generate() {
            Clear();
            Water.Build();
            //var camera = this.avatar.Position;
            //var current = new Coord((int)(camera.X / GridUtils.GRASS_SIZE), 0);

            ilGrass.Clear();
            ilGrass.Capacity = GRASS_GRID * GRASS_GRID;
            gmGrass.Init(ilGrass, GRASS_GRID, GridUtils.GRASS_SIZE);

            ilForest.Clear();
            ilForest.Capacity = FOREST_GRID * FOREST_GRID;
            gmForest.Init(ilForest, FOREST_GRID, GridUtils.FOREST_SIZE);

            ilTerrain.Clear();
            ilTerrain.Capacity = TERRAIN_GRID * TERRAIN_GRID;
            gmTerrain.Init(ilTerrain, TERRAIN_GRID, GridUtils.TERRAIN_SIZE);

            ilBrush.Clear();
            ilBrush.Capacity = BRUSH_GRID * BRUSH_GRID;
            gmBrush.Init(ilBrush, BRUSH_GRID, GridUtils.BRUSH_SIZE);
        }

        public void Progress(out int ready, out int total) {
            ready = gmTerrain.ItemsReadyCount;
            total = Math.Min(gmTerrain.ItemsViewableCount, 3);
        }

        public void RestartProgress() {
            gmTerrain.RestartProgress();
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