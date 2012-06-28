/*-----------------------------------------------------------------------------
  Avatar.cs
  Handles movement and player input.
-----------------------------------------------------------------------------*/

using System;

using OpenTK;
using OpenTK.Graphics;

namespace Frontier {
	class Avatar : CFigure {
		public enum AnimType { Idle, Run, Sprint, Flying, Fall, Jump, Swim, Float };

		#region Member field, constants and properties
		private const float
			JUMP_SPEED		= 4.0f,
			MOVE_SPEED		= 5.5f,
			SLOW_SPEED		= (MOVE_SPEED * 0.15f),
			SPRINT_SPEED	= 8.0f,
			EYE_HEIGHT		= 1.75f,
			STOP_SPEED    = 0.02f,
			SWIM_DEPTH    = 1.4f,
			ACCEL         = 0.66f,
			DECEL         = 1.5f;
		private const int
			CAM_MIN				= 1,
			CAM_MAX       = 12;

		private readonly string[] ANIM_NAMES = {
			"Idle",					"Running",
			"Sprinting",		"Flying",
			"Falling",			"Jumping",
			"Swimming",			"Floating"};

		private Vector3         angle;
		private Vector3         avatar_facing;
		private Vector2         current_movement;
		private Vector2         desired_movement;
		private float           cam_distance;
		private float           desired_cam_distance;
		private bool            on_ground;
		private bool            swimming;
		private bool            sprinting;
		private uint            last_update;
		private Region           region;
		private CFigure          avatar;
		private CAnim[]          anim = new CAnim[ANIM_COUNT];
		private float            distance_walked;
		private float            last_time;
		private float            current_speed;
		private float            current_angle;
		private float            velocity;
		private ParticleSet      dust_particle;
		private float            last_step_tracking;

		public AnimType AvatarAnim { get; private set; }

		private Vector3 mPosition;
		public Vector3 Position { get { return mPosition; } }

		public Vector3 AvatarCameraPosition { get; private set; }
		private Vector3 AvatarCameraAngle { get; private set; }
		#endregion

		#region Private methods
		private void do_model() {
			avatar.LoadX("models//male.x");
			if (CVarUtils.GetCVar<bool>("avatar.expand")) {
				avatar.BoneInflate(BoneId.Pelvis, 0.02f, true);
				avatar.BoneInflate(BoneId.Head, 0.025f, true);
				avatar.BoneInflate(BoneId.LWrist, 0.03f, true);
				avatar.BoneInflate(BoneId.RWrist, 0.03f, true);
				avatar.BoneInflate(BoneId.RAnkle, 0.05f, true);
				avatar.BoneInflate(BoneId.LAnkle, 0.05f, true);
			}
		}

		private void do_move(Vector3 delta) {
			Vector3    movement;
			float       forward;

			if (CVarUtils.GetCVar<bool>("flying")) {
				forward = Math.Sin(angle.X * DEGREES_TO_RADIANS);
				movement.X = Math.Cos(angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Sin(angle.Z * DEGREES_TO_RADIANS) * delta.Y * forward;
				movement.Y = -Math.Sin(angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Cos(angle.Z * DEGREES_TO_RADIANS) * delta.Y * forward;
				movement.Z = Math.Cos(angle.X * DEGREES_TO_RADIANS) * delta.Y;
				mPosition += movement;
			} else {
				desired_movement.X += Math.Cos(angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Sin(angle.Z * DEGREES_TO_RADIANS) * delta.Y;
				desired_movement.Y += -Math.Sin(angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Cos(angle.Z * DEGREES_TO_RADIANS) * delta.Y;
			}
		}

		private void do_camera() {
			Vector3  cam;
			float     vert_delta;
			float     horz_delta;
			float     ground;
			Vector2 rads;

			rads.X = angle.X * DEGREES_TO_RADIANS;
			vert_delta = (float) (Math.Cos(rads.X) * cam_distance);
			horz_delta = (float) Math.Sin(rads.X);

			cam = Position;
			cam.Z += EYE_HEIGHT;

			cam.X += Math.Sin(angle.Z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
			cam.Y += Math.Cos(angle.Z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
			cam.Z += vert_delta;

			ground = CacheElevation(cam.X, cam.Y) + 0.2f;
			cam.Z = Math.Max(cam.Z, ground);
			AvatarCameraAngle = angle;
			AvatarCameraPosition = cam;
		}

		private void do_location() {
			//ostringstream   oss(ostringstream.in);

			//oss << APP << " ";
			////oss << WorldLocationName (region.grid_pos.x, region.grid_pos.y) << " (" << region.title << ") ";
			//oss << WorldLocationName ((int)Position.X, (int)Position.Y) << " (" << region.title << ") ";
			//oss << "Looking " << WorldDirectionFromAngle (angle.z);
			//SdlSetCaption (oss.str ().c_str ());
		}

		private void AvatarUpdate() {
			float     ground;
			float     water;
			float     elapsed;
			float     movement_animation;
			float     time_passed;
			Vector3   old;
			bool      flying;
			bool      moving;
			float     max_speed;
			float     min_speed;
			float     desired_angle;
			float     lean_angle;
			float     angle_adjust;
			float     step_tracking;

			if (!GameRunning())
				return;

			if (InputKeyState(SDLK_LCTRL))
				AvatarLook(0, 1);

			flying = CVarUtils.GetCVar<bool>("flying");
			elapsed = SdlElapsedSeconds();
			elapsed = Math.Min(elapsed, 0.25f);
			old = Position;
			desired_movement = Vector2.Zero;

			if (InputKeyPressed(SDLK_SPACE) && on_ground) {
				velocity = JUMP_SPEED;
				on_ground = false;
			}

			if (InputKeyPressed(SDLK_F2))
				CVarUtils.SetCVar("flying", !CVarUtils.GetCVar<bool>("flying"));

			//Joystick movement
			AvatarLook((int) (InputJoystickGet(3) * 5.0f), (int) (InputJoystickGet(4) * -5.0f));
			do_move(Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));

			if (InputMouselook()) {
				if (InputKeyPressed(INPUT_MWHEEL_UP))			desired_cam_distance -= 1.0f;
				if (InputKeyPressed(INPUT_MWHEEL_DOWN))		desired_cam_distance += 1.0f;
				if (InputKeyState(SDLK_w))								do_move(-Vector3.UnitY);
				if (InputKeyState(SDLK_s))								do_move(Vector3.UnitY);
				if (InputKeyState(SDLK_a))								do_move(-Vector3.UnitX);
				if (InputKeyState(SDLK_d))								do_move(Vector3.UnitX);

				do_move(new Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));
			}

			//Figure out our speed
			max_speed = MOVE_SPEED;
			min_speed = 0.0f;
			moving = desired_movement.Length > 0.0f; //"moving" means, "trying to move". (Pressing buttons.)

			if (moving)
				min_speed = MOVE_SPEED * 0.33f;

			if (InputKeyState(SDLK_LSHIFT)) {
				sprinting = true;
				max_speed = SPRINT_SPEED;
			} else
				sprinting = false;
			desired_angle = current_angle;

			if (moving) { // We're trying to accelerate
				desired_angle = MathAngle(0.0f, 0.0f, desired_movement.x, desired_movement.y);
				current_speed += elapsed * MOVE_SPEED * ACCEL;
			} else //We've stopped pushing forward
				current_speed -= elapsed * MOVE_SPEED * DECEL;
			current_speed = clamp(current_speed, min_speed, max_speed);

			// Now figure out the angle of movement
			angle_adjust = MathAngleDifference(current_angle, desired_angle);
			// If we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
			lean_angle = 0.0f;

			if (Math.Abs(angle_adjust) > 135)
				current_speed = SLOW_SPEED;

			if (Math.Abs(angle_adjust) < 1.0f || current_speed <= SLOW_SPEED) {
				current_angle = desired_angle;
				angle_adjust = 0.0f;
			} else {
				if (Math.Abs(angle_adjust) < 135) {
					current_angle -= angle_adjust * elapsed * 2.0f;
					lean_angle = clamp(angle_adjust / 4.0f, -15, 15);
				}
			}

			current_movement.X = -Math.Sin(current_angle * DEGREES_TO_RADIANS);
			current_movement.Y = -Math.Cos(current_angle * DEGREES_TO_RADIANS);

			// Apply the movement
			current_movement *= current_speed * elapsed;
			mPosition.X += current_movement.X;
			mPosition.Y += current_movement.Y;
			desired_cam_distance = clamp(desired_cam_distance, CAM_MIN, CAM_MAX);
			cam_distance = MathInterpolate(cam_distance, desired_cam_distance, elapsed);
			ground = CacheElevation(mPosition.X, mPosition.Y);
			water = WorldWaterLevel((int) mPosition.X, (int) mPosition.Y);
			avatar_facing.Y = MathInterpolate(avatar_facing.Y, lean_angle, elapsed);

			if (!flying) {
				velocity -= GRAVITY * elapsed;
				mPosition.Z += velocity * elapsed;
				if (mPosition.Z <= ground) {
					on_ground = true;
					swimming = false;
					mPosition.Z = ground;
					velocity = 0.0f;
				} else if (mPosition.Z > ground + GRAVITY * 0.1f)
					on_ground = false;
				if (mPosition.Z + SWIM_DEPTH < water) {
					swimming = true;
					velocity = 0.0f;
				}
			}

			movement_animation = distance_walked / 4.0f;

			if (on_ground)
				distance_walked += current_speed * elapsed;
		
			if (current_movement.X != 0.0f && current_movement.Y != 0.0f)
				avatar_facing.Z = -MathAngle(0.0f, 0.0f, current_movement.X, current_movement.Y);
			
			if (flying)
				AvatarAnim = Flying;
			else if (swimming) {
				if (current_speed == 0.0f)
					AvatarAnim = Float;
				else
					AvatarAnim = Swim;
			} else if (!on_ground) {
				if (velocity > 0.0f)
					AvatarAnim = Jump;
				else
					AvatarAnim = Fall;
			} else if (current_speed == 0.0f)
				AvatarAnim = Idle;
			else if (sprinting)
				AvatarAnim = Sprint;
			else
				AvatarAnim = Run;
			
			avatar.Animate(anim[(int) AvatarAnim], movement_animation);
			avatar.Position = Position;
			avatar.RotationSet(avatar_facing);
			avatar.Update();
			step_tracking = fmod(movement_animation, 1.0f);

			if (AvatarAnim == AnimType.Run || AvatarAnim == AnimType.Sprint) {
				if (step_tracking < last_step_tracking || (step_tracking > 0.5f && last_step_tracking < 0.5f)) {
					dust_particle.colors.clear();
					if (Position.Z < 0.0f)
						dust_particle.colors.push_back(new Color4(0.4f, 0.7f, 1.0f, 1.0f));
					else
						dust_particle.colors.push_back(CacheSurfaceColor((int) Position.X, (int) Position.Y));
					ParticleAdd(dust_particle, Position);
				}
			}
			last_step_tracking = step_tracking;
			time_passed = GameTime() - last_time;
			last_time = GameTime();
			TextPrint("%s elapsed: %f", anim_names[AvatarAnim], elapsed);
			region = WorldRegionGet((int) (Position.X + REGION_HALF) / REGION_SIZE, (int) (Position.Y + REGION_HALF) / REGION_SIZE);
			do_camera();
			do_location();
		}

		private void AvatarInit() {
			desired_cam_distance = IniFloat("Avatar", "CameraDistance");
			do_model();
			for (int i = 0; i < ANIM_COUNT; i++) {
				anim[i].LoadBvh(IniString("Animations", anim_names[i]));
				IniStringSet("Animations", anim_names[i], IniString("Animations", anim_names[i]));
			}
			ParticleLoad("step", &dust_particle);
		}

		private void AvatarLook(int x, int y) {
			float   mouse_sense;

			if (CVarUtils.GetCVar<bool>("mouse.invert"))
				x *= -1;
			mouse_sense = CVarUtils.GetCVar<float>("mouse.sensitivity");
			angle.X -= (float) x * mouse_sense;
			angle.Z += (float) y * mouse_sense;
			angle.X = clamp(angle.x, 0.0f, 180.0f);
			angle.Z = fmod(angle.z, 360.0f);
			if (angle.Z < 0.0f)
				angle.Z += 360.0f;
		}

		private void PositionSet(Vector3 new_pos) {
			new_pos.Z = clamp(new_pos.z, -25, 2048);
			new_pos.X = clamp(new_pos.x, 0, (REGION_SIZE * WORLD_GRID));
			new_pos.Y = clamp(new_pos.y, 0, (REGION_SIZE * WORLD_GRID));
			mPosition = new_pos;
			AvatarCameraPosition = mPosition;
			angle = AvatarCameraAngle = Vector3(90.0f, 0.0f, 0.0f);
			last_time = GameTime();
			do_model();
		}

		private void AvatarRender() {
			glBindTexture(GL_TEXTURE_2D, TextureIdFromName("avatar.png"));
			//glBindTexture (GL_TEXTURE_2D, 0);
			avatar.Render();
			if (CVarUtils.GetCVar<bool>("show.skeleton"))
				avatar.RenderSkeleton();
		}
		#endregion
	}
}