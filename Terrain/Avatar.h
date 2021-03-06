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

