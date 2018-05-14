﻿namespace FrontierSharp.Common.World {
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
        int TreeCanopy { get; }
        bool NorthernHemisphere { get; }

        float GetNoiseF(int index);
        int GetNoiseI(int index);

        // TODO: Look into using Array or ImmutableArray for this data
        float GetWaterLevel(Vector2 coord);
        float GetWaterLevel(int x, int y);

        // TODO: Look into using ImmutableArray for this data
        Region GetRegion(int x, int y);
        Region GetRegionFromPosition(int worldX, int worldY);
        void SetRegion(int x, int y, Region region);

        Cell GetCell(int worldX, int worldY);
        Color3 GetColor(int worldX, int worldY, SurfaceColor c);
        ITree GetTree(int id);
        int GetTreeType(float moisture, float temperature);

        void Generate(int seed);
        void Load(int seed);
        void Save();
    }
}

/* From World.h
char* WorldLocationName(int world_x, int world_y);
float WorldWaterLevel(int world_x, int world_y);

char* WorldDirectionFromAngle(float angle);
//char*         WorldDirectory ();
World* WorldPtr();
void WorldTexturePurge();
*/
