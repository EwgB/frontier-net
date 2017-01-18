namespace FrontierSharp.Interfaces {
    using System;
    using System.Collections;

    using OpenTK;
    using OpenTK.Graphics;

    using Util;

    public interface IEnvironment {
        EnvironmentData GetCurrent();

        void Init();
    }

    public class EnvironmentData {
        public ColorTypeIndexedArray<Color4> color = new ColorTypeIndexedArray<Color4>();

        //public Color4 color2[ColorType c] { get; set; }
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
        Ambient
    }
    public class ColorTypeIndexedArray<T> : IEnumerable {
        private T[] elements = new T[Enum.GetNames(typeof(ColorType)).Length];
        public T this[ColorType index] {
            get { return elements[(int)index]; }
            set { elements[(int)index] = value; }
        }
        public IEnumerator GetEnumerator() => elements.GetEnumerator();
    }

}
