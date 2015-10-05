namespace terrain_sharp.Source.CFigure {
	using System.Collections.Generic;

	using OpenTK;
	using OpenTK.Graphics;

	using CAnim;

	class Bone {
		internal struct BWeight {
			public int _index;
			public float _weight;
		}

		public BoneId Id { get; set; }
		public BoneId IdParent { get; set; }
		public Vector3 Origin { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }
		public Color4 Color { get; set; }
		public List<int> Children { get; set; }
		public List<BWeight> VertexWeights { get; set; }
		public GLmatrix Matrix { get; set; }
	}
}
