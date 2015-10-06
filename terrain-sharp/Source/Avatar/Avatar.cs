namespace terrain_sharp.Source.Avatar {
	using System;
	using System.Collections.Generic;

	using OpenTK;
	using OpenTK.Graphics;
	using OpenTK.Graphics.OpenGL;

	using CAnim;
	using CFigure;
	using StdAfx;
	using Utils;
	using World;

	///<summary>Handles movement and player input.</summary>
	class Avatar {
		private const float JUMP_SPEED = 4;
		private const float MOVE_SPEED = 5.5f;
		private const float SLOW_SPEED = (MOVE_SPEED * 0.15f);
		private const float SPRINT_SPEED = 8;
		private const float EYE_HEIGHT = 1.75f;
		private const int CAM_MIN = 1;
		private const int CAM_MAX = 12;
		private const float STOP_SPEED = 0.02f;
		private const float SWIM_DEPTH = 1.4f;
		private const float ACCEL = 0.66f;
		private const float DECEL = 1.5f;

		private readonly IDictionary<AnimType, string> anim_names = new Dictionary<AnimType, string>() {
			{AnimType.ANIM_IDLE, "Idle" },
			{AnimType.ANIM_RUN, "Running"},
			{AnimType.ANIM_SPRINT, "Sprinting"},
			{AnimType.ANIM_FLYING, "Flying" },
			{AnimType.ANIM_FALL, "Falling" },
			{AnimType.ANIM_JUMP, "Jumping" },
			{AnimType.ANIM_SWIM, "Swimming" },
			{AnimType.ANIM_FLOAT, "Floating" }
		};

		private static readonly Avatar instance = new Avatar();
		private Avatar() { }
		public static Avatar Instance { get { return instance; } }

		public Vector3 CameraAngle { get; private set; }
		private Vector3 position;
		public Vector3 Position { get { return position; } private set { position = value; } }
		public Region Region { get; private set; }
		public AnimType AnimId { get; private set; }
		public Vector3 CameraPosition { get; private set; }

		private Vector3 angle;
		private Vector3 avatar_facing;
		private Vector2 current_movement;
		private Vector2 desired_movement;
		private float cam_distance;
		private float desired_cam_distance;
		private bool on_ground;
		private bool swimming;
		private bool sprinting;
		private int last_update;
		private CFigure avatar;
		private Dictionary<AnimType, CAnim> anim = new Dictionary<AnimType, CAnim>(Enum.GetValues(typeof(AnimType)).Length);
		private float distance_walked;
		private float last_time;
		private float current_speed;
		private float current_angle;
		private float velocity;
		private ParticleSet dust_particle;
		private float last_step_tracking;

		private void do_model() {
			avatar.LoadX("models//male.X");
			if (CVarUtils.GetCVar<bool>("avatar.expand")) {
				avatar.BoneInflate(BoneId.Pelvis, 0.02f, true);
				avatar.BoneInflate(BoneId.Head, 0.025f, true);
				avatar.BoneInflate(BoneId.LeftWrist, 0.03f, true);
				avatar.BoneInflate(BoneId.RightWrist, 0.03f, true);
				avatar.BoneInflate(BoneId.RightAnkle, 0.05f, true);
				avatar.BoneInflate(BoneId.LeftAnkle, 0.05f, true);
			}
		}

		private void do_move(Vector3 delta) {
			if (CVarUtils.GetCVar<bool>("flying")) {
				float forward = (float) Math.Sin(MathHelper.DegreesToRadians(angle.X));
				var movement = new Vector3(
					(float) (Math.Cos(MathHelper.DegreesToRadians(angle.Z)) * delta.X + Math.Sin(MathHelper.DegreesToRadians(angle.Z)) * delta.Y * forward),
					(float) (-Math.Sin(MathHelper.DegreesToRadians(angle.Z)) * delta.X + Math.Cos(MathHelper.DegreesToRadians(angle.Z)) * delta.Y * forward),
					(float) Math.Cos(MathHelper.DegreesToRadians(angle.X)) * delta.Y);
				Position += movement;
			} else {
				desired_movement.X += (float) (Math.Cos(MathHelper.DegreesToRadians(angle.Z)) * delta.X + Math.Sin(MathHelper.DegreesToRadians(angle.Z)) * delta.Y);
				desired_movement.Y += (float) (-Math.Sin(MathHelper.DegreesToRadians(angle.Z)) * delta.X + Math.Cos(MathHelper.DegreesToRadians(angle.Z)) * delta.Y);
			}
		}

		private void do_camera() {
			float rads = MathHelper.DegreesToRadians(angle.X);
			float vert_delta = (float) Math.Cos(rads) * cam_distance;
			float horz_delta = (float) Math.Sin(rads);

			Vector3 cam = Position;
			cam.Z += EYE_HEIGHT;

			cam.X += (float) Math.Sin(MathHelper.DegreesToRadians(angle.Z)) * cam_distance * horz_delta;
			cam.Y += (float) Math.Cos(MathHelper.DegreesToRadians(angle.Z)) * cam_distance * horz_delta;
			cam.Z += vert_delta;

			float ground = CacheElevation(cam.X, cam.Y) + 0.2f;
			cam.Z = Math.Max(cam.Z, ground);
			CameraAngle = angle;
			CameraPosition = cam;
		}

		private void do_location() {
			//ostringstream oss(ostringstream.in);

			//oss << APP << " ";
			////oss << WorldLocationName (region.grid_pos.X, region.grid_pos.Y) << " (" << region.title << ") ";
			//oss << WorldLocationName((int) Position.X, (int) Position.Y) << " (" << Region.title << ") ";
			//oss << "Looking " << WorldDirectionFromAngle(angle.Z);
			//SdlSetCaption(oss.str().c_str());
		}

		public void Update() {
			if (!GameRunning())
				return;
			if (InputKeyState(SDLK_LCTRL))
				Look(0, 1);
			bool flying = CVarUtils.GetCVar<bool>("flying");
			float elapsed = Math.Min(SdlElapsedSeconds(), 0.25f);
			Vector3 old = Position;
			desired_movement = new Vector2();
			if (InputKeyPressed(SDLK_SPACE) && on_ground) {
				velocity = JUMP_SPEED;
				on_ground = false;
			}
			if (InputKeyPressed(SDLK_F2))
				CVarUtils.SetCVar("flying", !CVarUtils.GetCVar<bool>("flying"));
			//Joystick movement
			Look((int) (InputJoystickGet(3) * 5), (int) (InputJoystickGet(4) * -5));
			do_move(new Vector3(InputJoystickGet(0), InputJoystickGet(1), 0));
			if (InputMouselook()) {
				if (InputKeyPressed(INPUT_MWHEEL_UP))
					desired_cam_distance -= 1;
				if (InputKeyPressed(INPUT_MWHEEL_DOWN))
					desired_cam_distance += 1;
				if (InputKeyState(SDLK_w))
					do_move(new Vector3(0, -1, 0));
				if (InputKeyState(SDLK_s))
					do_move(new Vector3(0, 1, 0));
				if (InputKeyState(SDLK_a))
					do_move(new Vector3(-1, 0, 0));
				if (InputKeyState(SDLK_d))
					do_move(new Vector3(1, 0, 0));
				do_move(new Vector3(InputJoystickGet(0), InputJoystickGet(1), 0));
			}
			//Figure out our   speed
			float max_speed = MOVE_SPEED;
			float min_speed = 0;
			bool moving = desired_movement.Length > 0;//"moving" means, "trying to move". (Pressing buttons.)
			if (moving)
				min_speed = MOVE_SPEED * 0.33f;
			if (InputKeyState(SDLK_LSHIFT)) {
				sprinting = true;
				max_speed = SPRINT_SPEED;
			} else
				sprinting = false;
			float desired_angle = current_angle;
			if (moving) {//We're trying to accelerate
				desired_angle = MathUtils.Angle(0, 0, desired_movement.X, desired_movement.Y);
				current_speed += elapsed * MOVE_SPEED * ACCEL;
			} else //We've stopped pushing forward
				current_speed -= elapsed * MOVE_SPEED * DECEL;
			current_speed = MathHelper.Clamp(current_speed, min_speed, max_speed);
			//Now figure out the angle of movement
			float angle_adjust = MathUtils.AngleDifference(current_angle, desired_angle);
			//if we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
			float lean_angle = 0;
			if (Math.Abs(angle_adjust) > 135)
				current_speed = SLOW_SPEED;
			if (Math.Abs(angle_adjust) < 1 || current_speed <= SLOW_SPEED) {
				current_angle = desired_angle;
				angle_adjust = 0;
			} else {
				if (Math.Abs(angle_adjust) < 135) {
					current_angle -= angle_adjust * elapsed * 2;
					lean_angle = MathHelper.Clamp(angle_adjust / 4, -15, 15);
				}
			}
			current_movement.X = (float) -Math.Sin(MathHelper.DegreesToRadians(current_angle));
			current_movement.Y = (float) -Math.Cos(MathHelper.DegreesToRadians(current_angle));
			//Apply the movement
			current_movement *= current_speed * elapsed;
			position.X += current_movement.X;
			position.Y += current_movement.Y;
			desired_cam_distance = MathHelper.Clamp(desired_cam_distance, CAM_MIN, CAM_MAX);
			cam_distance = MathUtils.Interpolate(cam_distance, desired_cam_distance, elapsed);
			float ground = CacheElevation(Position.X, Position.Y);
			float water = World.Instance.WaterLevel((int) Position.X, (int) Position.Y);
			avatar_facing.Y = MathUtils.Interpolate(avatar_facing.Y, lean_angle, elapsed);
			if (!flying) {
				velocity -= StdAfx.GRAVITY * elapsed;
				position.Z += velocity * elapsed;
				if (Position.Z <= ground) {
					on_ground = true;
					swimming = false;
					position.Z = ground;
					velocity = 0;
				} else if (Position.Z > ground + StdAfx.GRAVITY * 0.1f)
					on_ground = false;
				if (Position.Z + SWIM_DEPTH < water) {
					swimming = true;
					velocity = 0;
				}
			}
			float movement_animation = distance_walked / 4;
			if (on_ground)
				distance_walked += current_speed * elapsed;
			if (current_movement.X != 0 && current_movement.Y != 0)
				avatar_facing.Z = -MathUtils.Angle(0, 0, current_movement.X, current_movement.Y);
			if (flying)
				AnimId = AnimType.ANIM_FLYING;
			else if (swimming) {
				if (current_speed == 0)
					AnimId = AnimType.ANIM_FLOAT;
				else
					AnimId = AnimType.ANIM_SWIM;
			} else if (!on_ground) {
				if (velocity > 0)
					AnimId = AnimType.ANIM_JUMP;
				else
					AnimId = AnimType.ANIM_FALL;
			} else if (current_speed == 0)
				AnimId = AnimType.ANIM_IDLE;
			else if (sprinting)
				AnimId = AnimType.ANIM_SPRINT;
			else
				AnimId = AnimType.ANIM_RUN;
			avatar.Animate(anim[AnimId], movement_animation);
			avatar.Position = Position;
			avatar.SetRotation(avatar_facing);
			avatar.Update();
			float step_tracking = movement_animation % 1;
			if (AnimId == AnimType.ANIM_RUN || AnimId == AnimType.ANIM_SPRINT) {
				if (step_tracking < last_step_tracking || (step_tracking > 0.5f && last_step_tracking < 0.5f)) {
					dust_particle.colors.clear();
					if (Position.Z < 0)
						dust_particle.colors.Add(new Color4(0.4f, 0.7f, 1, 1));
					else
						dust_particle.colors.Add(CacheSurfaceColor((int) Position.X, (int) Position.Y));
					ParticleAdd(dust_particle, Position);
				}
			}
			last_step_tracking = step_tracking;
			float time_passed = GameTime() - last_time;
			last_time = GameTime();
			TextPrint("%s elapsed: %f", anim_names[AnimId], elapsed);
			Region = World.Instance.GetRegion(
				(int) (Position.X + World.REGION_HALF) / World.REGION_SIZE,
				(int) (Position.Y + World.REGION_HALF) / World.REGION_SIZE);
			do_camera();
			do_location();
		}

		public void Init() {
			desired_cam_distance = IniFloat("Avatar", "CameraDistance");
			do_model();
			foreach (AnimType animType in Enum.GetValues(typeof(AnimType))) {
				anim[animType].LoadBvh(IniString("Animations", anim_names[animType]));
				IniStringSet("Animations", anim_names[animType], IniString("Animations", anim_names[animType]));
			}
			ParticleLoad("step", &dust_particle);
		}

		public void Look(int x, int y) {
			if (CVarUtils.GetCVar<bool>("mouse.invert"))
				x = -x;
			float mouse_sense = CVarUtils.GetCVar<float>("mouse.sensitivity");
			angle.X -= x * mouse_sense;
			angle.Z += y * mouse_sense;
			angle.X = MathHelper.Clamp(angle.X, 0, 180);
			angle.Z = angle.Z % 360;
			if (angle.Z < 0)
				angle.Z += 360;
		}

		public void SetPosition(Vector3 new_pos) {
			new_pos.Z = MathHelper.Clamp(new_pos.Z, -25, 2048);
			new_pos.X = MathHelper.Clamp(new_pos.X, 0, (World.REGION_SIZE * World.WORLD_GRID));
			new_pos.Y = MathHelper.Clamp(new_pos.Y, 0, (World.REGION_SIZE * World.WORLD_GRID));
			Position = new_pos;
			CameraPosition = Position;
			angle = CameraAngle = new Vector3(90, 0, 0);
			last_time = GameTime();
			do_model();
		}

		public void Render() {
			GL.BindTexture(TextureTarget.Texture2D, TextureIdFromName("avatar.png"));
			//GL.BindTexture (TextureTarget.Texture2D, 0);
			avatar.Render();
			if (CVarUtils.GetCVar<bool>("show.skeleton"))
				avatar.RenderSkeleton();
		}
	}
}
