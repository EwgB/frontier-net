namespace FrontierSharp.Avatar {
    using System;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Animation;
    using Common.Avatar;
    using Common.Particles;
    using Common.Property;
    using Common.Region;
    using Common.Textures;
    using Common.World;
    using Common.Util;

    public class AvatarImpl : IAvatar {

        #region Constants

        private const float JUMP_SPEED = 4.0f;
        private const float MOVE_SPEED = 5.5f;
        private const float SLOW_SPEED = (MOVE_SPEED * 0.15f);
        private const float SPRINT_SPEED = 8.0f;
        private const float EYE_HEIGHT = 1.75f;
        private const float CAM_MIN = 1;
        private const float CAM_MAX = 12;
        private const float STOP_SPEED = 0.02f;
        private const float SWIM_DEPTH = 1.4f;
        private const float ACCEL = 0.66f;
        private const float DECEL = 1.5f;

        #endregion

        #region Modules

        private IGame game;
        private IParticles particles;
        private IText text;
        private ITextures textures;
        private IWorld world;

        private IFigure avatar;

        #endregion

        #region Properties

        private Vector3 position;
        public Vector3 Position {
            get { return this.position; }
            set {
                this.position.Z = MathHelper.Clamp(value.Z, -25, 2048);
                this.position.X = MathHelper.Clamp(value.X, 0, (WorldUtils.REGION_SIZE * WorldUtils.WORLD_GRID));
                this.position.Y = MathHelper.Clamp(value.Y, 0, (WorldUtils.REGION_SIZE * WorldUtils.WORLD_GRID));
                CameraPosition = this.position;
                this.angle = CameraAngle = new Vector3(90.0f, 0.0f, 0.0f);
                this.lastTime = this.game.Time;
                DoModel();

            }
        }

        public IRegion Region { get; private set; }
        public Vector3 CameraAngle { get; private set; }
        public Vector3 CameraPosition { get; private set; }
        public AnimTypes AnimationType { get; private set; }

        private IAvatarProperties properties = new AvatarProperties();
        public IProperties Properties { get { return this.properties; } }
        public IAvatarProperties AvatarProperties { get { return this.properties; } }

        #endregion

        #region Member variables

        private Vector3 angle;
        private Vector3 avatarFacing;
        private Vector2 currentMovement;
        private Vector2 desiredMovement;
        private float camDistance;
        private float desiredCamDistance;
        private bool onGround;
        private bool swimming;
        private bool sprinting;
        private uint lastUpdate;
        private AnimationTypeArray anim = new AnimationTypeArray();
        private AnimTypes animType;
        private float distanceWalked;
        private float lastTime;
        private float currentSpeed;
        private float currentAngle;
        private float velocity;
        private ParticleSet dustParticle;
        private float lastStepTracking;

        #endregion

        public AvatarImpl(IFigure avatar, IGame game, IParticles particles, IText text, ITextures textures, IWorld world) {
            this.avatar = avatar;
            this.game = game;
            this.particles = particles;
            this.text = text;
            this.textures = textures;
            this.world = world;

            // TODO: Is this the desired behaviour? Where should this be initialised?
            this.Region = this.world.GetRegion(0, 0);
        }

        public void Init() {
            //this.desiredCamDistance = IniFloat("Avatar", "CameraDistance"); // TODO: do we want to add ini options?
            DoModel();
            for (var i = AnimTypes.Idle; i < AnimTypes.Max; i++) {
                /*
                    anim[i].LoadBvh(IniString("Animations", AnimTypes.names[i]));
                    IniStringSet("Animations", AnimTypes.names[i], IniString("Animations", AnimTypes.names[i]));
                */
            }
            this.particles.LoadParticles("step", this.dustParticle);
        }

        public void Update() {
            /*
            float ground;
            float water;
            float elapsed;
            float movement_animation;
            float time_passed;
            Vector3 old;
            bool flying;
            bool moving;
            float max_speed;
            float min_speed;
            float desired_angle;
            float lean_angle;
            float angle_adjust;
            float step_tracking;

            if (!GameRunning())
                return;
            if (InputKeyState(SDLK_LCTRL))
                AvatarLook(0, 1);
            flying = CVarUtils::GetCVar<bool>("flying");
            elapsed = SdlElapsedSeconds();
            elapsed = min(elapsed, 0.25f);
            old = position;
            desired_movement = Vector3(0.0f, 0.0f);
            if (InputKeyPressed(SDLK_SPACE) && on_ground) {
                velocity = JUMP_SPEED;
                on_ground = false;
            }
            if (InputKeyPressed(SDLK_F2))
                CVarUtils::SetCVar("flying", !CVarUtils::GetCVar<bool>("flying"));
            //Joystick movement
            AvatarLook((int)(InputJoystickGet(3) * 5.0f), (int)(InputJoystickGet(4) * -5.0f));
            do_move(Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));
            if (InputMouselook()) {
                if (InputKeyPressed(INPUT_MWHEEL_UP))
                    desiredCamDistance -= 1.0f;
                if (InputKeyPressed(INPUT_MWHEEL_DOWN))
                    desiredCamDistance += 1.0f;
                if (InputKeyState(SDLK_w))
                    do_move(Vector3(0, -1, 0));
                if (InputKeyState(SDLK_s))
                    do_move(Vector3(0, 1, 0));
                if (InputKeyState(SDLK_a))
                    do_move(Vector3(-1, 0, 0));
                if (InputKeyState(SDLK_d))
                    do_move(Vector3(1, 0, 0));
                do_move(Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));
            }
            //Figure out our   speed
            max_speed = MOVE_SPEED;
            min_speed = 0.0f;
            moving = desired_movement.Length() > 0.0f;//"moving" means, "trying to move". (Pressing buttons.)
            if (moving)
                min_speed = MOVE_SPEED * 0.33f;
            if (InputKeyState(SDLK_LSHIFT)) {
                sprinting = true;
                max_speed = SPRINT_SPEED;
            } else
                sprinting = false;
            desired_angle = current_angle;
            if (moving) {//We're trying to accelerate
                desired_angle = MathAngle(0.0f, 0.0f, desired_movement.x, desired_movement.y);
                current_speed += elapsed * MOVE_SPEED * ACCEL;
            } else //We've stopped pushing forward
                current_speed -= elapsed * MOVE_SPEED * DECEL;
            current_speed = clamp(current_speed, min_speed, max_speed);
            //Now figure out the angle of movement
            angle_adjust = MathAngleDifference(current_angle, desired_angle);
            //if we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
            lean_angle = 0.0f;
            if (abs(angle_adjust) > 135)
                current_speed = SLOW_SPEED;
            if (abs(angle_adjust) < 1.0f || current_speed <= SLOW_SPEED) {
                current_angle = desired_angle;
                angle_adjust = 0.0f;
            } else {
                if (abs(angle_adjust) < 135) {
                    current_angle -= angle_adjust * elapsed * 2.0f;
                    lean_angle = clamp(angle_adjust / 4.0f, -15, 15);
                }
            }
            current_movement.x = -sin(current_angle * DEGREES_TO_RADIANS);
            current_movement.y = -cos(current_angle * DEGREES_TO_RADIANS);
            //Apply the movement
            current_movement *= current_speed * elapsed;
            position.x += current_movement.x;
            position.y += current_movement.y;
            desiredCamDistance = clamp(desiredCamDistance, CAM_MIN, CAM_MAX);
            cam_distance = MathInterpolate(cam_distance, desiredCamDistance, elapsed);
            ground = CacheElevation(position.x, position.y);
            water = WorldWaterLevel((int)position.x, (int)position.y);
            avatar_facing.y = MathInterpolate(avatar_facing.y, lean_angle, elapsed);
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
                avatar_facing.z = -MathAngle(0.0f, 0.0f, current_movement.x, current_movement.y);
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
            avatar.Animate(&anim[anim_id], movement_animation);
            avatar.PositionSet(position);
            avatar.RotationSet(avatar_facing);
            avatar.Update();
            step_tracking = fmod(movement_animation, 1.0f);
            if (anim_id == ANIM_RUN || anim_id == ANIM_SPRINT) {
                if (step_tracking < last_step_tracking || (step_tracking > 0.5f && last_step_tracking < 0.5f)) {
                    dust_particle.colors.clear();
                    if (position.z < 0.0f)
                        dust_particle.colors.push_back(glRgba(0.4f, 0.7f, 1.0f));
                    else
                        dust_particle.colors.push_back(CacheSurfaceColor((int)position.x, (int)position.y));
                    ParticleAdd(&dust_particle, position);
                }
            }
            last_step_tracking = step_tracking;
            time_passed = GameTime() - last_time;
            last_time = GameTime();
            TextPrint("%s elapsed: %f", anim_names[anim_id], elapsed);
            */
            this.Region = this.world.GetRegion(
                (int)(this.position.X + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE,
                (int)(this.position.Y + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE);
            /*
            do_camera();
            do_location();
            */
        }

        public void Render() {
            GL.BindTexture(TextureTarget.Texture2D, this.textures.TextureIdFromName("avatar.png"));
            GL.BindTexture(TextureTarget.Texture2D, 0);
            avatar.Render();
            if (this.properties.ShowSkeleton) {
                avatar.RenderSkeleton();
            }
        }

        private void DoModel() {
            // TODO
            //avatar.LoadX("models//male.x");
            //if (CVarUtils::GetCVar<bool>("avatar.expand")) {
            //    avatar.BoneInflate(BONE_PELVIS, 0.02f, true);
            //    avatar.BoneInflate(BONE_HEAD, 0.025f, true);
            //    avatar.BoneInflate(BONE_LWRIST, 0.03f, true);
            //    avatar.BoneInflate(BONE_RWRIST, 0.03f, true);
            //    avatar.BoneInflate(BONE_RANKLE, 0.05f, true);
            //    avatar.BoneInflate(BONE_LANKLE, 0.05f, true);
            //}
        }

        private void DoCamera() {
            // TODO
            //Vector3 cam;
            //float vert_delta;
            //float horz_delta;
            //float ground;
            //Vector2 rads;


            //rads.X = this.angle.X * DEGREES_TO_RADIANS;
            //vert_delta = Math.Cos(rads.X) * this.camDistance;
            //horz_delta = Math.Sin(rads.X);


            //cam = position;
            //cam.Z += EYE_HEIGHT;

            //cam.X += Math.Sin(this.angle.Z * DEGREES_TO_RADIANS) * this.camDistance * horz_delta;
            //cam.Y += Math.Cos(this.angle.Z * DEGREES_TO_RADIANS) * this.camDistance * horz_delta;
            //cam.Z += vert_delta;

            //ground = CacheElevation(cam.X, cam.Y) + 0.2f;
            //cam.Z = max(cam.Z, ground);
            //CameraAngle = this.angle;
            //CameraPosition = cam;
        }

        private void DoLocation() {
            // TODO
            //ostringstream oss(ostringstream::in);
            //oss << APP << " ";
            ////oss << WorldLocationName (region.grid_pos.X, region.grid_pos.Y) << " (" << region.title << ") ";
            //oss << WorldLocationName((int)position.X, (int)position.Y) << " (" << region.title << ") ";
            //oss << "Looking " << WorldDirectionFromAngle(this.angle.Z);
            //SdlSetCaption(oss.str().c_str());
        }

        private void DoMove(Vector3 delta) {
            // TODO
            //Vector3 movement;
            //float forward;

            //if (CVarUtils::GetCVar<bool>("flying")) {
            //    forward = Math.Sin(this.angle.X * DEGREES_TO_RADIANS);
            //    movement.X = Math.Cos(this.angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Sin(this.angle.Z * DEGREES_TO_RADIANS) * delta.Y * forward;
            //    movement.Y = -Math.Sin(this.angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Cos(this.angle.Z * DEGREES_TO_RADIANS) * delta.Y * forward;
            //    movement.Z = Math.Cos(this.angle.X * DEGREES_TO_RADIANS) * delta.Y;
            //    position += movement;
            //} else {
            //    this.desiredMovement.X += Math.Cos(this.angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Sin(this.angle.Z * DEGREES_TO_RADIANS) * delta.Y;
            //    this.desiredMovement.Y += -Math.Sin(this.angle.Z * DEGREES_TO_RADIANS) * delta.X + Math.Cos(this.angle.Z * DEGREES_TO_RADIANS) * delta.Y;
            //}
        }
    }
}
