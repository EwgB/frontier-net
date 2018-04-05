namespace FrontierSharp.Common.Terraform {
    /// <summary>
    /// A set of worker functions for IWorld.  Really, everything
    /// here could go in IWorld/WorldImpl, except that it would be just too
    /// damn big and unmanageable.
    /// 
    /// Still, this system isn't connected to anything else and it's only used
    /// when IWorld is generating region data.
    /// </summary>
    public interface ITerraform : IModule {
    }
}
