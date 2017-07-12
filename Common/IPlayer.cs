namespace FrontierSharp.Common {
    using OpenTK;

    public interface IPlayer : IModule {
        Vector3 Position { get; set; }

        void Reset();
    }
}

/* From Player.h
void      PlayerLoad ();
void      PlayerSave ();
 */
