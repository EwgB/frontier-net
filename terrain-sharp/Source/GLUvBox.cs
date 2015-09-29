namespace terrain_sharp.Source {
	using OpenTK;

	///<summary>This class is used for storing and and manipULating UV texture coords.</summary>
	class GLUvBox {
		public Vector2 UL { get; set; }
		public Vector2 LR { get; set; }
		public Vector2 Center { get { return (UL + LR) / 2; } }

		public void Set(Vector2 ul, Vector2 lr) {
			UL = ul;
			LR = lr;
		}

		public void Set(int x, int y, int columns, int rows) {
			Vector2 frame_size = new Vector2(1.0f / columns, 1.0f / rows);

			UL = new Vector2(x * frame_size.X, y * frame_size.Y);
			LR = new Vector2((x + 1) * frame_size.X, (y + 1) * frame_size.Y);
		}

		public void Set(float repeats) {
			UL = new Vector2(0, 0);
			LR = new Vector2(repeats, repeats);
		}

		public Vector2 Corner(UvBoxPosition position) {
			switch (position) {
				case UvBoxPosition.TopLeft:
					return UL;
				case UvBoxPosition.TopRight:
					return new Vector2(LR.X, UL.Y);
				case UvBoxPosition.BottomRight:
					return LR;
				case UvBoxPosition.BottomLeft:
					return new Vector2(UL.X, LR.Y);
				case UvBoxPosition.LeftEdge:
					return new Vector2(UL.X, (UL.Y + LR.Y) / 2);
				case UvBoxPosition.RightEdge:
					return new Vector2(LR.X, (UL.Y + LR.Y) / 2);
				case UvBoxPosition.TopEdge:
					return new Vector2((UL.X + LR.X) / 2, UL.Y);
				case UvBoxPosition.BottomEdge:
					return new Vector2((UL.X + LR.X) / 2, LR.Y);
			}
			return new Vector2(0, 0);
		}
	}
}
