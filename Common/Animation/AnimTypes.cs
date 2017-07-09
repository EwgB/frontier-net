namespace FrontierSharp.Common.Animation {
    public enum AnimTypes {
        Idle,
        Run,
        Sprint,
        Flying,
        Fall,
        Jump,
        Swim,
        Float,
        Max
    }

    public class AnimationTypeArray {
        private readonly IAnimation[] elements = new IAnimation[(int)AnimTypes.Max];
        public IAnimation this[AnimTypes index] {
            get { return this.elements[(int)index]; }
            set { this.elements[(int)index] = value; }
        }
    }
}
