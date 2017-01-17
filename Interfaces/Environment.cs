namespace FrontierSharp.Interfaces {
    using OpenTK;
    using OpenTK.Graphics;
    using System;

    public interface IEnvironment {
        EnvironmentData GetCurrent();
    }

    public class EnvironmentData {
        // TODO: maybe replace by some appropriate construct, remove the COUNT value from enum
        Color4[] color = new Color4[(int) ColorType.ENV_COLOR_COUNT];
        Vector3 light;
        Range<float> fog;
        float star_fade;
        float sunrise_fade;
        float sunset_fade;
        float sun_angle;
        float cloud_cover;
        bool draw_sun;
    }

    // TODO: rename
    internal enum ColorType {
        ENV_COLOR_HORIZON,
        ENV_COLOR_SKY,
        ENV_COLOR_FOG,
        ENV_COLOR_LIGHT,
        ENV_COLOR_AMBIENT,
        ENV_COLOR_COUNT
    }

    internal struct Range<T> where T : IComparable<T> {
        private T min;
        T Min { 
            get { return min; }
            set {
                if (value.CompareTo(max) > 0)
                    throw new ArgumentOutOfRangeException("Min can't be larger than Max");
                min = value;
            }
        }

        private T max;
        T Max
        {
            get { return max; }
            set {
                if (value.CompareTo(min) < 0)
                    throw new ArgumentOutOfRangeException("Max can't be smaller than Min");
                max = value;
            }
        }
    }
}
