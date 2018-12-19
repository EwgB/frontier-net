namespace FrontierSharp.Animation {
    using Common.Animation;

    public struct BWeight {
        public int Index;
        public float Weight;
    }

    public struct PWeight {
        public BoneId Bone;
        public float Weight;
    }
}
