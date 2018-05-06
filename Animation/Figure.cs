namespace FrontierSharp.Animation {
    using OpenTK;

    using Common.Animation;

    internal class Figure : IFigure {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public void Render() {
            // TODO
            /*
            glColor3f(1, 1, 1);
            glPushMatrix();
            CgSetOffset(_position);
            glTranslatef(_position.x, _position.y, _position.z);
            CgUpdateMatrix();
            _skin_render.Render();
            CgSetOffset(Vector3(0, 0, 0));
            glPopMatrix();
            CgUpdateMatrix();
            */
        }

        public void Animate(IAnimation animation, float delta) {
            // TODO
            //AnimJoint aj;

            //if (delta > 1.0f)
            //    delta -= (float)((int)delta);
            //aj = animation.GetFrame(delta);
            //for (var i = 0; i < animation.Joints(); i++)
            //    RotateBone(aj[i].id, aj[i].rotation);
        }

        public void RenderSkeleton() {
            // TODO
            /*
            int i;
            int parent;

            glLineWidth(12.0f);
            glPushMatrix();
            glTranslatef(_position.x, _position.y, _position.z);
            CgUpdateMatrix();
            glDisable(GL_DEPTH_TEST);
            glDisable(GL_TEXTURE_2D);
            glDisable(GL_LIGHTING);
            for (i = 0; i < _bone.size(); i++) {
                parent = _bone_index[_bone[i]._id_parent];
                if (!parent)
                    continue;
                glColor3fv(&_bone[i]._color.red);
                glBegin(GL_LINES);
                Vector3 p = _bone[i]._position;
                glVertex3fv(&_bone[i]._position.x);
                glVertex3fv(&_bone[parent]._position.x);
                glEnd();
            }
            glLineWidth(1.0f);
            glEnable(GL_LIGHTING);
            glEnable(GL_TEXTURE_2D);
            glEnable(GL_DEPTH_TEST);
            glPopMatrix();
            CgUpdateMatrix();

        */
        }

        public void Update() {
            // TODO
            //int i;
            //int c;
            //GLmatrix m;
            //Bone* b;
            //vector<Bone>::reverse_iterator rit;

            //_skin_render = _skin_deform;
            //for (i = 1; i < _bone.size(); i++)
            //    _bone[i]._position = _bone[i]._origin;
            //for (rit = _bone.rbegin(); rit < _bone.rend(); ++rit) {
            //    b = &(*rit);
            //    if (b._rotation == Vector3(0.0f, 0.0f, 0.0f))
            //        continue;
            //    m.Identity();
            //    m.Rotate(b._rotation.x, 1.0f, 0.0f, 0.0f);
            //    m.Rotate(b._rotation.z, 0.0f, 0.0f, 1.0f);
            //    m.Rotate(b._rotation.y, 0.0f, 1.0f, 0.0f);
            //    RotatePoints(b._id, b._position, m);
            //    for (c = 0; c < b._children.size(); c++) {
            //        //Root is self-parent, but shouldn't rotate self!
            //        if (b._children[c])
            //            RotateHierarchy(b._children[c], b._position, m);
            //    }
            //}
        }
    }
}

/*From CFigure.h
 * 
 * #ifndef CANIM_H
#include "canim.h"
#endif

struct BWeight
{
  int          _index;
  float             _weight;
};


struct PWeight
{
  BoneId            _bone;
  float             _weight;
};

struct Bone
{
  BoneId            _id;
  BoneId            _id_parent;
  Vector3          _origin;
  Vector3          _position;
  Vector3          _rotation;
  GLrgba            _color;
  vector<int>  _children;
  vector<BWeight>   _vertex_weights;
  GLmatrix          _matrix;
};

class CFigure
{
  void              RotateHierarchy (int id, Vector3 offset, GLmatrix m);
  void              RotatePoints (int id, Vector3 offset, GLmatrix m);
public:
  vector<Bone>      _bone;
  Vector3          _position;
  Vector3          _rotation;
  int          _bone_index[BONE_COUNT];
  int          _unknown_count;
  GLmesh            _skin_static;//The original, "read only"
  GLmesh            _skin_deform;//Altered
  GLmesh            _skin_render;//Updated every frame
  

  CFigure ();
  void              Clear ();
  bool              LoadX (char* filename);
  BoneId            IdentifyBone (char* name);
  void              PositionSet (Vector3 pos) { _position = pos; };
  void              RotationSet (Vector3 rot);
  void              RotateBone (BoneId id, Vector3 angle);
  void              PushBone (BoneId id, int parent, Vector3 pos);
  void              PushWeight (int id, int index, float weight);
  void              Prepare ();
  void              BoneInflate (BoneId id, float distance, bool do_children);

  GLmesh*           Skin () { return &_skin_static; };
};
*/

/* From CFigure.cpp

#define NEWLINE   "\n"

static void clean_chars(char* target, char* chars) {

    int i;
    char* c;

    for (i = 0; i < strlen(chars); i++) {
        while (c = strchr(target, chars[i]))
            *c = ' ';
    }

}

CFigure::CFigure ()
{

  Clear();

}

void CFigure::Clear() {

    int i;

    for (i = 0; i < BONE_COUNT; i++)
        _bone_index[i] = BONE_INVALID;
    _unknown_count = 0;
    _skin_static.Clear();
    _skin_deform.Clear();
    _skin_render.Clear();
    _bone.clear();


}

//We take a string and turn it into a BoneId, using unknowns as needed
BoneId CFigure::IdentifyBone(char* name) {

    BoneId bid;

    bid = CAnim::BoneFromString(name);
    //If CAnim couldn't make sense of the name, or if that id is already in use...
    if (bid == BONE_INVALID || _bone_index[bid] != BONE_INVALID) {
        ConsoleLog("Couldn't id Bone '%s'.", name);
        bid = (BoneId)(BONE_UNKNOWN0 + _unknown_count);
        _unknown_count++;
    }
    return bid;

}

void CFigure::RotateBone(BoneId id, Vector3 angle) {

    if (_bone_index[id] != BONE_INVALID)
        _bone[_bone_index[id]]._rotation = angle;

}

void CFigure::RotatePoints(int id, Vector3 offset, GLmatrix m) {

    Bone* b;
    int i;
    int index;


    b = &_bone[_bone_index[id]];
    for (i = 0; i < b._vertex_weights.size(); i++) {
        index = b._vertex_weights[i]._index;
        _skin_render._vertex[index] = glMatrixTransformPoint(m, _skin_render._vertex[index] - offset) + offset;
        //from = _skin_render._vertex[index] - offset;
        //to = glMatrixTransformPoint (m, from);
        //movement = movement - _skin_static._vertex[index]; 
        //_skin_render._vertex[index] = Vector3Interpolate (from, to, b._vertex_weights[i]._weight) + offset;
    }

}

void CFigure::RotateHierarchy(int id, Vector3 offset, GLmatrix m) {

    Bone* b;
    int i;

    b = &_bone[_bone_index[id]];
    b._position = glMatrixTransformPoint(m, b._position - offset) + offset;
    RotatePoints(id, offset, m);
    for (i = 0; i < b._children.size(); i++) {
        if (b._children[i])
            RotateHierarchy(b._children[i], offset, m);
    }

}

void CFigure::PushWeight(int id, int index, float weight) {

    BWeight bw;

    bw._index = index;
    bw._weight = weight;
    _bone[_bone_index[id]]._vertex_weights.push_back(bw);

}

void CFigure::PushBone(BoneId id, int parent, Vector3 pos) {

    Bone b;

    _bone_index[id] = _bone.size();
    b._id = (BoneId)id;
    b._id_parent = (BoneId)parent;
    b._position = pos;
    b._origin = pos;
    b._rotation = Vector3(0.0f, 0.0f, 0.0f);
    b._children.clear();
    b._color = glRgbaUnique(id + 1);
    _bone.push_back(b);
    _bone[_bone_index[parent]]._children.push_back(id);

}

void CFigure::BoneInflate(BoneId id, float distance, bool do_children) {

    Bone* b;
    int i;
    int c;
    int index;

    b = &_bone[_bone_index[id]];
    for (i = 0; i < b._vertex_weights.size(); i++) {
        index = b._vertex_weights[i]._index;
        _skin_deform._vertex[index] = _skin_static._vertex[index] + _skin_static._normal[index] * distance;
    }
    if (!do_children)
        return;
    for (c = 0; c < b._children.size(); c++)
        BoneInflate((BoneId)b._children[c], distance, do_children);


}

void CFigure::RotationSet(Vector3 rot) {

    _bone[_bone_index[BONE_ROOT]]._rotation = rot;

}

void CFigure::Prepare() {

    _skin_deform = _skin_static;

}

bool CFigure::LoadX(char* filename) {

    FileXLoad(filename, this);
    Prepare();
    return true;

}
*/
