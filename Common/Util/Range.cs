namespace FrontierSharp.Common.Util {
    using System;

    public struct Range<T> where T : IComparable<T> {
        private T min;
        public T Min {
            get { return this.min; }
            set {
                if (value.CompareTo(this.max) > 0)
                    throw new ArgumentOutOfRangeException("Min can't be larger than Max");
                this.min = value;
            }
        }

        private T max;
        public T Max {
            get { return this.max; }
            set {
                if (value.CompareTo(this.min) < 0)
                    throw new ArgumentOutOfRangeException("Max can't be smaller than Min");
                this.max = value;
            }
        }

        public Range(T min, T max) : this() {
            // Set Max first because of the invariance checks in the setters
            this.Max = max;
            this.Min = min;
        }

    }
}
