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
		public int X { get; set; }
		public int Y { get; set; }

		public Coord(int x, int y)	{ X = x;	Y = y; }
		public Coord(Coord c)				{ X = c.X; Y = c.Y; }

		public static bool operator ==(Coord a, Coord b)	{ return  (a.X == b.X && a.Y == b.Y); }
		public static bool operator !=(Coord a, Coord b)	{ return !(a.X == b.X && a.Y == b.Y); }

		public static Coord operator +(Coord a, Coord b) { return new Coord(a.X + b.X, a.Y + b.Y); }
		public static Coord operator +(Coord c, int i) { return new Coord(c.X + i, c.Y + i); }

		public static Coord operator -(Coord a, Coord b) { return new Coord(a.X - b.X, a.Y - b.Y); }
		public static Coord operator -(Coord c, int i)		{ return new Coord(c.X - i, c.Y - i);}

		public static Coord operator *(Coord a, Coord b)	{ return new Coord(a.X * b.X, a.Y * b.Y); }
		public static Coord operator *(Coord c, int i)		{ return new Coord(c.X * i, c.Y * i); }

		public bool Walk(int size) { return Walk (size, size); }
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
	}
}
