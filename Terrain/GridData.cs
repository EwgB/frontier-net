using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Frontier {
	//A virtual class.  Anything to be managed should be a subclass of this
	interface GridData {
		//protected BBox bbox;

		Coord GridPosition { get; }
		bool Ready();
		void Render();
		void Set(int x, int y, int distance);
		void Update(long stop);
		void Invalidate();
	}
}
