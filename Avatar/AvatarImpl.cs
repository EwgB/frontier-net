namespace FrontierSharp.Avatar {
    using System;

    using OpenTK;
    using OpenTK.Graphics.OpenGL;
    using OpenTK.Input;

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

        private ICache cache;
        private IGame game;
        private GameWindow gameWindow;
        private IInput input;
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
                this.angle = CameraAngle = new Vector3(90, 0, 0);
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

        public AvatarImpl(
                IFigure avatar,
                ICache cache,
                IGame game,
                GameWindow gameWindow,
                IInput input,
                IParticles particles,
                IText text,
                ITextures textures,
                IWorld world) {
            this.avatar = avatar;
            this.cache = cache;
            this.game = game;
            this.gameWindow = gameWindow;
            this.input = input;
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
            if (!this.game.IsRunning)
                return;

            if (this.input.KeyState(Key.ControlLeft))
                this.Look(0, 1);

            float elapsed = (float)Math.Min(this.gameWindow.UpdateTime, 0.25f);
            Vector3 old = position;
            this.desiredMovement = Vector2.Zero;
            if (this.input.KeyPressed(Key.Space) && this.onGround) {
                this.velocity = JUMP_SPEED;
                this.onGround = false;
            }
            if (this.input.KeyPressed(Key.F2))
                this.properties.Flying ^= true; // Invert Flying
            //Joystick movement
            this.Look((int)(this.input.JoystickGet(3) * 5.0f), (int)(this.input.JoystickGet(4) * -5.0f));
            DoMove(new Vector3(this.input.JoystickGet(0), this.input.JoystickGet(1), 0));
            if (this.input.Mouselook) {
                if (this.input.MouseWheelUp)
                    this.desiredCamDistance -= 1;
                if (this.input.MouseWheelDown)
                    this.desiredCamDistance += 1;
                if (this.input.KeyState(Key.W))
                    DoMove(-Vector3.UnitY);
                if (this.input.KeyState(Key.S))
                    DoMove(Vector3.UnitY);
                if (this.input.KeyState(Key.A))
                    DoMove(-Vector3.UnitX);
                if (this.input.KeyState(Key.D))
                    DoMove(Vector3.UnitX);
                DoMove(new Vector3(this.input.JoystickGet(0), this.input.JoystickGet(1), 0));
            }
            //Figure out our   speed
            float maxSpeed = MOVE_SPEED;
            float minSpeed = 0;
            bool moving = this.desiredMovement.Length > 0;//"moving" means, "trying to move". (Pressing buttons.)
            if (moving)
                minSpeed = MOVE_SPEED * 0.33f;
            if (this.input.KeyState(Key.ShiftLeft)) {
                this.sprinting = true;
                maxSpeed = SPRINT_SPEED;
            } else {
                this.sprinting = false;
            }
            float desiredAngle = this.currentAngle;
            if (moving) {//We're trying to accelerate
                desiredAngle = MathUtils.Angle(0, 0, this.desiredMovement.X, this.desiredMovement.Y);
                this.currentSpeed += elapsed * MOVE_SPEED * ACCEL;
            } else //We've stopped pushing forward
                this.currentSpeed -= elapsed * MOVE_SPEED * DECEL;
            this.currentSpeed = MathHelper.Clamp(this.currentSpeed, minSpeed, maxSpeed);
            //Now figure out the angle of movement
            float angleAdjust = MathUtils.AngleDifference(this.currentAngle, desiredAngle);
            //if we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
            float leanAngle = 0;
            if (Math.Abs(angleAdjust) > 135)
                this.currentSpeed = SLOW_SPEED;
            if (Math.Abs(angleAdjust) < 1 || this.currentSpeed <= SLOW_SPEED) {
                this.currentAngle = desiredAngle;
                angleAdjust = 0;
            } else {
                if (Math.Abs(angleAdjust) < 135) {
                    this.currentAngle -= angleAdjust * elapsed * 2.0f;
                    leanAngle = MathHelper.Clamp(angleAdjust / 4.0f, -15, 15);
                }
            }
            this.currentMovement.X = (float)-Math.Sin(this.currentAngle * MathUtils.DEGREES_TO_RADIANS);
            this.currentMovement.Y = (float)-Math.Cos(this.currentAngle * MathUtils.DEGREES_TO_RADIANS);
            //Apply the movement
            this.currentMovement *= this.currentSpeed * elapsed;
            position.X += this.currentMovement.X;
            position.Y += this.currentMovement.Y;
            this.desiredCamDistance = MathHelper.Clamp(this.desiredCamDistance, CAM_MIN, CAM_MAX);
            this.camDistance = MathUtils.Interpolate(this.camDistance, this.desiredCamDistance, elapsed);
            float ground = this.cache.GetElevation(position.X, position.Y);
            float water = this.world.GetWaterLevel(position.X, position.Y);
            this.avatarFacing.Y = MathUtils.Interpolate(this.avatarFacing.Y, leanAngle, elapsed);
            if (!this.properties.Flying) {
                this.velocity -= WorldUtils.GRAVITY * elapsed;
                position.Z += this.velocity * elapsed;
                if (position.Z <= ground) {
                    this.onGround = true;
                    this.swimming = false;
                    position.Z = ground;
                    this.velocity = 0;
                } else if (position.Z > ground + WorldUtils.GRAVITY * 0.1f)
                    this.onGround = false;
                if (position.Z + SWIM_DEPTH < water) {
                    this.swimming = true;
                    this.velocity = 0;
                }
            }
            float movementAnimation = this.distanceWalked / 4.0f;
            if (this.onGround)
                this.distanceWalked += this.currentSpeed * elapsed;
            if (this.currentMovement.X != 0 && this.currentMovement.Y != 0)
                this.avatarFacing.Z = -MathUtils.Angle(0, 0, this.currentMovement.X, this.currentMovement.Y);
            if (this.properties.Flying)
                this.animType = AnimTypes.Flying;
            else if (this.swimming) {
                if (this.currentSpeed == 0)
                    this.animType = AnimTypes.Float;
                else
                    this.animType = AnimTypes.Swim;
            } else if (!this.onGround) {
                if (this.velocity > 0)
                    this.animType = AnimTypes.Jump;
                else
                    this.animType = AnimTypes.Fall;
            } else if (this.currentSpeed == 0)
                this.animType = AnimTypes.Idle;
            else if (this.sprinting)
                this.animType = AnimTypes.Sprint;
            else
                this.animType = AnimTypes.Run;
            this.avatar.Animate(anim[this.animType], movementAnimation);
            this.avatar.Position = position;
            this.avatar.Rotation = this.avatarFacing;
            this.avatar.Update();
            float stepTracking = movementAnimation % 1;
            if (this.animType == AnimTypes.Run || this.animType == AnimTypes.Sprint) {
                if (stepTracking < this.lastStepTracking || (stepTracking > 0.5f && this.lastStepTracking < 0.5f)) {
                    this.dustParticle.colors.Clear();
                    if (position.Z < 0)
                        this.dustParticle.colors.Add(new Color3(0.4f, 0.7f, 1));
                    else
                        this.dustParticle.colors.Add(this.cache.GetSurfaceColor((int)position.X, (int)position.Y));
                    this.particles.AddParticles(this.dustParticle, position);
                }
            }
            this.lastStepTracking = stepTracking;
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
            this.angle.X -= MathHelper.Clamp(x * mouseSensitivity, 0, 180);
            this.angle.Z += y * mouseSensitivity;
            this.angle.Z %= 360;
            if (this.angle.Z < 0)
                this.angle.Z += 360;

        }

        private void DoModel() {
            // TODO
            //this.avatar.LoadX("models//male.X");
            if (this.properties.ExpandAvatar) {
                //    this.avatar.BoneInflate(BONE_PELVIS, 0.02f, true);
                //    this.avatar.BoneInflate(BONE_HEAD, 0.025f, true);
                //    this.avatar.BoneInflate(BONE_LWRIST, 0.03f, true);
                //    this.avatar.BoneInflate(BONE_RWRIST, 0.03f, true);
                //    this.avatar.BoneInflate(BONE_RANKLE, 0.05f, true);
                //    this.avatar.BoneInflate(BONE_LANKLE, 0.05f, true);
            }
        }

        private void DoCamera() {
            // TODO
            //Vector3 cam;
            //float vert_delta;
            //float horz_delta;
            //float ground;
            //Vector2 rads;


            //rads.X = this.angle.X * MathUtils.DEGREES_TO_RADIANS;
            //vert_delta = Math.Cos(rads.X) * this.camDistance;
            //horz_delta = Math.Sin(rads.X);


            //cam = position;
            //cam.Z += EYE_HEIGHT;

            //cam.X += Math.Sin(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * this.camDistance * horz_delta;
            //cam.Y += Math.Cos(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * this.camDistance * horz_delta;
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

            if (this.properties.Flying) {
                var forward = Math.Sin(this.angle.X * MathUtils.DEGREES_TO_RADIANS);
                var movement = new Vector3(
                    (float)(Math.Cos(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Sin(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y * forward),
                    (float)(-Math.Sin(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Cos(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y * forward),
                    (float)Math.Cos(this.angle.X * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
                position += movement;
            } else {
                this.desiredMovement.X += (float)(Math.Cos(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Sin(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
                this.desiredMovement.Y += (float)(-Math.Sin(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Cos(this.angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
            }
        }
    }
}
