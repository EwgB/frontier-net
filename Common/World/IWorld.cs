namespace FrontierSharp.Common.World {
    using Region;
    using OpenTK;

    using Property;

    ///<summary>
    /// Only one of these is ever instantiated.  This is everything that goes into a "save file".
    /// Using only this, the entire world can be re-created.
    /// </summary>
    public interface IWorld : IHasProperties, IModule {

        uint MapId { get; }
        bool WindFromWest { get; set; }

        // TODO: Look into using Array or ImmutableArray for this data
        float GetWaterLevel(Vector2 coord);
        float GetWaterLevel(float x, float y);

        // TODO: Look into using ImmutableArray for this data
        IRegion GetRegion(int x, int y);

        void Generate(uint seed);
        void Load(uint seed);
        void Save();
    }
}

/* From World.h
{
  unsigned      seed;
  bool          northern_hemisphere;
  unsigned      river_count;
  unsigned      lake_count;
  float         noisef[NOISE_BUFFER];
  unsigned      noisei[NOISE_BUFFER];
  Region        map[WORLD_GRID][WORLD_GRID];
};

Cell WorldCell(int world_x, int world_y);
GLrgba WorldColorGet(int world_x, int world_y, SurfaceColor c);
char* WorldLocationName(int world_x, int world_y);
Region WorldRegionFromPosition(int world_x, int world_y);
Region WorldRegionFromPosition(int world_x, int world_y);
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
class CTree* WorldTree(uint id);
void WorldTexturePurge();
*/
