namespace FrontierSharp.Scene {
    using System.Collections.Generic;

    using Common;
    using Common.Grid;
    using Common.Property;
    using Common.Scene;
    using Common.Terrain;

    public class SceneImpl : IScene {

        #region Constants
        private const int BRUSH_GRID = 7;
        private const int FOREST_GRID = 7;
        private const int GRASS_GRID = 5;
        private const int TERRAIN_GRID = 9;
        private const int PARTICLE_GRID = 3;
        #endregion

        #region Modules

        private IGame game;
        private IText text;

        #endregion

        #region Properties

        private ISceneProperties properties = new SceneProperties();
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

        public SceneImpl(IGame game, IText text) {
            this.game = game;
            this.text = text;
        }

        public void Init() {
            /*
                static uint update_type;

                if (!GameRunning())
                    return;
                //We don't want any grid to starve the others, so we rotate the order of priority.
                update_type++;
                switch (update_type % 4) {
                    case 0: gm_terrain.Update(stop); break;
                    case 1: gm_grass.Update(stop); break;
                    case 2: gm_forest.Update(stop); break;
                    case 3: gm_brush.Update(stop); break;
                }
                //any time left over goes to the losers...
                gm_particle.Update(stop);
                gm_terrain.Update(stop);
                gm_grass.Update(stop);
                gm_forest.Update(stop);
                gm_brush.Update(stop);
                TextPrint("Scene: %d of %d terrains ready", gm_terrain.ItemsReady(), gm_terrain.ItemsViewable());
             */
        }

        public void Render() {
            /*
                 if (!GameRunning())
                    return;
                if (!CVarUtils::GetCVar<bool>("render.textured"))
                    glDisable(GL_TEXTURE_2D);
                else
                    glEnable(GL_TEXTURE_2D);
                SkyRender();
                glDisable(GL_CULL_FACE);
                CgShaderSelect(VSHADER_TREES);
                CgShaderSelect(FSHADER_GREEN);
                glColor3f(1, 1, 1);
                glDisable(GL_TEXTURE_2D);
                gm_forest.Render();
                glEnable(GL_CULL_FACE);
                CgShaderSelect(VSHADER_NORMAL);
                glColor3f(1, 1, 1);
                gm_terrain.Render();
                WaterRender();
                glBindTexture(GL_TEXTURE_2D, TextureIdFromName("grass3.png"));
                CgShaderSelect(VSHADER_GRASS);
                glColorMask(false, false, false, false);
                gm_grass.Render();
                gm_brush.Render();
                glColorMask(true, true, true, true);
                glEnable(GL_BLEND);
                glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                gm_grass.Render();
                gm_brush.Render();
                CgShaderSelect(VSHADER_NORMAL);
                AvatarRender();
                CgShaderSelect(FSHADER_NONE);
                CgShaderSelect(VSHADER_NONE);
                ParticleRender();
                gm_particle.Render();

            */
        }

        public void RenderDebug() {
            /*
            glEnable(GL_BLEND);
            glDisable(GL_TEXTURE_2D);
            glDisable(GL_LIGHTING);
            glBlendFunc(GL_ONE, GL_ONE);
            glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
            glColor3f(1, 1, 1);
            gm_forest.Render();
            gm_terrain.Render();
            gm_grass.Render();
            gm_brush.Render();
            glColor3f(1, 1, 1);
            AvatarRender();
            WaterRender();
             */
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
            this.text.Print(string.Format( "Scene: %d of %d terrains ready", gm_terrain.ItemsReadyCount, gm_terrain.ItemsViewableCount));
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