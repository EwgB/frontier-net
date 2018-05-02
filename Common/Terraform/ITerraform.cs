namespace FrontierSharp.Common.Terraform {
    using Grid;
    using Util;

    /// <summary>
    /// A set of worker functions for IWorld.  Really, everything
    /// here could go in IWorld/WorldImpl, except that it would be just too
    /// damn big and unmanageable.
    /// 
    /// Still, this system isn't connected to anything else and it's only used
    /// when IWorld is generating region data.
    /// </summary>
    public interface ITerraform : IModule {
        
        /// <summary>
        /// Blur the region attributes by averaging each region with its neighbors.
        /// This prevents overly harsh transitions.
        /// </summary>
        void Average();

        /// <summary> Find existing ocean regions and place costal regions beside them. </summary>
        void Coast();

        /// <summary> Determine the grass, dirt, rock, and other colors used by this region. </summary>
        void Colors();

        /// <summary> Pass over the map, calculate the temperature & moisture. </summary>
        void Climate();

        /// <summary> This will fill in all previously un-assigned regions. </summary>
        void Fill();

        /// <summary> Figure out what plant life should grow here. </summary>
        void Flora();

        /// <summary> Search around for places to put lakes. </summary>
        /// <param name="count">Number of lakes to generate</param>
        void Lakes(int count);

        /// <summary> Indentify regions where geo_scale is negative. These will be ocean. </summary>
        void Oceans();

        void Prepare();

        /// <summary> Drop a point in the middle of the terrain and attempt to place a river. </summary>
        /// <param name="count">Number of rivers to generate</param>
        void Rivers(int count);

        /// <summary> Create zones of different climates. </summary>
        void Zones();

        Color3 ColorGenerate(SurfaceColors c, float moisture, float temperature, int seed);
    }
}
