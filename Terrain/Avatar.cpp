/*-----------------------------------------------------------------------------

  Avatar.cpp


-------------------------------------------------------------------------------

  Handles movement and player input.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include <sstream>
#include "avatar.h"
#include "cache.h"
#include "cfigure.h"
#include "game.h"
#include "ini.h"
#include "input.h"
#include "main.h"
#include "math.h"
#include "render.h"
#include "sdl.h"
#include "Text.h"
#include "Texture.h"
#include "world.h"

#define JUMP_SPEED      4.0f
#define MOVE_SPEED      4.0f
#define SPRINT_SPEED    8.0f
#define EYE_HEIGHT      1.75f
#define CAM_MIN         1
#define CAM_MAX         12
#define STOP_SPEED      0.02f
#define SWIM_DEPTH      1.4f
#define ACCEL           0.33f
#define DECEL           1.0f

static char*      anim_names[] =
{
  "Idle",
  "Running",
  "Sprinting",
  "Flying",
  "Falling",
  "Jumping",
  "Swimming",
  "Floating",
};

static GLvector         camera_position;
static GLvector         camera_angle;
static GLvector         angle;
static GLvector         avatar_facing;
static GLvector         position;
static GLvector2        current_movement;
static GLvector2        desired_movement;
static float            cam_distance;
static float            desired_cam_distance;
static bool             on_ground;
static bool             swimming;
static bool             sprinting;
static unsigned         last_update;
static Region           region;
static CFigure          avatar;
static CAnim            anim[ANIM_COUNT];
static AnimType         anim_id;
static float            distance_walked;
static float            last_time;
static float            current_speed;
static float            current_angle;
static float            velocity;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_model ()
{

  avatar.LoadX ("models//male.x");
  if (CVarUtils::GetCVar<bool> ("avatar.expand")) {
    avatar.BoneInflate (BONE_PELVIS, 0.02f, true);
    avatar.BoneInflate (BONE_HEAD, 0.025f, true);
    avatar.BoneInflate (BONE_LWRIST, 0.03f, true);
    avatar.BoneInflate (BONE_RWRIST, 0.03f, true);
    avatar.BoneInflate (BONE_RANKLE, 0.05f, true);
    avatar.BoneInflate (BONE_LANKLE, 0.05f, true);
  }


}

static void do_move (GLvector delta)		
{

  GLvector    movement;
  float       forward;
  
  if (CVarUtils::GetCVar<bool> ("flying")) {
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
  camera_angle = angle;
  camera_position = cam;

}



void do_location ()
{

  ostringstream   oss(ostringstream::in);

  oss << APP << " ";
  //oss << WorldLocationName (region.grid_pos.x, region.grid_pos.y) << " (" << region.title << ") ";
  oss << WorldLocationName ((int)position.x, (int)position.y) << " (" << region.title << ") ";
  oss << "Looking " << WorldDirectionFromAngle (angle.z);
  SdlSetCaption (oss.str ().c_str ());

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void AvatarUpdate (void)
{

  float     ground;
  float     water;
  float     elapsed;
  float     movement_animation;
  float     time_passed;
  GLvector  old;
  bool      flying;
  bool      moving;
  float     max_speed;
  float     min_speed;
  float     desired_angle;
  float     angle_adjust;

  if (!GameRunning ())
    return;
  flying = CVarUtils::GetCVar<bool> ("flying");
  elapsed = SdlElapsedSeconds ();
  elapsed = min (elapsed, 0.25f);
  old = position;
  desired_movement = glVector (0.0f, 0.0f);
  if (InputKeyPressed (SDLK_SPACE) && on_ground) {
    velocity = JUMP_SPEED;
    on_ground = false;
  }
  if (InputKeyPressed (SDLK_F2)) 
    CVarUtils::SetCVar ("flying", !CVarUtils::GetCVar<bool> ("flying"));  
  //Joystick movement
  AvatarLook ((int)(InputJoystickGet (3) * 5.0f), (int)(InputJoystickGet (4) * -5.0f));
  do_move (glVector (InputJoystickGet (0), InputJoystickGet (1), 0.0f));
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
    do_move (glVector (InputJoystickGet (0), InputJoystickGet (1), 0.0f));
  }
  //Figure out our   speed
  max_speed = MOVE_SPEED;
  min_speed = 0.0f;
  moving = desired_movement.Length () > 0.0f;//"moving" means, "trying to move". (Pressing buttons.)
  if (moving)
    min_speed = MOVE_SPEED * 0.33f;
  if (InputKeyState (SDLK_LSHIFT)) {
    sprinting = true;
    max_speed = SPRINT_SPEED;
  } else 
    sprinting = false;
  if (desired_movement.Length () > 1.0f)
    desired_movement.Normalize ();
  desired_angle = current_angle;
  if (moving && current_speed < max_speed) {//We're moving
    desired_angle = MathAngle (0.0f, 0.0f, desired_movement.x, desired_movement.y);
    current_speed += elapsed * MOVE_SPEED * ACCEL;
  } else
    current_speed -= elapsed * MOVE_SPEED * DECEL;
  current_speed = clamp (current_speed, min_speed, max_speed);
  //Now figure out the angle of movement
  angle_adjust = MathAngleDifference (current_angle, desired_angle);
  if (abs (angle_adjust) < 3.0f || current_speed < MOVE_SPEED * 0.1f) {
    current_angle = desired_angle;
    angle_adjust = 0.0f;
  } else
    current_angle -= angle_adjust * elapsed * 2.0f;
  current_movement.x = -sin (current_angle * DEGREES_TO_RADIANS);
  current_movement.y = -cos (current_angle * DEGREES_TO_RADIANS);
  //Apply the movement
  current_movement *= current_speed * elapsed;
  position.x += current_movement.x;
  position.y += current_movement.y;
  desired_cam_distance = clamp (desired_cam_distance, CAM_MIN, CAM_MAX);
  cam_distance = MathInterpolate (cam_distance, desired_cam_distance, elapsed);
  ground = CacheElevation (position.x, position.y);
  water = WorldWaterLevel ((int)position.x, (int)position.y);

  if (!flying) {
    velocity -= GRAVITY * elapsed;
    position.z += velocity * elapsed;
    if (position.z <= ground) {
      on_ground = true;
      swimming = false;
      position.z = ground;
      velocity = 0.0f;
    } else if (position.z > ground + GRAVITY * 0.1f)
      on_ground = false;
    if (position.z + SWIM_DEPTH < water) {
      swimming = true;
      velocity = 0.0f;
    }
  }
  movement_animation = distance_walked / 4.0f;
  if (on_ground)
    distance_walked += current_speed * elapsed;
  if (current_movement.x != 0.0f && current_movement.y != 0.0f)
    avatar_facing.z = -MathAngle (0.0f, 0.0f, current_movement.x, current_movement.y) / 2.0f;
  if (flying)
    anim_id = ANIM_FLYING;
  else if (swimming) {
    if (current_speed == 0.0f)
      anim_id = ANIM_FLOAT;
    else
      anim_id = ANIM_SWIM;
  } else if (!on_ground) {
  if (velocity > 0.0f) 
      anim_id = ANIM_JUMP;
    else 
      anim_id = ANIM_FALL;
  } else if (current_speed == 0.0f) 
    anim_id = ANIM_IDLE;
  else if (sprinting)
    anim_id = ANIM_SPRINT;
  else
    anim_id = ANIM_RUN;
  avatar.Animate (&anim[anim_id], movement_animation);
  avatar.PositionSet (position);
  avatar.RotationSet (avatar_facing);
  avatar.Update ();
  time_passed = GameTime () - last_time;
  last_time = GameTime ();
  TextPrint ("%s %1.2f", anim_names[anim_id], elapsed);
  region = WorldRegionGet ((int)(position.x + REGION_HALF) / REGION_SIZE, (int)(position.y + REGION_HALF) / REGION_SIZE);
  do_camera ();
  do_location ();

}

AnimType AvatarAnim ()
{
  return anim_id;
}

void AvatarInit (void)		
{

  desired_cam_distance = IniFloat ("Avatar", "CameraDistance");
  do_model ();
  for (int i = 0; i < ANIM_COUNT; i++) {
    anim[i].LoadBvh (IniString ("Animations", anim_names[i]));
    IniStringSet ("Animations", anim_names[i], IniString ("Animations", anim_names[i]));
  }

}

void AvatarLook (int x, int y)
{

  float   mouse_sense;

  if (CVarUtils::GetCVar<bool> ("mouse.invert"))
    x = -x;
  mouse_sense = CVarUtils::GetCVar<float> ("mouse.sensitivity");
  angle.x -= (float)x * mouse_sense;
  angle.z += (float)y * mouse_sense;
  angle.x = clamp (angle.x, 0.0f, 180.0f);
  angle.z = fmod (angle.z, 360.0f);
  if (angle.z < 0.0f)
    angle.z += 360.0f;


}

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
  camera_position = position;
  angle = camera_angle = glVector (90.0f, 0.0f, 0.0f);
  last_time = GameTime ();
  do_model ();

}

GLvector AvatarCameraPosition ()
{

  return camera_position;

}

GLvector AvatarCameraAngle ()
{

  return camera_angle;

}

void* AvatarRegion ()
{
  return (void*)&region;
};

void AvatarRender ()
{

  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("avatar.png"));
  //glBindTexture (GL_TEXTURE_2D, 0);
  avatar.Render ();
  if (CVarUtils::GetCVar<bool> ("show.skeleton"))
    avatar.RenderSkeleton ();

}
