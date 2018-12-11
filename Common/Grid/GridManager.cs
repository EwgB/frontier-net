namespace FrontierSharp.Common.Grid {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using OpenTK;

    using Avatar;
    using Util;

    /// <summary>
    /// The grid manager. You need one of these for each type of object you plan to manage.
    /// </summary>
    public class GridManager : ITimeCapped, IRenderable {

        private struct Dist {
            public Coord Offset;
            public float DistanceFloat;
            public int DistanceInt;
        }

        private readonly IAvatar avatar;

        public int ItemsReadyCount => listPos;
        
        /// <summary>How many items in the table are within the viewable circle?</summary>
        public int ItemsViewableCount { get; private set; }

        #region Private members

        /// <summary>Our list of items</summary>
        private List<IGridData> items;

        /// <summary>The size of the grid of items to manage. Should be odd. Bigger = see farther.</summary>
        private int gridSize;

        /// <summary>The mid-point of the grid</summary>
        private int gridHalf;

        /// <summary>Size of an item in world units.</summary>
        private int itemSize;

        private Coord lastViewer;
        private int listPos;
        private bool listReady;
        private readonly List<Dist> distanceList = new List<Dist>();

        #endregion

        public GridManager(IAvatar avatar) {
            this.avatar = avatar;
            Clear();
        }

        public void Init(List<IGridData> newItems, int newGridSize, int newItemSize) {
            if (!listReady)
                DoList();

            items = newItems;
            gridSize = newGridSize;
            gridHalf = gridSize / 2;
            itemSize = newItemSize;
            Debug.Assert(items.Count == gridSize * gridSize);
            lastViewer = GetViewPosition(avatar.Position);
            listPos = 0;

            var walk = new Coord();
            ItemsViewableCount = 0;
            for (var i = 0; i < distanceList.Count; i++) {
                if (distanceList[i].DistanceInt <= gridHalf)
                    ItemsViewableCount++;
                else
                    break;
            }

            bool rollover;
            do {
                var gridData = GetItem(walk);
                gridData.Invalidate();
                //gd.Set (0, 0, 0);
                //gd.Set ( this.lastViewer.X + walk.X - this.gridHalf,  this.lastViewer.Y + walk.Y - this.gridHalf, 0);
                //gd.Set (viewer.X + walk.X - this.gridHalf, viewer.Y + walk.Y - this.gridHalf, 0);
                walk = walk.Walk(gridSize, out rollover);
            } while (!rollover);
        }

        public void Clear() {
            items = null;
            gridSize = 0;
            gridHalf = 0;
            itemSize = 0;
            lastViewer = new Coord();
            listPos = 0;
        }

        public void RestartProgress() {
            listPos = 0;
        }

        public void Update(double stopAt) {
            if (null == items)
                return;
            var viewer = GetViewPosition(avatar.Position);
            // If the player has moved to a new spot on the grid, restart our outward walk.
            if (viewer != lastViewer) {
                lastViewer = viewer;
                listPos = 0;
            }

            // Figure out where the player is in our rolling grid.
            var gridPos = new Coord(
                gridHalf + viewer.X % gridSize,
                gridHalf + viewer.Y % gridSize);
            // Now offset that with the position being updated.
            gridPos += distanceList[listPos].Offset;
            // Bring it back into bounds.
            gridPos = new Coord(
                gridPos.X + gridSize % gridSize,
                gridPos.Y + gridSize % gridSize);

            var item = GetItem(gridPos);
            var pos = item.GridPosition;

            var x = pos.X;
            if (viewer.X - pos.X > gridHalf)
                x += gridSize;
            else if (pos.X - viewer.X > gridHalf)
                x -= gridSize;

            var y = pos.Y;
            if (viewer.Y - pos.Y > gridHalf)
                y += gridSize;
            else if (pos.Y - viewer.Y > gridHalf)
                y -= gridSize;

            pos = new Coord(x, y) + viewer + distanceList[listPos].Offset;
            var dist = Math.Max(Math.Abs(pos.X - viewer.X), Math.Abs(pos.Y - viewer.Y));
            item.Set(pos, dist);
            item.Update(stopAt);

            if (item.IsReady) {
                listPos++;
                // If we reach the outer ring, move back to the center and begin again.
                if (distanceList[listPos].DistanceInt > gridHalf)
                    listPos = 0;
            }
        }

        public void Render() {
            items.ForEach(item => item.Render());
        }

        private IGridData GetItem(Coord c) {
            //No more dicey pointer arithmetic. C# is a more awesome language than C++!
            return items[(c.X % gridSize) + (c.Y % gridSize) * gridSize];
        }

        private Coord GetViewPosition(Vector3 eye) {
            return new Coord(
                (int) (eye.X / itemSize),
                (int) (eye.Y / itemSize));
        }

        private void DoList() {
            listReady = true;
            distanceList.Capacity = GridUtils.TABLE_SIZE * GridUtils.TABLE_SIZE;
            //  foo2.resize (GridUtils.TABLE_SIZE *  GridUtils.TABLE_SIZE);
            var i = 0;
            for (var x = 0; x < GridUtils.TABLE_SIZE; x++) {
                for (var y = 0; y < GridUtils.TABLE_SIZE; y++) {
                    var d = distanceList[i];
                    d.Offset = new Coord(
                        x - GridUtils.TABLE_HALF,
                        y - GridUtils.TABLE_HALF);
                    var toCenter = new Vector2(d.Offset.X, d.Offset.Y);
                    d.DistanceFloat = toCenter.Length;
                    d.DistanceInt = (int) d.DistanceFloat;
                    i++;
                }
            }
            distanceList.Sort((left, right) => left.DistanceFloat.CompareTo(right.DistanceFloat));
        }
    }
}

