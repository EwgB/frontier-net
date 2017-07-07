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
            float ground;
            float water;
            float movement_animation;
            float time_passed;
            bool moving;
            float max_speed;
            float min_speed;
            float desired_angle;
            float lean_angle;
            float angle_adjust;
            float step_tracking;

            if (!this.game.IsRunning)
                return;

            if (InputKeyState(SDLK_LCTRL))
                this.Look(0, 1);

            float elapsed = Math.Min(SdlElapsedSeconds(), 0.25f);
            Vector3 old = position;
            this.desiredMovement = Vector2.Zero;
            if (InputKeyPressed(SDLK_SPACE) && this.onGround) {
                this.velocity = JUMP_SPEED;
                this.onGround = false;
            }
            if (InputKeyPressed(SDLK_F2))
                CVarUtils::SetCVar("flying", !CVarUtils::GetCVar<bool>("flying"));
            //Joystick movement
            this.Look((int)(InputJoystickGet(3) * 5.0f), (int)(InputJoystickGet(4) * -5.0f));
            DoMove(new Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));
            if (InputMouselook()) {
                if (InputKeyPressed(INPUT_MWHEEL_UP))
                    this.desiredCamDistance -= 1.0f;
                if (InputKeyPressed(INPUT_MWHEEL_DOWN))
                    this.desiredCamDistance += 1.0f;
                if (InputKeyState(SDLK_w))
                    DoMove(-Vector3.UnitY);
                if (InputKeyState(SDLK_s))
                    DoMove(Vector3.UnitY);
                if (InputKeyState(SDLK_a))
                    DoMove(-Vector3.UnitX);
                if (InputKeyState(SDLK_d))
                    DoMove(Vector3.UnitX);
                DoMove(new Vector3(InputJoystickGet(0), InputJoystickGet(1), 0.0f));
            }
            //Figure out our   speed
            max_speed = MOVE_SPEED;
            min_speed = 0.0f;
            moving = this.desiredMovement.Length > 0.0f;//"moving" means, "trying to move". (Pressing buttons.)
            if (moving)
                min_speed = MOVE_SPEED * 0.33f;
            if (InputKeyState(SDLK_LSHIFT)) {
                this.sprinting = true;
                max_speed = SPRINT_SPEED;
            } else
                this.sprinting = false;
            desired_angle = this.currentAngle;
            if (moving) {//We're trying to accelerate
                desired_angle = MathAngle(0.0f, 0.0f, this.desiredMovement.X, this.desiredMovement.Y);
                this.currentSpeed += elapsed * MOVE_SPEED * ACCEL;
            } else //We've stopped pushing forward
                this.currentSpeed -= elapsed * MOVE_SPEED * DECEL;
            this.currentSpeed = MathHelper.Clamp(this.currentSpeed, min_speed, max_speed);
            //Now figure out the angle of movement
            angle_adjust = MathAngleDifference(this.currentAngle, desired_angle);
            //if we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
            lean_angle = 0.0f;
            if (Math.Abs(angle_adjust) > 135)
                this.currentSpeed = SLOW_SPEED;
            if (Math.Abs(angle_adjust) < 1.0f || this.currentSpeed <= SLOW_SPEED) {
                this.currentAngle = desired_angle;
                angle_adjust = 0.0f;
            } else {
                if (Math.Abs(angle_adjust) < 135) {
                    this.currentAngle -= angle_adjust * elapsed * 2.0f;
                    lean_angle = MathHelper.Clamp(angle_adjust / 4.0f, -15, 15);
                }
            }
            this.currentMovement.X = -Math.Sin(this.currentAngle * DEGREES_TO_RADIANS);
            this.currentMovement.Y = -Math.Cos(this.currentAngle * DEGREES_TO_RADIANS);
            //Apply the movement
            this.currentMovement *= this.currentSpeed * elapsed;
            position.X += this.currentMovement.X;
            position.Y += this.currentMovement.Y;
            this.desiredCamDistance = MathHelper.Clamp(this.desiredCamDistance, CAM_MIN, CAM_MAX);
            this.camDistance = MathUtils.Interpolate(this.camDistance, this.desiredCamDistance, elapsed);
            ground = CacheElevation(position.X, position.Y);
            water = this.world.GetWaterLevel(position.X, position.Y);
            this.avatarFacing.Y = MathUtils.Interpolate(this.avatarFacing.Y, lean_angle, elapsed);
            bool flying = CVarUtils::GetCVar<bool>("flying");
            if (!flying) {
                this.velocity -= GRAVITY * elapsed;
                position.Z += this.velocity * elapsed;
                if (position.Z <= ground) {
                    this.onGround = true;
                    this.swimming = false;
                    position.Z = ground;
                    this.velocity = 0.0f;
                } else if (position.Z > ground + GRAVITY * 0.1f)
                    this.onGround = false;
                if (position.Z + SWIM_DEPTH < water) {
                    this.swimming = true;
                    this.velocity = 0.0f;
                }
            }
            movement_animation = this.distanceWalked / 4.0f;
            if (this.onGround)
                this.distanceWalked += this.currentSpeed * elapsed;
            if (this.currentMovement.X != 0.0f && this.currentMovement.Y != 0.0f)
                this.avatarFacing.Z = -MathAngle(0.0f, 0.0f, this.currentMovement.X, this.currentMovement.Y);
            if (flying)
                this.animType = AnimTypes.Flying;
            else if (this.swimming) {
                if (this.currentSpeed == 0.0f)
                    this.animType = AnimTypes.Float;
                else
                    this.animType = AnimTypes.Swim;
            } else if (!this.onGround) {
                if (this.velocity > 0.0f)
                    this.animType = AnimTypes.Jump;
                else
                    this.animType = AnimTypes.Fall;
            } else if (this.currentSpeed == 0.0f)
                this.animType = AnimTypes.Idle;
            else if (this.sprinting)
                this.animType = AnimTypes.Sprint;
            else
                this.animType = AnimTypes.Run;
            this.avatar.Animate(anim[this.animType], movement_animation);
            this.avatar.Position = position;
            this.avatar.Rotation = this.avatarFacing;
            this.avatar.Update();
            step_tracking = movement_animation % 1.0f;
            if (this.animType == AnimTypes.Run || this.animType == AnimTypes.Sprint) {
                if (step_tracking < this.lastStepTracking || (step_tracking > 0.5f && this.lastStepTracking < 0.5f)) {
                    this.dustParticle.colors.Clear();
                    if (position.Z < 0.0f)
                        this.dustParticle.colors.Add(new Color3(0.4f, 0.7f, 1.0f));
                    else
                        this.dustParticle.colors.Add(CacheSurfaceColor((int)position.X, (int)position.Y));
                    this.particles.AddParticles(this.dustParticle, position);
                }
            }
            this.lastStepTracking = step_tracking;
            time_passed = this.game.Time - this.lastTime;
            this.lastTime = this.game.Time;
            this.text.Print("{0} elapsed: {1}", this.animType.ToString(), elapsed);
            this.Region = this.world.GetRegion(
                (int)(this.position.X + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE,
                (int)(this.position.Y + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE);
            DoCamera();
            DoLocation();
        }

        public void Render() {
            GL.BindTexture(TextureTarget.Texture2D, this.textures.TextureIdFromName("avatar.png"));
            GL.BindTexture(TextureTarget.Texture2D, 0);
            this.avatar.Render();
            if (this.properties.ShowSkeleton) {
                this.avatar.RenderSkeleton();
            }
        }

        public void Look(int x, int y) {
            if (this.properties.InvertMouse)
                x = -x;
            float mouseSensitivity = this.properties.MouseSensitivity;
            this.angle.X -= MathHelper.Clamp(x * mouseSensitivity, 0.0f, 180.0f);
            this.angle.Z += y * mouseSensitivity;
            this.angle.Z %= 360.0f;
            if (this.angle.Z < 0.0f)
                this.angle.Z += 360.0f;

        }

        private void DoModel() {
            // TODO
            //this.avatar.LoadX("models//male.X");
            //if (CVarUtils::GetCVar<bool>("avatar.expand")) {
            //    this.avatar.BoneInflate(BONE_PELVIS, 0.02f, true);
            //    this.avatar.BoneInflate(BONE_HEAD, 0.025f, true);
            //    this.avatar.BoneInflate(BONE_LWRIST, 0.03f, true);
            //    this.avatar.BoneInflate(BONE_RWRIST, 0.03f, true);
            //    this.avatar.BoneInflate(BONE_RANKLE, 0.05f, true);
            //    this.avatar.BoneInflate(BONE_LANKLE, 0.05f, true);
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
