using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Frontier {
	//A virtual class.  Anything to be managed should be a subclass of this
	abstract class GridData {
		//protected BBox bbox;

		protected Coord mGridPosition;
		public Coord GridPosition { get { return mGridPosition; } private set { mGridPosition = value; } }

		public bool Ready();
		public void Render();
		public void Set(int x, int y, int distance);
		public void Update(long stop);
		public void Invalidate();
	}
}
