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
}
