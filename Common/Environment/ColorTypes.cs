namespace FrontierSharp.Common.Environment {
    using Util;

    public enum ColorTypes {
        Horizon,
        Sky,
        Fog,
        Light,
        Ambient,
        Max
    }

    public class ColorTypeArray {
        private Color3[] elements = new Color3[(int)ColorTypes.Max];
        public Color3 this[ColorTypes index] {
            get { return elements[(int)index]; }
            set { elements[(int)index] = value; }
        }
    }
}
