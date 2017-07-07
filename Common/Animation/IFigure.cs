namespace FrontierSharp.Common.Animation {
    using OpenTK;

    /// <summary>Animated model.</summary>
    public interface IFigure : IRenderable {
        Vector3 Position { get; set; }
        Vector3 Rotation { get; set; }

        void Animate (IAnimation animation, float delta);
        void RenderSkeleton();
        void Update();
    }
}

/*
  void              RotateHierarchy (unsigned id, GLvector offset, GLmatrix m);
  void              RotatePoints (unsigned id, GLvector offset, GLmatrix m);
public:
  vector<Bone>      _bone;
  GLvector          _position;
  GLvector          _rotation;
  unsigned          _bone_index[BONE_COUNT];
  unsigned          _unknown_count;
  GLmesh            _skin_static;//The original, "read only"
  GLmesh            _skin_deform;//Altered
  GLmesh            _skin_render;//Updated every frame
  
  void              Clear ();
  bool              LoadX (char* filename);
  BoneId            IdentifyBone (char* name);
  void              RotateBone (BoneId id, GLvector angle);
  void              PushBone (BoneId id, unsigned parent, GLvector pos);
  void              PushWeight (unsigned id, unsigned index, float weight);
  void              Prepare ();
  void              BoneInflate (BoneId id, float distance, bool do_children);

  GLmesh*           Skin () { return &_skin_static; };
 */
