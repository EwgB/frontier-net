namespace FrontierSharp.Interfaces {
    using OpenTK;
    using OpenTK.Graphics;
    using System;

    public interface IEnvironment {
        EnvironmentData GetCurrent();
    }

    public class EnvironmentData {
        // TODO: maybe replace by some appropriate construct, remove the COUNT value from enum
        public Color4[] color = new Color4[(int)ColorType.ENV_COLOR_COUNT];
        public Vector3 light;
        public Range<float> fog;
        public float star_fade;
        public float sunrise_fade;
        public float sunset_fade;
        public float sun_angle;
        public float cloud_cover;
        public bool draw_sun;
    }

    // TODO: rename
    public enum ColorType {
        ENV_COLOR_HORIZON,
        ENV_COLOR_SKY,
        ENV_COLOR_FOG,
        ENV_COLOR_LIGHT,
        ENV_COLOR_AMBIENT,
        ENV_COLOR_COUNT
    }

    public struct Range<T> where T : IComparable<T> {
        private T min;
        public T Min {
            get { return min; }
            set {
                if (value.CompareTo(max) > 0)
                    throw new ArgumentOutOfRangeException("Min can't be larger than Max");
                min = value;
            }
        }

        private T max;
        public T Max {
            get { return max; }
            set {
                if (value.CompareTo(min) < 0)
                    throw new ArgumentOutOfRangeException("Max can't be smaller than Min");
                max = value;
            }
        }

        public Range(T min, T max) : this() {
            // Set Max first because of the invariance checks in the setters
            this.Max = max;
            this.Min = min;
        }

    }
}
