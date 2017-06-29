namespace FrontierSharp.Interfaces {
    using OpenTK;

    using Property;
    using Region;

    public interface IAvatar : IHasProperties, IModule {
        Vector3 CameraPosition { get; }
        Vector3 CameraAngle { get; }
        IRegion Region { get; }
    }


}

/* From Avatar.h
enum AnimType
{
  ANIM_IDLE,
  ANIM_RUN,
  ANIM_SPRINT,
  ANIM_FLYING,
  ANIM_FALL,
  ANIM_JUMP,
  ANIM_SWIM,
  ANIM_FLOAT,
  ANIM_COUNT
};

AnimType  AvatarAnim ();
GLvector  AvatarCameraAngle ();
GLvector  AvatarCameraPosition ();
void      AvatarInit (void);
void      AvatarLook (int x, int y);
GLvector  AvatarPosition ();
void      AvatarPositionSet (GLvector new_pos);
void*     AvatarRegion ();
void      AvatarRender ();
void      AvatarUpdate ();

*/
