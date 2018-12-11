namespace FrontierSharp.Avatar {
    using System;

    using Ninject;
    using OpenTK;
    using OpenTK.Graphics.OpenGL;
    using OpenTK.Input;

    using Common;
    using Common.Animation;
    using Common.Avatar;
    using Common.Input;
    using Common.Particles;
    using Common.Property;
    using Common.Region;
    using Common.Textures;
    using Common.World;
    using Common.Util;

    internal class AvatarImpl : IAvatar {

        #region Constants

        private const float JUMP_SPEED = 4.0f;
        private const float MOVE_SPEED = 5.5f;
        private const float SLOW_SPEED = MOVE_SPEED * 0.15f;
        private const float SPRINT_SPEED = 8.0f;
        private const float EYE_HEIGHT = 1.75f;
        private const float CAM_MIN = 1;
        private const float CAM_MAX = 12;
        private const float SWIM_DEPTH = 1.4f;
        private const float ACCEL = 0.66f;
        private const float DECEL = 1.5f;

        #endregion

        #region Modules

        private readonly IKernel kernel;

        private ICache cache;
        private ICache Cache => cache ?? (cache = kernel.Get<ICache>());

        private GameWindow gameWindow;
        private GameWindow GameWindow => gameWindow ?? (gameWindow = kernel.Get<GameWindow>());

        private IInput input;
        private IInput Input => input ?? (input = kernel.Get<IInput>());

        private IParticles particles;
        private IParticles Particles => particles ?? (particles = kernel.Get<IParticles>());

        private IText text;
        private IText Text => text ?? (text = kernel.Get<IText>());

        private ITextures textures;
        private ITextures Textures => textures ?? (textures = kernel.Get<ITextures>());

        private IWorld world;
        private IWorld World => world ?? (world = kernel.Get<IWorld>());

        private IFigure figure;
        private IFigure Figure => figure ?? (figure = kernel.Get<IFigure>());

        #endregion

        #region Properties

        private Vector3 position;
        public Vector3 Position {
            get => position;
            set {
                position.Z = MathHelper.Clamp(value.Z, -25, 2048);
                position.X = MathHelper.Clamp(value.X, 0, WorldUtils.REGION_SIZE * WorldUtils.WORLD_GRID);
                position.Y = MathHelper.Clamp(value.Y, 0, WorldUtils.REGION_SIZE * WorldUtils.WORLD_GRID);
                CameraPosition = position;
                angle = CameraAngle = new Vector3(90, 0, 0);
                DoModel();
            }
        }

        public Region Region { get; private set; }
        public Vector3 CameraAngle { get; private set; }
        public Vector3 CameraPosition { get; private set; }
        public AnimTypes AnimationType { get; private set; }

        public IAvatarProperties AvatarProperties { get; } = new AvatarProperties();
        public IProperties Properties => AvatarProperties;

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
        private readonly AnimationTypeArray anim = new AnimationTypeArray();
        private AnimTypes animType;
        private float distanceWalked;
        private float currentSpeed;
        private float currentAngle;
        private float velocity;
        private ParticleSet dustParticle;
        private float lastStepTracking;

        #endregion

        public AvatarImpl(IKernel kernel) {
            this.kernel = kernel;

            // TODO: Is this the desired behaviour? Where should this be initialised?
            Region = World.GetRegion(0, 0);
        }

        public void Init() {
            //this.desiredCamDistance = IniFloat("Avatar", "CameraDistance"); // TODO: do we want to add ini options?
            DoModel();
            // TODO: Convert body
            for (var i = AnimTypes.Idle; i < AnimTypes.Max; i++) {
                /*
                    anim[i].LoadBvh(IniString("Animations", AnimTypes.names[i]));
                    IniStringSet("Animations", AnimTypes.names[i], IniString("Animations", AnimTypes.names[i]));
                */
            }
            dustParticle = Particles.LoadParticles("step");
        }

        public void Update() {
            if (Input.KeyState(Key.ControlLeft))
                Look(0, 1);

            var elapsed = (float)Math.Min(GameWindow.UpdateTime, 0.25f);
            desiredMovement = Vector2.Zero;
            if (Input.KeyPressed(Key.Space) && onGround) {
                velocity = JUMP_SPEED;
                onGround = false;
            }
            if (Input.KeyPressed(Key.F2))
                AvatarProperties.Flying ^= true; // Invert Flying
            //Joystick movement
            Look((int)(Input.Joystick[3] * 5.0f), (int)(Input.Joystick[4] * -5.0f));
            DoMove(new Vector3(Input.Joystick[0], Input.Joystick[1], 0));
            if (Input.Mouselook) {
                if (Input.MouseWheelUp)
                    desiredCamDistance -= 1;
                if (Input.MouseWheelDown)
                    desiredCamDistance += 1;
                if (Input.KeyState(Key.W))
                    DoMove(-Vector3.UnitY);
                if (Input.KeyState(Key.S))
                    DoMove(Vector3.UnitY);
                if (Input.KeyState(Key.A))
                    DoMove(-Vector3.UnitX);
                if (Input.KeyState(Key.D))
                    DoMove(Vector3.UnitX);
                DoMove(new Vector3(Input.Joystick[0], Input.Joystick[1], 0));
            }
            //Figure out our   speed
            var maxSpeed = MOVE_SPEED;
            float minSpeed = 0;
            var moving = desiredMovement.Length > 0;//"moving" means, "trying to move". (Pressing buttons.)
            if (moving)
                minSpeed = MOVE_SPEED * 0.33f;
            if (Input.KeyState(Key.ShiftLeft)) {
                sprinting = true;
                maxSpeed = SPRINT_SPEED;
            } else {
                sprinting = false;
            }
            var desiredAngle = currentAngle;
            if (moving) {//We're trying to accelerate
                desiredAngle = MathUtils.Angle(0, 0, desiredMovement.X, desiredMovement.Y);
                currentSpeed += elapsed * MOVE_SPEED * ACCEL;
            } else //We've stopped pushing forward
                currentSpeed -= elapsed * MOVE_SPEED * DECEL;
            currentSpeed = MathHelper.Clamp(currentSpeed, minSpeed, maxSpeed);
            //Now figure out the angle of movement
            var angleAdjust = MathUtils.AngleDifference(currentAngle, desiredAngle);
            //if we're trying to reverse direction, don't do a huge, arcing turn.  Just slow and double back
            float leanAngle = 0;
            if (Math.Abs(angleAdjust) > 135)
                currentSpeed = SLOW_SPEED;
            if (Math.Abs(angleAdjust) < 1 || currentSpeed <= SLOW_SPEED) {
                currentAngle = desiredAngle;
            } else {
                if (Math.Abs(angleAdjust) < 135) {
                    currentAngle -= angleAdjust * elapsed * 2.0f;
                    leanAngle = MathHelper.Clamp(angleAdjust / 4.0f, -15, 15);
                }
            }
            currentMovement.X = (float)-Math.Sin(currentAngle * MathUtils.DEGREES_TO_RADIANS);
            currentMovement.Y = (float)-Math.Cos(currentAngle * MathUtils.DEGREES_TO_RADIANS);
            //Apply the movement
            currentMovement *= currentSpeed * elapsed;
            position.X += currentMovement.X;
            position.Y += currentMovement.Y;
            desiredCamDistance = MathHelper.Clamp(desiredCamDistance, CAM_MIN, CAM_MAX);
            camDistance = MathUtils.Interpolate(camDistance, desiredCamDistance, elapsed);
            var ground = Cache.GetElevation(position.X, position.Y);
            var water = World.GetWaterLevel((int) position.X, (int) position.Y);
            avatarFacing.Y = MathUtils.Interpolate(avatarFacing.Y, leanAngle, elapsed);
            if (!AvatarProperties.Flying) {
                velocity -= WorldUtils.GRAVITY * elapsed;
                position.Z += velocity * elapsed;
                if (position.Z <= ground) {
                    onGround = true;
                    swimming = false;
                    position.Z = ground;
                    velocity = 0;
                } else if (position.Z > ground + WorldUtils.GRAVITY * 0.1f)
                    onGround = false;
                if (position.Z + SWIM_DEPTH < water) {
                    swimming = true;
                    velocity = 0;
                }
            }
            var movementAnimation = distanceWalked / 4.0f;
            if (onGround)
                distanceWalked += currentSpeed * elapsed;
            if (currentMovement.X != 0 && currentMovement.Y != 0)
                avatarFacing.Z = -MathUtils.Angle(0, 0, currentMovement.X, currentMovement.Y);
            if (AvatarProperties.Flying)
                animType = AnimTypes.Flying;
            else if (swimming) {
                animType = currentSpeed == 0 ? AnimTypes.Float : AnimTypes.Swim;
            } else if (!onGround) {
                animType = velocity > 0 ? AnimTypes.Jump : AnimTypes.Fall;
            }
            else if (currentSpeed == 0)
                animType = AnimTypes.Idle;
            else if (sprinting)
                animType = AnimTypes.Sprint;
            else
                animType = AnimTypes.Run;
            Figure.Animate(anim[animType], movementAnimation);
            Figure.Position = position;
            Figure.Rotation = avatarFacing;
            Figure.Update();
            var stepTracking = movementAnimation % 1;
            if (animType == AnimTypes.Run || animType == AnimTypes.Sprint) {
                if (stepTracking < lastStepTracking || (stepTracking > 0.5f && lastStepTracking < 0.5f)) {
                    dustParticle.Colors.Clear();
                    if (position.Z < 0)
                        dustParticle.Colors.Add(new Color3(0.4f, 0.7f, 1));
                    else
                        dustParticle.Colors.Add(Cache.GetSurfaceColor((int) position.X, (int) position.Y));
                    Particles.AddParticles(dustParticle, position);
                }
            }
            lastStepTracking = stepTracking;
            Text.Print("{0} elapsed: {1}", animType.ToString(), elapsed);
            Region = World.GetRegion(
                (int)(position.X + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE,
                (int)(position.Y + WorldUtils.REGION_HALF) / WorldUtils.REGION_SIZE);
            DoCamera();
            DoLocation();
        }

        public void Render() {
            GL.BindTexture(TextureTarget.Texture2D, Textures.TextureIdFromName("avatar.png"));
            GL.BindTexture(TextureTarget.Texture2D, 0);
            Figure.Render();
            if (AvatarProperties.ShowSkeleton) {
                Figure.RenderSkeleton();
            }
        }

        public void Look(int x, int y) {
            if (AvatarProperties.InvertMouse)
                x = -x;
            var mouseSensitivity = AvatarProperties.MouseSensitivity;
            angle.X -= MathHelper.Clamp(x * mouseSensitivity, 0, 180);
            angle.Z += y * mouseSensitivity;
            angle.Z %= 360;
            if (angle.Z < 0)
                angle.Z += 360;

        }

        private void DoModel() {
            // TODO
            //this.Figure.LoadX("models//male.X");
            if (AvatarProperties.ExpandAvatar) {
                //    this.Figure.BoneInflate(BONE_PELVIS, 0.02f, true);
                //    this.Figure.BoneInflate(BONE_HEAD, 0.025f, true);
                //    this.Figure.BoneInflate(BONE_LWRIST, 0.03f, true);
                //    this.Figure.BoneInflate(BONE_RWRIST, 0.03f, true);
                //    this.Figure.BoneInflate(BONE_RANKLE, 0.05f, true);
                //    this.Figure.BoneInflate(BONE_LANKLE, 0.05f, true);
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
            if (AvatarProperties.Flying) {
                var forward = Math.Sin(angle.X * MathUtils.DEGREES_TO_RADIANS);
                var movement = new Vector3(
                    (float)(Math.Cos(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Sin(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y * forward),
                    (float)(-Math.Sin(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Cos(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y * forward),
                    (float)Math.Cos(angle.X * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
                position += movement;
            } else {
                desiredMovement.X += (float)(Math.Cos(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Sin(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
                desiredMovement.Y += (float)(-Math.Sin(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.X + Math.Cos(angle.Z * MathUtils.DEGREES_TO_RADIANS) * delta.Y);
            }
        }
    }
}
