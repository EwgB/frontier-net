namespace FrontierSharp.Common {
    using OpenTK;

    /// <summary>Handles the character stats. Hitpoints, energy, etc.</summary>
    public interface IPlayer : IModule {
        Vector3 Position { get; set; }

        void Reset();
    }
}

/* From Player.h
void      PlayerLoad ();
void      PlayerSave ();
 */
