namespace FrontierSharp.Common.Environment {
    using OpenTK;

    using Util;

    public class EnvironmentData {
        public ColorTypeArray Color = new ColorTypeArray();

        //public Color3 color2[ColorType c] { get; set; }
        public Vector3 Light { get; set; }
        public Range<float> Fog { get; set; }
        public float StarFade { get; set; }
        public float SunriseFade { get; set; }
        public float SunsetFade { get; set; }
        public float SunAngle { get; set; }
        public float CloudCover { get; set; }
        public bool DrawSun { get; set; }
    }

    public enum ColorType {
        Horizon,
        Sky,
        Fog,
        Light,
        Ambient,
        Max
    }

    public class ColorTypeArray {
        private Color3[] elements = new Color3[(int)ColorType.Max];
        public Color3 this[ColorType index] {
            get { return elements[(int)index]; }
            set { elements[(int)index] = value; }
        }
    }
}
