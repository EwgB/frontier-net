namespace FrontierSharp.Common.Util {
    /// <summary>Coord is a struct for manipulating a pair of ints. Good for grid-walking.</summary>
    public struct Coord {
        public int X { get; }
        public int Y { get; }

        public Coord(int x, int y) {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Walks succesively over all points on a square grid. Increases first on x axis, then on y axis.
        /// Returns new point, rolledOver signifies when the coordinate rolles over the whole grid.
        /// </summary>
        /// <param name="size">The size of the grid.</param>
        /// <param name="rolledOver">True iff the coordinate rolled over the grid on both axis'.</param>
        /// <returns></returns>
        public Coord Walk(int size, out bool rolledOver) {
            // TODO: Alternative implementation? (Unit test)
            // Make this method of grid, not of Coord
            var x = this.X + 1;
            var y = this.Y;
            if (x >= size) {
                x = 0;
                y++;
                if (y >= size) {
                    y = 0;
                    rolledOver = true;
                }
            }
            rolledOver = false;
            return new Coord(x, y);
        }

        public override bool Equals(object obj) {
            if (!(obj is Coord))
                return false;

            return Equals((Coord)obj);
        }

        public bool Equals(Coord other) {
            return this.X == other.X && this.Y == other.Y;
        }

        public override int GetHashCode() {
            return this.X.GetHashCode() ^ this.Y.GetHashCode();
        }

        #region Operators

        public static bool operator ==(Coord left, Coord right) { return left.Equals(right); }
        public static bool operator !=(Coord left, Coord right) { return !left.Equals(right); }

        public static Coord operator +(Coord left, Coord right) { return new Coord(left.X + right.X, left.Y + right.Y); }
        public static Coord operator +(Coord c, int i) { return new Coord(c.X + i, c.Y + i); }

        public static Coord operator -(Coord left, Coord right) { return new Coord(left.X - right.X, left.Y - right.Y); }
        public static Coord operator -(Coord c, int i) { return new Coord(c.X - i, c.Y - i); }

        public static Coord operator *(Coord left, Coord right) { return new Coord(left.X * right.X, left.Y * right.Y); }
        public static Coord operator *(Coord c, int i) { return new Coord(c.X * i, c.Y * i); }

        #endregion
    }
}
