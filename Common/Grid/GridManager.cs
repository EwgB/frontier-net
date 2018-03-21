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

        public int ItemsReadyCount => this.listPos;
        
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
            if (!this.listReady)
                DoList();

            this.items = newItems;
            this.gridSize = newGridSize;
            this.gridHalf = this.gridSize / 2;
            this.itemSize = newItemSize;
            Debug.Assert(this.items.Count == this.gridSize * this.gridSize);
            this.lastViewer = GetViewPosition(this.avatar.Position);
            this.listPos = 0;

            var walk = new Coord();
            this.ItemsViewableCount = 0;
            for (var i = 0; i < this.distanceList.Count; i++) {
                if (this.distanceList[i].DistanceInt <= this.gridHalf)
                    this.ItemsViewableCount++;
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
                walk = walk.Walk(this.gridSize, out rollover);
            } while (!rollover);
        }

        public void Clear() {
            this.items = null;
            this.gridSize = 0;
            this.gridHalf = 0;
            this.itemSize = 0;
            this.lastViewer = new Coord();
            this.listPos = 0;
        }

        public void RestartProgress() {
            this.listPos = 0;
        }

        public void Update(double stopAt) {
            if (null == this.items)
                return;
            var viewer = GetViewPosition(this.avatar.Position);
            // If the player has moved to a new spot on the grid, restart our outward walk.
            if (viewer != this.lastViewer) {
                this.lastViewer = viewer;
                this.listPos = 0;
            }

            // Figure out where the player is in our rolling grid.
            var gridPos = new Coord(
                this.gridHalf + viewer.X % this.gridSize,
                this.gridHalf + viewer.Y % this.gridSize);
            // Now offset that with the position being updated.
            gridPos += this.distanceList[this.listPos].Offset;
            // Bring it back into bounds.
            gridPos = new Coord(
                gridPos.X + this.gridSize % this.gridSize,
                gridPos.Y + this.gridSize % this.gridSize);

            var item = GetItem(gridPos);
            var pos = item.GridPosition;

            var x = pos.X;
            if (viewer.X - pos.X > this.gridHalf)
                x += this.gridSize;
            else if (pos.X - viewer.X > this.gridHalf)
                x -= this.gridSize;

            var y = pos.Y;
            if (viewer.Y - pos.Y > this.gridHalf)
                y += this.gridSize;
            else if (pos.Y - viewer.Y > this.gridHalf)
                y -= this.gridSize;

            pos = new Coord(x, y) + viewer + this.distanceList[this.listPos].Offset;
            var dist = Math.Max(Math.Abs(pos.X - viewer.X), Math.Abs(pos.Y - viewer.Y));
            item.Set(pos, dist);
            item.Update(stopAt);

            if (item.IsReady) {
                this.listPos++;
                // If we reach the outer ring, move back to the center and begin again.
                if (this.distanceList[this.listPos].DistanceInt > this.gridHalf)
                    this.listPos = 0;
            }
        }

        public void Render() {
            this.items.ForEach(item => item.Render());
        }

        private IGridData GetItem(Coord c) {
            //No more dicey pointer arithmetic. C# is a more awesome language than C++!
            return this.items[(c.X % this.gridSize) + (c.Y % this.gridSize) * this.gridSize];
        }

        private Coord GetViewPosition(Vector3 eye) {
            return new Coord(
                (int) (eye.X / this.itemSize),
                (int) (eye.Y / this.itemSize));
        }

        private void DoList() {
            this.listReady = true;
            this.distanceList.Capacity = GridUtils.TABLE_SIZE * GridUtils.TABLE_SIZE;
            //  foo2.resize (GridUtils.TABLE_SIZE *  GridUtils.TABLE_SIZE);
            var i = 0;
            for (var x = 0; x < GridUtils.TABLE_SIZE; x++) {
                for (var y = 0; y < GridUtils.TABLE_SIZE; y++) {
                    var d = this.distanceList[i];
                    d.Offset = new Coord(
                        x - GridUtils.TABLE_HALF,
                        y - GridUtils.TABLE_HALF);
                    var toCenter = new Vector2(d.Offset.X, d.Offset.Y);
                    d.DistanceFloat = toCenter.Length;
                    d.DistanceInt = (int) d.DistanceFloat;
                    i++;
                }
            }
            this.distanceList.Sort((left, right) => left.DistanceFloat.CompareTo(right.DistanceFloat));
        }
    }
}

