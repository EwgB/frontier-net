namespace FrontierSharp.Common.World {
    using OpenTK;

    using Grid;
    using Property;
    using Region;
    using Tree;
    using Util;

    ///<summary>
    /// This holds the region grid, which is the main table of information from
    /// which ALL OTHER GEOGRAPHICAL DATA is generated or derived.Note that
    /// the resulting data is not STORED here.Regions are sets of rules and
    /// properties.You crank numbers through them, and it creates the world.
    /// 
    /// This output data is stored and managed elsewhere. (See ICachePage)
    /// 
    /// This also holds tables of random numbers.Basically, everything needed to
    /// re-create the world should be stored here.
    /// 
    /// Only one of these is ever instantiated.  This is everything that goes into a "save file".
    /// Using only this, the entire world can be re-created.
    /// </summary>
    public interface IWorld : IHasProperties, IModule {

        int MapId { get; }
        int Seed { get; }
        bool WindFromWest { get; }

        // TODO: Look into using Array or ImmutableArray for this data
        float GetWaterLevel(Vector2 coord);
        float GetWaterLevel(float x, float y);

        // TODO: Look into using ImmutableArray for this data
        IRegion GetRegion(int x, int y);
        IRegion GetRegionFromPosition(int worldX, int worldY);

        Cell GetCell(int worldX, int worldY);
        Color3 GetColor(int worldX, int worldY, SurfaceColors c);
        ITree GetTree(uint id);

        void Generate(int seed);
        void Load(int seed);
        void Save();
    }
}

/* From World.h
char* WorldLocationName(int world_x, int world_y);
float WorldWaterLevel(int world_x, int world_y);

uint WorldCanopyTree();
char* WorldDirectionFromAngle(float angle);
//char*         WorldDirectory ();
uint WorldMap();
uint WorldNoisei(int index);
float WorldNoisef(int index);
World* WorldPtr();
Region WorldRegionGet(int index_x, int index_y);
void WorldRegionSet(int index_x, int index_y, Region val);
uint WorldTreeType(float moisture, float temperature);
void WorldTexturePurge();
*/
