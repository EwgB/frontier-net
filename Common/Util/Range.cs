namespace FrontierSharp.Common.Util {
    using System;

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
