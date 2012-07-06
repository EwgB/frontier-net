using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Frontier {
	//A virtual class.  Anything to be managed should be a subclass of this
	abstract class GridData {
		//protected BBox bbox;

		protected Coord mGridPosition;
		public Coord GridPosition { get { return mGridPosition; } protected set { mGridPosition = value; } }

		public bool Valid { get; protected set; }
		public void Invalidate() { Valid = false; }

		public abstract bool IsReady { get; }
		public abstract void Render();
		public abstract void Set(int x, int y, int distance);
		public abstract void Update(long stop);
	}
}
