/*-----------------------------------------------------------------------------

  Avatar.cpp


-------------------------------------------------------------------------------

  Handles movement and player input.
 
-----------------------------------------------------------------------------*/


#include "stdafx.h"
#include "cache.h"
#include "camera.h"
#include "cfigure.h"
#include "ini.h"
#include "input.h"
#include "math.h"
#include "render.h"
#include "sdl.h"
#include "Text.h"
#include "Texture.h"
#include "world.h"

#define JUMP_SPEED      4.0f
#define MOVE_SPEED      4.5f
#define EYE_HEIGHT      1.75f
#define CAM_MIN         1
#define CAM_MAX         12
#define STOP_SPEED      0.02f


enum
{
  ANIM_IDLE,
  ANIM_RUN,
  ANIM_FALL,
  ANIM_COUNT
};

static GLvector         angle;
static GLvector         avatar_facing;
static GLvector         position;
static GLvector2        current_movement;
static GLvector2        desired_movement;
static float            cam_distance;
static float            desired_cam_distance;
static float            velocity;
static bool             fly;
static bool             on_ground;
static unsigned         last_update;
static Region           region;
static CFigure          avatar;
static CAnim            anim[ANIM_COUNT];
static float            distance_walked;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_move (GLvector delta)		
{

  GLvector    movement;
  float       forward;
  
  if (fly) {
    forward = sin (angle.x * DEGREES_TO_RADIANS);
    movement.x = cos (angle.z * DEGREES_TO_RADIANS) * delta.x +  sin (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
    movement.y = -sin (angle.z * DEGREES_TO_RADIANS) * delta.x +  cos (angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
    movement.z = cos (angle.x * DEGREES_TO_RADIANS) * delta.y;
    position += movement;
  } else {
    desired_movement.x += cos (angle.z * DEGREES_TO_RADIANS) * delta.x +  sin (angle.z * DEGREES_TO_RADIANS) * delta.y;
    desired_movement.y += -sin (angle.z * DEGREES_TO_RADIANS) * delta.x +  cos (angle.z * DEGREES_TO_RADIANS) * delta.y;
  }

}

void do_camera ()
{

  GLvector  cam;
  float     vert_delta;
  float     horz_delta;
  float     ground;
  GLvector2 rads;

  
  rads.x = angle.x * DEGREES_TO_RADIANS;
  vert_delta = cos (rads.x) * cam_distance;
  horz_delta = sin (rads.x);


  cam = position;
  cam.z += EYE_HEIGHT;
  
  cam.x += sin (angle.z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
  cam.y += cos (angle.z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
  cam.z += vert_delta;

  ground = CacheElevation (cam.x, cam.y) + 0.2f;
  cam.z = max (cam.z, ground);
  CameraAngleSet (angle);
  CameraPositionSet (cam);

}



/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void AvatarUpdate (void)
{

  float     e;
  float     speed;
  float     elapsed;
  float     steps;
  char*     direction;
  GLvector  old;

  elapsed = SdlElapsedSeconds ();
  elapsed = min (elapsed, 0.25f);
  old = position;
  desired_movement = glVector (0.0f, 0.0f);
  if (InputKeyPressed (SDLK_F2))
    fly = !fly;
  if (InputKeyPressed (SDLK_SPACE) && on_ground) {
    velocity = JUMP_SPEED;
    on_ground = false;
  }
  if (InputMouselook ()) {
    if (InputKeyPressed (INPUT_MWHEEL_UP))
      desired_cam_distance -= 1.0f;
    if (InputKeyPressed (INPUT_MWHEEL_DOWN))
      desired_cam_distance += 1.0f;
    if (InputKeyState (SDLK_w))
      do_move (glVector (0, -1, 0));
    if (InputKeyState (SDLK_s))
      do_move (glVector (0, 1, 0));
    if (InputKeyState (SDLK_a))
      do_move (glVector (-1, 0, 0));
    if (InputKeyState (SDLK_d))
      do_move (glVector (1, 0, 0));
  }
  speed = elapsed * MOVE_SPEED;
  if (fly) 
    velocity = 0.0f;
  else {
    position.z += velocity * elapsed;
    velocity -= elapsed * GRAVITY;
  }
  if (InputKeyState (SDLK_LSHIFT)) {
    if (!fly)
      speed *= 2.5f;
    else 
      speed *= 25;
  }
  desired_movement.Normalize ();
  desired_movement *= speed;
  current_movement = glVectorInterpolate (current_movement, desired_movement, elapsed * 4.0f);
  steps = current_movement.Length ();
  if (desired_movement.x == 0.0f && desired_movement.y == 0.0f && steps < STOP_SPEED) 
    current_movement = glVector (0.0f, 0.0f);
  position.x += current_movement.x;
  position.y += current_movement.y;
  desired_cam_distance = clamp (desired_cam_distance, CAM_MIN, CAM_MAX);
  cam_distance = MathInterpolate (cam_distance, desired_cam_distance, elapsed);
  e = CacheElevation (position.x, position.y);
  if (!fly) {
    if (position.z <= e) {
      on_ground = true;
      position.z = e;
      velocity = 0.0f;
    } else if (position.z > e + GRAVITY * 0.1f)
      on_ground = false;
  }
  if (on_ground)
    distance_walked += steps;
  if (current_movement.x != 0.0f && current_movement.y != 0.0f)
    avatar_facing.z = -MathAngle (0.0f, 0.0f, current_movement.x, current_movement.y) / 2.0f;
  //avatar_facing.x = avatar_facing.z;
  if (steps == 0 || !on_ground)
    avatar.Animate (&anim[ANIM_FALL], velocity);
  else 
    avatar.Animate (&anim[ANIM_RUN], distance_walked / 4.0f);
  avatar.PositionSet (position);
  avatar.RotationSet (avatar_facing);
  avatar.Update ();
  region = WorldRegionGet ((int)(position.x + REGION_HALF) / REGION_SIZE, (int)(position.y + REGION_HALF) / REGION_SIZE);
  direction = WorldDirectionFromAngle (angle.z);

  TextPrint ("%s @%s - Facing %s %f", region.title, WorldLocationName (region.grid_pos.x, region.grid_pos.y), direction, avatar_facing.z);
  //TextPrint ("Temp:%1.1f%c Moisture:%1.0f%%\nGeo Scale: %1.2f Water Level: %1.2f Topography Detail:%1.2f Topography Bias:%1.2f", region.temperature * 100.0f, 186, region.moisture * 100.0f, region.geo_scale, region.geo_water, region.geo_detail, region.geo_bias);
  do_camera ();

}


void AvatarInit (void)		
{

  angle = IniVector ("AvatarAngle");
  position = IniVector ("AvatarPosition");
  desired_cam_distance = IniFloat ("AvatarCameraDistance");
  fly = IniInt ("AvatarFlying") != 0;
  avatar.LoadX ("models//male.x");
  avatar.BoneInflate (BONE_PELVIS, 0.02f, true);
  avatar.BoneInflate (BONE_HEAD, 0.025f, true);
  avatar.BoneInflate (BONE_LWRIST, 0.03f, true);
  avatar.BoneInflate (BONE_RWRIST, 0.03f, true);
  avatar.BoneInflate (BONE_RANKLE, 0.05f, true);
  avatar.BoneInflate (BONE_LANKLE, 0.05f, true);
  anim[ANIM_IDLE].LoadBvh (IniString ("AnimIdle"));
  anim[ANIM_RUN].LoadBvh (IniString ("AnimRun"));
  anim[ANIM_FALL].LoadBvh (IniString ("AnimFall"));
  //anim.LoadBvh ("Anims//walk.bvh");

}

void AvatarTerm (void)		
{

  //just store our most recent position in the ini
  IniVectorSet ("AvatarAngle", angle);
  IniFloatSet ("AvatarCameraDistance", cam_distance);
  IniVectorSet ("AvatarPosition", position);
  IniIntSet ("AvatarFlying", fly ? 1 : 0);
 
}

void AvatarLook (int x, int y)
{

  angle.x += x;
  angle.z += y;
  angle.x = clamp (angle.x, 0.0f, 180.0f);
  angle.z = fmod (angle.z, 360.0f);
  if (angle.z < 0.0f)
    angle.z += 360.0f;


}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector AvatarPosition ()
{

  return position;

}

void AvatarPositionSet (GLvector new_pos)		
{

  new_pos.z = clamp (new_pos.z, -25, 2048);
  new_pos.x = clamp (new_pos.x, 0, (REGION_SIZE * WORLD_GRID));
  new_pos.y = clamp (new_pos.y, 0, (REGION_SIZE * WORLD_GRID));
  position = new_pos;

}

void AvatarRender ()
{

  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("check.bmp"));
  avatar.Render ();
  if (RenderConsole ())
    avatar.RenderSkeleton ();

}