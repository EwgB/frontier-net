namespace FrontierSharp.Avatar {
    using OpenTK;
    using OpenTK.Graphics.OpenGL;

    using Common;
    using Common.Animation;
    using Common.Avatar;
    using Common.Particles;
    using Common.Region;
    using Common.Textures;

    using Animation;
    using World;
    using Common.Property;

    public class AvatarImpl : IAvatar {

        #region Modules

        private IGame game;
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
                this.last_time = this.game.Time;
                DoModel();

            }
        }

        public IRegion Region { get; private set; }
        public Vector3 CameraAngle { get; private set; }
        public Vector3 CameraPosition { get; private set; }
        public AnimType AnimationType { get; private set; }

        private IAvatarProperties properties = new AvatarProperties();
        public IProperties Properties { get { return this.properties; } }
        public IAvatarProperties AvatarProperties { get { return this.properties; } }

        #endregion

        #region Member variables

        private Vector3 angle;
        private Vector3 avatar_facing;
        private Vector2 current_movement;
        private Vector2 desired_movement;
        private float cam_distance;
        private float desiredCamDistance;
        private bool on_ground;
        private bool swimming;
        private bool sprinting;
        private uint last_update;
        private Anim[] anim = new Anim[(int)AnimType.Max];
        private AnimType anim_id;
        private float distance_walked;
        private float last_time;
        private float current_speed;
        private float current_angle;
        private float velocity;
        private ParticleSet dust_particle;
        private float last_step_tracking;

        #endregion

        public AvatarImpl(IFigure avatar, IGame game, ITextures textures, IWorld world) {
            this.avatar = avatar;
            this.game = game;
            this.textures = textures;
            this.world = world;

            // TODO: Is this the desired behaviour? Where should this be initialised?
            this.Region = this.world.GetRegion(0, 0);
        }

        public void Init() {
            //desiredCamDistance = IniFloat("Avatar", "CameraDistance");
            DoModel();
            /*
            for (int i = 0; i < ANIM_COUNT; i++) {
                anim[i].LoadBvh(IniString("Animations", anim_names[i]));
                IniStringSet("Animations", anim_names[i], IniString("Animations", anim_names[i]));
            }
            ParticleLoad("step", &dust_particle);
            */
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

    }
}

/* From Avatar.cpp
#define JUMP_SPEED      4.0f
#define MOVE_SPEED      5.5f
#define SLOW_SPEED      (MOVE_SPEED * 0.15f)
#define SPRINT_SPEED    8.0f
#define EYE_HEIGHT      1.75f
#define CAM_MIN         1
#define CAM_MAX         12
#define STOP_SPEED      0.02f
#define SWIM_DEPTH      1.4f
#define ACCEL           0.66f
#define DECEL           1.5f

static void do_move(Vector3 delta) {

    Vector3 movement;
    float forward;

    if (CVarUtils::GetCVar<bool>("flying")) {
        forward = sin(angle.x * DEGREES_TO_RADIANS);
        movement.x = cos(angle.z * DEGREES_TO_RADIANS) * delta.x + sin(angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
        movement.y = -sin(angle.z * DEGREES_TO_RADIANS) * delta.x + cos(angle.z * DEGREES_TO_RADIANS) * delta.y * forward;
        movement.z = cos(angle.x * DEGREES_TO_RADIANS) * delta.y;
        position += movement;
    } else {
        desired_movement.x += cos(angle.z * DEGREES_TO_RADIANS) * delta.x + sin(angle.z * DEGREES_TO_RADIANS) * delta.y;
        desired_movement.y += -sin(angle.z * DEGREES_TO_RADIANS) * delta.x + cos(angle.z * DEGREES_TO_RADIANS) * delta.y;
    }

}

void do_camera() {

    Vector3 cam;
    float vert_delta;
    float horz_delta;
    float ground;
    Vector2 rads;


    rads.x = angle.x * DEGREES_TO_RADIANS;
    vert_delta = cos(rads.x) * cam_distance;
    horz_delta = sin(rads.x);


    cam = position;
    cam.z += EYE_HEIGHT;

    cam.x += sin(angle.z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
    cam.y += cos(angle.z * DEGREES_TO_RADIANS) * cam_distance * horz_delta;
    cam.z += vert_delta;

    ground = CacheElevation(cam.x, cam.y) + 0.2f;
    cam.z = max(cam.z, ground);
    CameraAngle = angle;
    CameraPosition = cam;

}



void do_location() {

    ostringstream oss(ostringstream::in);

    oss << APP << " ";
    //oss << WorldLocationName (region.grid_pos.x, region.grid_pos.y) << " (" << region.title << ") ";
    oss << WorldLocationName((int)position.x, (int)position.y) << " (" << region.title << ") ";
    oss << "Looking " << WorldDirectionFromAngle(angle.z);
    SdlSetCaption(oss.str().c_str());

}

AnimType AvatarAnim() {
    return anim_id;
}

void AvatarLook(int x, int y) {

    float mouse_sense;

    if (CVarUtils::GetCVar<bool>("mouse.invert"))
        x = -x;
    mouse_sense = CVarUtils::GetCVar<float>("mouse.sensitivity");
    angle.x -= (float)x * mouse_sense;
    angle.z += (float)y * mouse_sense;
    angle.x = clamp(angle.x, 0.0f, 180.0f);
    angle.z = fmod(angle.z, 360.0f);
    if (angle.z < 0.0f)
        angle.z += 360.0f;


}

*/
