///<summary>  Coord is a struct for manipulating a pair of ints. Good for grid-walking.</summary>
namespace terrain_sharp.Source.GLTypes {
	class Coord {
		public int X { get; set; }
		public int Y { get; set; }

		public Coord() {
			X = 0;
			Y = 0;
		}

		public Coord(int x, int y) {
			X = x;
			Y = y;
		}

		public Coord(Coord coord) {
			X = coord.X;
			Y = coord.Y;
		}

		public void Clear() {
			X = Y = 0;
		}

		public bool Walk(int size) {
			return Walk(size, size);
		}

		public bool Walk(int x_size, int y_size) {
			X++;
			if (X >= x_size) {
				Y++;
				X = 0;
				if (Y >= y_size) {
					Y = 0;
					return true;
				}
			}
			return false;
		}

		public override bool Equals(object o) {
			return (o is Coord) ? this == (o as Coord) : false;
		}

		public override int GetHashCode() {
			return X ^ Y;
		}

		public static bool operator ==(Coord c1, Coord c2) {
			return (c1.X == c2.X && c1.Y == c2.Y);
		}

		public static bool operator !=(Coord c1, Coord c2) {
			return !(c1 == c2);
		}

		public static Coord operator +(Coord c1, Coord c2) {
			return new Coord(c1.X + c2.X, c1.Y + c2.Y);
		}

		public static Coord operator +(Coord c, int d) {
			return new Coord(c.X + d, c.Y + d);
		}

		public static Coord operator -(Coord c1, Coord c2) {
			return new Coord(c1.X - c2.X, c1.Y - c2.Y);
		}

		public static Coord operator -(Coord c, int d) {
			return new Coord(c.X - d, c.Y - d);
		}

		public static Coord operator *(Coord c1, Coord c2) {
			return new Coord(c1.X * c2.X, c1.Y * c2.Y);
		}

		public static Coord operator *(Coord c, int d) {
			return new Coord(c.X * d, c.Y * d);
		}
	}
}
