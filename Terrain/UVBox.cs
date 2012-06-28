/*-----------------------------------------------------------------------------
  UVBox.cs
  2011 Shamus Young
-------------------------------------------------------------------------------
  This class is used for storing and and manipulating UV texture coords.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

using OpenTK;

namespace Frontier {
	struct UVBox {
		enum UV { TopLeft, TopRight, BottomRight, BottomLeft, LeftEdge, RightEdge, TopEdge, BottomEdge }

		Vector2 ul, lr;
		Vector2 Center { get { return (ul + lr) / 2; } }

		void Set(float repeats) {
			ul = Vector2.Zero;
			lr = new Vector2(repeats, repeats);
		}

		void Set(int x, int y, int columns, int rows) {
			Vector2   frame_size;

			frame_size.X = 1.0f / (float) columns;
			frame_size.Y = 1.0f / (float) rows;

			ul = new Vector2((float) x * frame_size.X, (float) y * frame_size.Y);
			lr = new Vector2((float) (x + 1) * frame_size.X, (float) (y + 1) * frame_size.Y);
		}

		void Set(Vector2 ul_in, Vector2 lr_in) {
			ul = ul_in;
			lr = lr_in;
		}

		Vector2 Corner(UV index) {
			switch (index) {
				case UV.TopLeft:			return ul;
				case UV.TopRight:			return new Vector2(lr.X, ul.Y);
				case UV.BottomRight:	return lr;
				case UV.BottomLeft:		return new Vector2(ul.X, lr.Y);
				case UV.LeftEdge:			return new Vector2(ul.X, (ul.Y + lr.Y) / 2);
				case UV.RightEdge:		return new Vector2(lr.X, (ul.Y + lr.Y) / 2);
				case UV.TopEdge:			return new Vector2((ul.X + lr.X) / 2, ul.Y);
				case UV.BottomEdge:		return new Vector2((ul.X + lr.X) / 2, lr.Y);
			}
			return Vector2.Zero;
		}
	}
}