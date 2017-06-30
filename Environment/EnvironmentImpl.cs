namespace FrontierSharp.Environment {
    using System;

    using OpenTK;
    using OpenTK.Graphics;

    using Interfaces;
    using Interfaces.Environment;
    using Interfaces.Property;
    using Interfaces.Region;

    using Util;

    public class EnvironmentImpl : IEnvironment {
        #region Constants
        /// <summary>How many milliseconds per in-game minute</summary>
        private const int TIME_SCALE = 300;
        //private const int MAX_DISTANCE = 900;
        //private const float NIGHT_FOG = (MAX_DISTANCE / 5);
        private const float ENV_TRANSITION = 0.02f;
        /// <summary>In milliseconds</summary>
        private const float UPDATE_INTERVAL = 50;
        private const float SECONDS_TO_DECIMAL = (1.0f / 60.0f);

        private const float TIME_DAWN = 5.5f;       // 5:30am
        private const float TIME_DAY = 6.5f;        // 6:30am
        private const float TIME_SUNSET = 19.5f;    // 7:30pm
        private const float TIME_DUSK = 20.5f;      // 8:30pm

        private static readonly Color4 NIGHT_COLOR = new Color4(0, 0, 0.3f, 1);
        private static readonly Color4 DAY_COLOR = Color4.White;

        private static readonly Color4 NIGHT_SCALING = new Color4(0.0f, 0.1f, 0.4f, 1);
        private static readonly Color4 DAY_SCALING = Color4.White;

        private static readonly Vector3 VECTOR_NIGHT = new Vector3(0.0f, 0.0f, -1.0f);
        private static readonly Vector3 VECTOR_SUNRISE = new Vector3(-0.8f, 0.0f, -0.2f);
        private static readonly Vector3 VECTOR_MORNING = new Vector3(-0.5f, 0.0f, -0.5f);
        private static readonly Vector3 VECTOR_AFTERNOON = new Vector3(0.5f, 0.0f, -0.5f);
        private static readonly Vector3 VECTOR_SUNSET = new Vector3(0.8f, 0.0f, -0.2f);

        private const int SUN_ANGLE_SUNRISE = -10;
        private const int SUN_ANGLE_MORNING = 15;
        private const int SUN_ANGLE_AFTERNOON = 165;
        private const int SUN_ANGLE_SUNSET = 190;
        #endregion

        #region Modules
        private readonly IGame game;
        #endregion

        #region Properties and variables
        private EnvironmentProperties properties = new EnvironmentProperties();
        public IProperties Properties { get { return this.properties; } }

        public EnvironmentData Current { get; private set; }

        private EnvironmentData Desired { get; set; }

        private float lastDecimalTime;
        //static int        update;
        //static bool       cycle_on;
        #endregion

        public EnvironmentImpl(IGame game) {
            this.game = game;

            this.Current = new EnvironmentData();
            this.Desired = new EnvironmentData();
        }

        public void Init() {
            doTime(1);
            this.Current = this.Desired;
        }

        public void Update() {
            // TODO
            //  update += SdlElapsed ();
            //  if (update > UPDATE_INTERVAL) {
            //    doTime (ENV_TRANSITION);
            //    update -= UPDATE_INTERVAL;
            //  }
        }

        private void doTime(float delta) {
            //Convert out hours and minutes into a decimal number. (100 "minutes" per hour.)
            Desired.Light = new Vector3(-0.5f, 0.0f, -0.5f);
            if (this.game.Time != lastDecimalTime)
                doCycle();
            lastDecimalTime = this.game.Time;
            for (var colorType = ColorType.Horizon; colorType < ColorType.Max; colorType++) {
                Current.Color[colorType] = ColorUtils.Interpolate(Current.Color[colorType], Desired.Color[colorType], delta);
            }
            Current.Fog = new Range<float>(
                MathUtils.Interpolate(Current.Fog.Min, Desired.Fog.Min, delta),
                MathUtils.Interpolate(Current.Fog.Max, Desired.Fog.Max, delta));
            Current.StarFade = MathUtils.Interpolate(Current.StarFade, Desired.StarFade, delta);
            Current.SunsetFade = MathUtils.Interpolate(Current.SunsetFade, Desired.SunsetFade, delta);
            Current.SunriseFade = MathUtils.Interpolate(Current.SunriseFade, Desired.SunriseFade, delta);
            Current.Light = MathUtils.Interpolate(Current.Light, Desired.Light, delta);
            Current.SunAngle = MathUtils.Interpolate(Current.SunAngle, Desired.SunAngle, delta);
            Current.CloudCover = MathUtils.Interpolate(Current.CloudCover, Desired.CloudCover, delta);
            Current.DrawSun = Desired.DrawSun;
            Current.Light.Normalize();
        }

        private void doCycle() {
            //  Region*   r;
            //  int       i;
            //  GLrgba    average;
            //  GLrgba    base_color;
            //  GLrgba    color_scaling;
            //  GLrgba    atmosphere;
            //  float     fade;
            //  float     late_fade;
            //  //float     humid_Fog;
            //  float     decimal_time;
            //  float     max_distance;
            //  Range     time_Fog;
            //  Range     humid_Fog;

            //  max_distance = SceneVisibleRange ();
            //  r = (Region*)AvatarRegion ();
            //  //atmosphere = r->color_atmosphere;
            //  humid_Fog.Max = MathUtils.Interpolate (max_distance, max_distance * 0.75f, r->moisture);
            //  humid_Fog.Min = MathUtils.Interpolate (max_distance * 0.85f, max_distance * 0.25f, r->moisture);
            //  if (r->climate == CLIMATE_SWAMP) {
            //    humid_Fog.Max /= 2.0f;
            //    humid_Fog.Min /= 2.0f;
            //  }
            //  Desired.cloud_cover = clamp (r->moisture, 0.20f, 0.6f);
            //  Desired.sunrise_fade = Desired.sunset_fade = 0.0f;
            //  decimal_time = fmod (GameTime (), 24.0f);
            //  if (decimal_time >= TIME_DAWN && decimal_time < TIME_DAY) { //sunrise
            //    fade = (decimal_time - TIME_DAWN) / (TIME_DAY - TIME_DAWN);
            //    late_fade = max ((fade -0.5f) * 2.0f, 0);
            //    base_color = ColorUtils.InterpolateColors (NIGHT_COLOR, DAY_COLOR, late_fade);
            //    atmosphere = ColorUtils.InterpolateColors (glRgba (0.0f), glRgba (1.0f), late_fade);
            //    time_Fog.Max = MathUtils.Interpolate (NIGHT_FOG, max_distance, fade);
            //    time_Fog.Min = time_Fog.Max / 2.0f;
            //    Desired.star_fade = max (1.0f - fade * 2.0f, 0.0f);
            //    //Sunrise fades in, then back out
            //    Desired.sunrise_fade = 1.0f - abs (fade -0.5f) * 2.0f;
            //    color_scaling = ColorUtils.InterpolateColors (NIGHT_SCALING, DAY_SCALING, fade);
            //    //The Light in the sky doesn't Lighten until the second half of sunrise
            //    if (fade > 0.5f)
            //      Desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 1.0f, 0.5f);
            //    else
            //      Desired.color[ENV_COLOR_LIGHT] = glRgba (0.5f, 0.7f, 1.0f);
            //    Desired.Light = glVectorInterpolate (VECTOR_SUNRISE, VECTOR_MORNING, fade);
            //    Desired.SunAngle = MathUtils.Interpolate (SUN_ANGLE_SUNRISE, SUN_ANGLE_MORNING, fade);
            //    Desired.draw_sun = true;
            //    Desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 0.6f);
            //  } else if (decimal_time >= TIME_DAY && decimal_time < TIME_SUNSET)  { //day
            //    atmosphere = glRgba (1.0f);
            //    fade = (decimal_time - TIME_DAY) / (TIME_SUNSET - TIME_DAY);
            //    base_color = DAY_COLOR;
            //    time_Fog.Max = max_distance;
            //    time_Fog.Min = time_Fog.Max / 2.0f;
            //    Desired.star_fade = 0.0f;
            //    color_scaling = DAY_SCALING;
            //    Desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f) + r->color_atmosphere;
            //    Desired.color[ENV_COLOR_LIGHT].Normalize ();
            //    Desired.Light = glVector (0, 0.5f, -0.5f);
            //    Desired.Light = glVectorInterpolate (VECTOR_MORNING, VECTOR_AFTERNOON, fade);
            //    Desired.SunAngle = MathUtils.Interpolate (SUN_ANGLE_MORNING, SUN_ANGLE_AFTERNOON, fade);
            //    Desired.draw_sun = true;
            //    Desired.color[ENV_COLOR_AMBIENT] = glRgba (0.4f, 0.4f, 0.4f);
            //  } else if (decimal_time >= TIME_SUNSET && decimal_time < TIME_DUSK) { // sunset
            //    fade = (decimal_time - TIME_SUNSET) / (TIME_DUSK - TIME_SUNSET);
            //    base_color = ColorUtils.InterpolateColors (DAY_COLOR, NIGHT_COLOR, fade);
            //    time_Fog.Max = MathUtils.Interpolate (max_distance, NIGHT_FOG, fade);
            //    time_Fog.Min = time_Fog.Max / 2.0f;
            //    if (fade > 0.5f)
            //      Desired.star_fade = (fade - 0.5f) * 2.0f;
            //    //Sunset fades in, then back out
            //    atmosphere = ColorUtils.InterpolateColors (glRgba (1.0f), glRgba (0.0f), min (1.0f, fade * 2.0f));
            //    Desired.sunset_fade = 1.0f - abs (fade -0.5f) * 2.0f;
            //    color_scaling = ColorUtils.InterpolateColors (DAY_SCALING, NIGHT_SCALING, fade);
            //    Desired.color[ENV_COLOR_LIGHT] = glRgba (1.0f, 0.5f, 0.5f);
            //    Desired.Light = glVector (0.8f, 0.0f, -0.2f);
            //    Desired.Light = glVectorInterpolate (VECTOR_AFTERNOON, VECTOR_SUNSET, fade);
            //    Desired.SunAngle = MathUtils.Interpolate (SUN_ANGLE_AFTERNOON, SUN_ANGLE_SUNSET, fade);
            //    Desired.draw_sun = true;
            //    Desired.color[ENV_COLOR_AMBIENT] = glRgba (0.3f, 0.3f, 0.6f);
            // } else { //night
            //   atmosphere = glRgba (0.0f);
            //    color_scaling = NIGHT_SCALING;
            //    base_color = NIGHT_COLOR;
            //    time_Fog.Min = 1;
            //    time_Fog.Max = NIGHT_FOG;
            //    Desired.star_fade = 1.0f;
            //    Desired.color[ENV_COLOR_LIGHT] = glRgba (0.1f, 0.3f, 0.7f);
            //    Desired.color[ENV_COLOR_AMBIENT] = glRgba (0.0f, 0.0f, 0.4f);
            //    Desired.Light = VECTOR_NIGHT;
            //    Desired.SunAngle = -90.0f;
            //    Desired.draw_sun = false;
            //  }
            //  Desired.Fog.Max = min (humid_Fog.Max, time_Fog.Max);
            //  Desired.Fog.Min = min (humid_Fog.Min, time_Fog.Min);
            //  for (i = 0; i < ENV_COLOR_COUNT; i++) {
            //    if (i == ENV_COLOR_LIGHT || i == ENV_COLOR_AMBIENT) 
            //      continue;
            //    average = base_color * atmosphere;
            //    //average.Normalize ();
            //    //average /= 3;
            //    Desired.color[i] = average;
            //    if (i == ENV_COLOR_SKY) 
            //      Desired.color[i] = base_color * 0.75f;
            //    Desired.color[i] *= color_scaling;
            //  }   
            //  Desired.color[ENV_COLOR_SKY] = r->color_atmosphere;
            //  Desired.color[ENV_COLOR_HORIZON] = (Desired.color[ENV_COLOR_SKY] + atmosphere + atmosphere) / 3.0f;
            //  Desired.color[ENV_COLOR_FOG] = Desired.color[ENV_COLOR_HORIZON];//Desired.color[ENV_COLOR_SKY];
            //  //Desired.color[ENV_COLOR_SKY] = Desired.color[ENV_COLOR_HORIZON] * glRgba (0.2f, 0.2f, 0.8f);

        }
    }
}
