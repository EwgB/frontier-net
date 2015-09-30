///<summary>  Coord is a struct for manipulating a pair of ints. Good for grid-walking.</summary>
namespace terrain_sharp.Source.GLTypes {
	class Coord {
		public int x { get; set; }
		public int y { get; set; }

		public Coord(int x, int y) {
			this.x = x;
			this.y = y;
		}

		public void Clear() {
			x = y = 0;
		}

		public bool Walk(int size) {
			return Walk(size, size);
		}

		public bool Walk(int x_size, int y_size) {
			x++;
			if (x >= x_size) {
				y++;
				x = 0;
				if (y >= y_size) {
					y = 0;
					return true;
				}
			}
			return false;
		}

		public override bool Equals(object o) {
			return (o is Coord) ? this == (o as Coord) : false;
		}

		public override int GetHashCode() {
			return x ^ y;
		}

		public static bool operator ==(Coord c1, Coord c2) {
			return (c1.x == c2.x && c1.y == c2.y);
		}

		public static bool operator !=(Coord c1, Coord c2) {
			return !(c1 == c2);
		}

		public static Coord operator +(Coord c1, Coord c2) {
			return new Coord(c1.x + c2.x, c1.y + c2.y);
		}

		public static Coord operator +(Coord c, int d) {
			return new Coord(c.x + d, c.y + d);
		}

		public static Coord operator -(Coord c1, Coord c2) {
			return new Coord(c1.x - c2.x, c1.y - c2.y);
		}

		public static Coord operator -(Coord c, int d) {
			return new Coord(c.x - d, c.y - d);
		}

		public static Coord operator *(Coord c1, Coord c2) {
			return new Coord(c1.x * c2.x, c1.y * c2.y);
		}

		public static Coord operator *(Coord c, int d) {
			return new Coord(c.x * d, c.y * d);
		}
	}
}
