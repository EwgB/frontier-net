namespace FrontierSharp.Animation
{
    using System.Collections.Generic;

    using Common.Util;

    using OpenTK;

    public struct Bone
    {
        public BoneId Id;
        public BoneId IdParent;
        public Vector3 Origin;
        public Vector3 Position;
        public Vector3 Rotation;
        public Color3 Color;
        public List<int> Children;
        public List<BWeight> VertexWeights;
        public Matrix3 Matrix;
    }
}
