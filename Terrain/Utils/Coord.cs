/*-----------------------------------------------------------------------------
  Coord.cs
  2011 Shamus Young
-------------------------------------------------------------------------------
  Coord is a struct for manipulating a pair of ints. Good for grid-walking.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Frontier {
	struct Coord {
		private int x;
		public int X { get { return x; } set { x = value; } }

		private int y;
		public int Y { get { return y; } set { y = value; } }

		public static Coord North	= new Coord( 0, -1);
		public static Coord South	= new Coord( 0,  1);
		public static Coord East	= new Coord( 1,  0);
		public static Coord West	= new Coord(-1,  0);

		public Coord(int x, int y)	{ this.x = x;	this.y = y; }
		public Coord(Coord c)				{ this.x = c.X; this.y = c.Y; }

		public static bool operator ==(Coord a, Coord b)	{ return  (a.X == b.X && a.Y == b.Y); }
		public static bool operator !=(Coord a, Coord b)	{ return !(a.X == b.X && a.Y == b.Y); }

		public static Coord operator +(Coord a, Coord b)	{ return new Coord(a.X + b.X, a.Y + b.Y); }
		public static Coord operator +(Coord c, int i)		{ return new Coord(c.X + i, c.Y + i); }

		public static Coord operator -(Coord a, Coord b)	{ return new Coord(a.X - b.X, a.Y - b.Y); }
		public static Coord operator -(Coord c, int i)		{ return new Coord(c.X - i, c.Y - i);}

		public static Coord operator *(Coord a, Coord b)	{ return new Coord(a.X * b.X, a.Y * b.Y); }
		public static Coord operator *(Coord c, int i)		{ return new Coord(c.X * i, c.Y * i); }

		public bool Walk(int size) { return Walk(size, size); }
		public bool Walk(int x, int y) {
			X++;
			if (X >= x) {
				Y++;
				X = 0;
				if (Y >= y) {
					Y = 0; 
					return true;
				}
			}
			return false;
		}

		public void Clear() { X = Y = 0; }

		public bool Equals(Coord p) { return (X == p.X) && (Y == p.Y); }
		public override bool Equals(Object obj) {
			// If parameter is null return false.
			if (obj == null)
				return false;

			// If parameter cannot be cast to Point return false.
			if (obj is Coord)
				// Return true if the fields match:
				return (X == ((Coord) obj).X) && (Y == ((Coord) obj).Y);
			else
				return false;
		}

		public override int GetHashCode() { return X ^ Y; }
	}
}
