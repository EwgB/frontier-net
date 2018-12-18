namespace FrontierSharp.Animation
{
    using System.Collections.Generic;

    using Common.Animation;
    using Common.Util;

    using OpenTK;

    public struct Bone
    {
        // TODO: Should really make this immutable, but would need a refactoring of Figure.cs
        public BoneId Id;
        public BoneId IdParent;
        public Vector3 Origin;
        public Vector3 Position;
        public Vector3 Rotation;
        public Color3 Color;
        public List<BoneId> Children;
        public List<BWeight> VertexWeights;
        public Matrix3 Matrix;
    }
}
