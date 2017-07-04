namespace FrontierSharp.Environment {
    using System;
    using NLog;

    using OpenTK;

    using Common;
    using Common.Avatar;
    using Common.Environment;
    using Common.Property;
    using Common.Region;
    using Common.Scene;
    using Common.Util;

    public class EnvironmentImpl : IEnvironment {
        #region Constants
        /// <summary>How many milliseconds per in-game minute</summary>
        private const int TIME_SCALE = 300;
        //private const int MAX_DISTANCE = 900;
        //private const float NIGHT_FOG = (MAX_DISTANCE / 5);
        private const float ENV_TRANSITION = 0.02f;
        /// <summary>In milliseconds</summary>
        //private const float UPDATE_INTERVAL = 50;
        private const float SECONDS_TO_DECIMAL = (1.0f / 60.0f);

        private const float TIME_DAWN = 5.5f;       // 5:30am
        private const float TIME_DAY = 6.5f;        // 6:30am
        private const float TIME_SUNSET = 19.5f;    // 7:30pm
        private const float TIME_DUSK = 20.5f;      // 8:30pm

        private static readonly Color3 NIGHT_COLOR = new Color3(0, 0, 0.3f);
        private static readonly Color3 DAY_COLOR = Color3.White;

        private static readonly Color3 NIGHT_SCALING = new Color3(0.0f, 0.1f, 0.4f);
        private static readonly Color3 DAY_SCALING = Color3.White;

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
        private readonly IAvatar avatar;
        private readonly IGame game;
        private readonly IScene scene;
        #endregion

        #region Properties and variables

        // Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private EnvironmentProperties properties = new EnvironmentProperties();
        public IProperties Properties { get { return this.properties; } }

        public EnvironmentData Current { get; private set; }

        private EnvironmentData Desired { get; set; }

        private float lastDecimalTime;

        #endregion

        public EnvironmentImpl(IAvatar avatar, IGame game, IScene scene) {
            this.avatar = avatar;
            this.game = game;
            this.scene = scene;

            this.Current = new EnvironmentData();
            this.Desired = new EnvironmentData();
        }

        public void Init() {
            Log.Info("Init");
            DoTime(1);
            this.Current = this.Desired;
        }

        public void Update() {
            // Original code had updates bound to framerate, so it used the following mechanic to simulate a constant update rate.
            // We don't really need it here.
            //update += SdlElapsed();
            //if (update > UPDATE_INTERVAL) {
            DoTime(ENV_TRANSITION);
            //    update -= UPDATE_INTERVAL;
            //}
        }

        private void DoTime(float delta) {
            //Convert out hours and minutes into a decimal number. (100 "minutes" per hour.)
            Desired.Light = new Vector3(-0.5f, 0.0f, -0.5f);
            if (this.game.Time != lastDecimalTime)
                DoCycle();
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
        
        private struct CycleInParameters {
            internal float DecimalTime;
            internal float MaxDistance;
            internal float NightFog;
            internal IRegion Region;
        }

        private struct CycleOutParameters {
            internal Range<float> TimeFog;
            internal Color3 BaseColor;
            internal Color3 Atmosphere;
            internal Color3 ColorScaling;
        }

        private void DoCycle() {
            float maxDistance = this.scene.VisibleRange;
            float nightFog = maxDistance / 5;
            IRegion region = this.avatar.Region;
            //atmosphere = region.ColorAtmosphere;
            var humidFog = new Range<float>(
                MathUtils.Interpolate(maxDistance * 0.85f, maxDistance * 0.25f, region.Moisture),
                MathUtils.Interpolate(maxDistance, maxDistance * 0.75f, region.Moisture));
            if (region.Climate == Climate.Swamp) {
                humidFog.Min /= 2.0f;
                humidFog.Max /= 2.0f;
            }
            Desired.CloudCover = MathHelper.Clamp(region.Moisture, 0.20f, 0.6f);
            Desired.SunriseFade = Desired.SunsetFade = 0.0f;

            float decimalTime = this.game.Time % 24;
            var inParams = new CycleInParameters() { DecimalTime = decimalTime, MaxDistance = maxDistance, NightFog = nightFog, Region = region };
            CycleOutParameters outParams;
            if (decimalTime >= TIME_DAWN && decimalTime < TIME_DAY) {
                ProcessSunrise(inParams, out outParams);
            } else if (decimalTime >= TIME_DAY && decimalTime < TIME_SUNSET) {
                ProcessDay(inParams, out outParams);
            } else if (decimalTime >= TIME_SUNSET && decimalTime < TIME_DUSK) {
                ProcessSunset(inParams, out outParams);
            } else { //night
                ProcessNight(inParams, out outParams);
            }

            Desired.Fog = new Range<float>(
                Math.Min(humidFog.Min, outParams.TimeFog.Min),
                Math.Min(humidFog.Max, outParams.TimeFog.Max));

            for (var colorType = ColorType.Horizon; colorType < ColorType.Max; colorType++) {
                if (colorType == ColorType.Light || colorType == ColorType.Ambient)
                    continue;
                Color3 average = outParams.BaseColor * outParams.Atmosphere;
                //average = average.Normalize() / 3;
                Desired.Color[colorType] = average;
                if (colorType == ColorType.Sky)
                    Desired.Color[colorType] = outParams.BaseColor * 0.75f;
                Desired.Color[colorType] *= outParams.ColorScaling;
            }

            Desired.Color[ColorType.Sky] = region.ColorAtmosphere;
            Desired.Color[ColorType.Fog] = Desired.Color[ColorType.Horizon] = (Desired.Color[ColorType.Sky] + outParams.Atmosphere * 2) / 3;
            //Desired.Color[ColorType.Sky] = Desired.Color[ColorType.Horizon] * (new Color3 (0.2f, 0.2f, 0.8f));
        }

        private void ProcessSunrise(CycleInParameters inParams, out CycleOutParameters outParams) {
            float fade = (inParams.DecimalTime - TIME_DAWN) / (TIME_DAY - TIME_DAWN);
            float lateFade = Math.Max((fade - 0.5f) * 2.0f, 0);
                Desired.Color[ColorType.Light] = new Color3(0.5f, 0.7f, 1.0f);
            outParams.BaseColor = ColorUtils.Interpolate(NIGHT_COLOR, DAY_COLOR, lateFade);
            outParams.Atmosphere = ColorUtils.Interpolate(Color3.Black, Color3.White, lateFade);
            var timeFog = new Range<float>();
            timeFog.Max = MathUtils.Interpolate(inParams.NightFog, inParams.MaxDistance, fade);
            timeFog.Min = timeFog.Max / 2.0f;
            outParams.TimeFog = timeFog;
            Desired.StarFade = Math.Max(1.0f - fade * 2.0f, 0.0f);
            // Sunrise fades in, then back out
            Desired.SunriseFade = 1.0f - Math.Abs(fade - 0.5f) * 2.0f;
            outParams.ColorScaling = ColorUtils.Interpolate(NIGHT_SCALING, DAY_SCALING, fade);
            //The light in the sky doesn't lighten until the second half of sunrise
            if (fade > 0.5f)
                Desired.Color[ColorType.Light] = new Color3(1.0f, 1.0f, 0.5f);
            else
                Desired.Color[ColorType.Light] = new Color3(0.5f, 0.7f, 1.0f);
            Desired.Light = MathUtils.Interpolate(VECTOR_SUNRISE, VECTOR_MORNING, fade);
            Desired.SunAngle = MathUtils.Interpolate(SUN_ANGLE_SUNRISE, SUN_ANGLE_MORNING, fade);
            Desired.DrawSun = true;
            Desired.Color[ColorType.Ambient] = new Color3(0.3f, 0.3f, 0.6f);
        }

        private void ProcessDay(CycleInParameters inParams, out CycleOutParameters outParams) {
            outParams.Atmosphere = Color3.White;
            float fade = (inParams.DecimalTime - TIME_DAY) / (TIME_SUNSET - TIME_DAY);
            outParams.BaseColor = DAY_COLOR;
            var timeFog = new Range<float>();
            timeFog.Max = inParams.MaxDistance;
            timeFog.Min = timeFog.Max / 2.0f;
            outParams.TimeFog = timeFog;
            Desired.StarFade = 0.0f;
            outParams.ColorScaling = DAY_SCALING;
            Desired.Color[ColorType.Light] = (Color3.White + inParams.Region.ColorAtmosphere).Normalize();
            Desired.Light = new Vector3(0, 0.5f, -0.5f);
            Desired.Light = MathUtils.Interpolate(VECTOR_MORNING, VECTOR_AFTERNOON, fade);
            Desired.SunAngle = MathUtils.Interpolate(SUN_ANGLE_MORNING, SUN_ANGLE_AFTERNOON, fade);
            Desired.DrawSun = true;
            Desired.Color[ColorType.Ambient] = new Color3(0.4f, 0.4f, 0.4f);
        }

        private void ProcessSunset(CycleInParameters inParams, out CycleOutParameters outParams) {
            float fade = (inParams.DecimalTime - TIME_SUNSET) / (TIME_DUSK - TIME_SUNSET);
            outParams.BaseColor = ColorUtils.Interpolate(DAY_COLOR, NIGHT_COLOR, fade);
            var timeFog = new Range<float>();
            timeFog.Max = MathUtils.Interpolate(inParams.MaxDistance, inParams.NightFog, fade);
            timeFog.Min = timeFog.Max / 2.0f;
            outParams.TimeFog = timeFog;
            if (fade > 0.5f)
                Desired.StarFade = (fade - 0.5f) * 2.0f;
            //Sunset fades in, then back out
            outParams.Atmosphere = ColorUtils.Interpolate(Color3.White, Color3.Black, Math.Min(1.0f, fade * 2.0f));
            Desired.SunsetFade = 1.0f - Math.Abs(fade - 0.5f) * 2.0f;
            outParams.ColorScaling = ColorUtils.Interpolate(DAY_SCALING, NIGHT_SCALING, fade);
            Desired.Color[ColorType.Light] = new Color3(1.0f, 0.5f, 0.5f);
            Desired.Light = new Vector3(0.8f, 0.0f, -0.2f);
            Desired.Light = MathUtils.Interpolate(VECTOR_AFTERNOON, VECTOR_SUNSET, fade);
            Desired.SunAngle = MathUtils.Interpolate(SUN_ANGLE_AFTERNOON, SUN_ANGLE_SUNSET, fade);
            Desired.DrawSun = true;
            Desired.Color[ColorType.Ambient] = new Color3(0.3f, 0.3f, 0.6f);
        }

        private void ProcessNight(CycleInParameters inParams, out CycleOutParameters outParams) {
            outParams.Atmosphere = Color3.Black;
            outParams.ColorScaling = NIGHT_SCALING;
            outParams.BaseColor = NIGHT_COLOR;
            outParams.TimeFog = new Range<float>(1, inParams.NightFog);
            Desired.StarFade = 1.0f;
            Desired.Color[ColorType.Light] = new Color3(0.1f, 0.3f, 0.7f);
            Desired.Color[ColorType.Ambient] = new Color3(0.0f, 0.0f, 0.4f);
            Desired.Light = VECTOR_NIGHT;
            Desired.SunAngle = -90.0f;
            Desired.DrawSun = false;
        }
    }
}
