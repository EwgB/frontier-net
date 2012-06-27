/*-----------------------------------------------------------------------------
  GridManager.cs
-------------------------------------------------------------------------------
  The grid manager handles various types of objects that make up the world. 
  Terrain, blocks of trees, grass, etc.  It takes tables of GridData objects
  and shuffles them around, rendering them and prioritizing their updates
  to favor things closest to the player.
-----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

namespace Frontier {
	//The grid manager. You need one of these for each type of object you plan to manage.
	class GridManager {
		struct Dist {
			public Coord offset;
			public float distancef;
			public int   distancei;
		}

		#region Class fields and properties

		private const int
			TABLE_SIZE = 32,
			TABLE_HALF = (TABLE_SIZE / 2);

		protected List<GridData> items;       //Our list of items
		protected Coord lastViewer;

		protected int
			gridSize,  //The size of the grid of items to manage. Should be odd. Bigger = see farther.
			gridHalf,  //The mid-point of the grid
			itemSize,  //Size of an item in world units.
			//_item_bytes, //size of items, in bytes
			itemCount; //How many total items in the table?

		private static List<Dist> disanceList;
		//private static List<Dist> foo2;
		private static bool listReady;

		public int ItemsReady { get; private set; }
		public int ItemsViewable { get; private set; }	//How many items in the table are within the viewable circle?

		#endregion

		#region Class methods
		protected Coord ViewPosition(Vector2 eye) { return new Coord((int) eye.X / itemSize, (int) eye.Y / itemSize); }

		protected GridData Item(int index)	{ return items[index]; }
		protected GridData Item(Coord c)		{ return items[(c.X % gridSize) + (c.Y % gridSize) * gridSize]; }

		public void Render()					{ foreach (GridData d in items) d.Render(); }
		public void RestartProgress()	{ ItemsReady = 0; }

		public GridManager() {
			items = new List<GridData>();
			Clear();
		}

		private int DistSort(Dist elem1, Dist elem2) {
			if (elem1.distancef < elem2.distancef)
		    return -1;
			else if (elem1.distancef > elem2.distancef)
		    return 1;
		  return 0;
		}

		/* Here we build a list of offsets.  These are used to walk a grid outward in
		 * concentric circles.  This is used to make sure we update the items closest to 
		 * the player first. */
		private static void DoList() {

			int     x, y, i;
			Dist    d;
			Vector2 to_center;

			listReady = true;
			disanceList.Capacity = TABLE_SIZE * TABLE_SIZE;
			//foo2.resize.Capacity = TABLE_SIZE * TABLE_SIZE;

			i = 0;
			for (x = 0; x < TABLE_SIZE; x++) {
				for (y = 0; y < TABLE_SIZE; y++) {
					d = disanceList[i];
					d.offset.X = x - TABLE_HALF;
					d.offset.Y = y - TABLE_HALF;
					to_center.X = (float) d.offset.X;
					to_center.Y = (float) d.offset.Y;
					d.distancef = to_center.Length;
					d.distancei = (int) d.distancef;
					i++;
				}
			}
			qsort(disanceList[0], disanceList.Count, sizeof(Dist), DistSort);
		}

		public void Clear() {
			items.Clear();
			gridSize = 0;
			gridHalf = 0;
			itemSize = 0;
			//_item_bytes = 0;
			itemCount = 0;
			lastViewer = new Coord(0, 0);
			ItemsReady = 0;
		}

		public void Init(List<GridData> items, int grid_size, int item_size) {
		  GridData  gd;
		  Coord     walk;
		  int       i;

		  if (!listReady)
		    DoList();

			this.items = items;
		  gridSize = grid_size;
		  gridHalf = gridSize / 2;
		  itemSize = item_size;
			//_item_bytes = items[0].Sizeof ();
		  itemCount = gridSize * gridSize;
		  lastViewer = ViewPosition (AvatarPosition());
		  ItemsReady = 0;
		  walk.Clear ();
			ItemsViewable = 0;

			for (i = 0; i < disanceList.Count; i++) {
				if (disanceList[i].distancei <= gridHalf)
					ItemsViewable++;
				else
					break;
			}

			do {
		    gd = Item(walk);
		    gd.Invalidate();
		    //gd.Set (0, 0, 0);
		    //gd.Set ( lastViewer.x + walk.x - gridHalf,  lastViewer.y + walk.y - gridHalf, 0);
		    //gd.Set (viewer.x + walk.x - gridHalf, viewer.y + walk.y - gridHalf, 0);
		  } while (!walk.Walk (gridSize));
		}

		void Update(long stop) {
			Coord  viewer, pos, gridPos;
			int    dist;

			viewer = ViewPosition(AvatarPosition());
			// If the player has moved to a new spot on the grid, restart our outward walk.
			if (viewer != lastViewer) {
				lastViewer = viewer;
				ItemsReady = 0;
			}

			// Figure out where the player is in our rolling grid
			gridPos.X = gridHalf + viewer.X % gridSize;
			gridPos.Y = gridHalf + viewer.Y % gridSize;

			// Now offset that with the position being updated.
			gridPos += disanceList[ItemsReady].offset;

			// Bring it back into bounds.
			if (gridPos.X < 0)		gridPos.X += gridSize;
			if (gridPos.Y < 0)		gridPos.Y += gridSize;
			gridPos.X %= gridSize;
			gridPos.Y %= gridSize;

			pos = Item(gridPos).GridPosition;

			if (viewer.X - pos.X > (int) gridHalf)		pos.X += gridSize;
			if (pos.X - viewer.X > (int) gridHalf)		pos.X -= gridSize;
			if (viewer.Y - pos.Y > (int) gridHalf)		pos.Y += gridSize;
			if (pos.Y - viewer.Y > (int) gridHalf)		pos.Y -= gridSize;

			pos = viewer + disanceList[ItemsReady].offset;
			dist = Math.Max(Math.Abs(pos.X - viewer.X), Math.Abs(pos.Y - viewer.Y));

			Item(gridPos).Set(pos.X, pos.Y, dist);
			Item(gridPos).Update(stop);

			if (Item(gridPos).Ready()) {
				ItemsReady++;
				//If we reach the outer ring, move back to the center and begin again.
				if (disanceList[ItemsReady].distancei > gridHalf)
					ItemsReady = 0;
			}
		}
		#endregion

	}
}