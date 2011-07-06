/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "console.h"
#include "cfigure.h"
#include "cg.h"
#include "file.h"

#define NEWLINE   "\n"

static void clean_chars (char* target, char* chars)
{

  unsigned    i;
  char*       c;

  for (i = 0; i < strlen (chars); i++) {
    while (c = strchr (target, chars[i]))
      *c = ' ';
  }

}

CFigure::CFigure ()
{

  Clear ();

}

void CFigure::Clear ()
{

  unsigned    i;

  for (i = 0; i < BONE_COUNT; i++) 
    _bone_index[i] = BONE_INVALID;
  _unknown_count = 0;
  _skin_static.Clear ();
  _skin_deform.Clear ();
  _skin_render.Clear ();
  _bone.clear ();


}

void CFigure::Animate (CAnim* anim, float delta)
{

  AnimJoint*    aj;
  
  if (delta > 1.0f)
    delta -= (float)((int)delta);
  aj = anim->GetFrame (delta);
  for (unsigned i = 0; i < anim->Joints (); i++) 
     RotateBone (aj[i].id, aj[i].rotation);



}

//We take a string and turn it into a BoneId, using unknowns as needed
BoneId CFigure::IdentifyBone (char* name)
{
  
  BoneId    bid;

  bid = CAnim::BoneFromString (name);
  //If CAnim couldn't make sense of the name, or if that id is already in use...
  if (bid == BONE_INVALID || _bone_index[bid] != BONE_INVALID) {
    ConsoleLog ("Couldn't id Bone '%s'.", name);
    bid = (BoneId)(BONE_UNKNOWN0 + _unknown_count);
    _unknown_count++;
  }
  return bid;

}

void CFigure::RotateBone (BoneId id, GLvector angle)
{

  if (_bone_index[id] != BONE_INVALID)
    _bone[_bone_index[id]]._rotation = angle;

}

void CFigure::RotatePoints (unsigned id, GLvector offset, GLmatrix m)
{

  Bone*       b;
  unsigned    i;
  unsigned    index;

  
  b = &_bone[_bone_index[id]];
  for (i = 0; i < b->_vertex_weights.size (); i++) {
    index = b->_vertex_weights[i]._index;
    _skin_render._vertex[index] = glMatrixTransformPoint (m, _skin_render._vertex[index] - offset) + offset;
    /*
    from = _skin_render._vertex[index] - offset;
    to = glMatrixTransformPoint (m, from);
    //movement = movement - _skin_static._vertex[index]; 
    _skin_render._vertex[index] = glVectorInterpolate (from, to, b->_vertex_weights[i]._weight) + offset;
    */
  }

}

void CFigure::RotateHierarchy (unsigned id, GLvector offset, GLmatrix m)
{

  Bone*       b;
  unsigned    i;

  b = &_bone[_bone_index[id]];
  b->_position = glMatrixTransformPoint (m, b->_position - offset) + offset;
  RotatePoints (id, offset, m);
  for (i = 0; i < b->_children.size (); i++) {
    if (b->_children[i])
      RotateHierarchy (b->_children[i], offset, m);
  }

}

void CFigure::Update ()
{

  unsigned    i;
  unsigned    c;
  GLmatrix    m;
  Bone*       b;

  _skin_render = _skin_deform;
  for (i = 1; i < _bone.size (); i++) 
    _bone[i]._position = _bone[i]._origin;

  vector<Bone>::reverse_iterator rit;
  for (rit = _bone.rbegin(); rit < _bone.rend(); ++rit) {
    b = &(*rit);
    if (b->_rotation == glVector (0.0f, 0.0f, 0.0f))
      continue;
    if (b->_id == BONE_ROOT)
      m.Identity ();

    m.Identity ();
    m.Rotate (b->_rotation.x, 1.0f, 0.0f, 0.0f);
    m.Rotate (b->_rotation.y, 0.0f, 1.0f, 0.0f);
    m.Rotate (b->_rotation.z, 0.0f, 0.0f, 1.0f);
    RotatePoints (b->_id, b->_position, m);
    for (c = 0; c < b->_children.size (); c++) 
      RotateHierarchy (b->_children[c], b->_position, m);
  }
  
}

void CFigure::PushWeight (unsigned id, unsigned index, float weight)
{

  BWeight   bw;

  bw._index = index;
  bw._weight = weight;  
  _bone[_bone_index[id]]._vertex_weights.push_back (bw);

}

void CFigure::PushBone (BoneId id, unsigned parent, GLvector pos)
{

  Bone    b;

  _bone_index[id] = _bone.size ();
  b._id = (BoneId)id;
  b._id_parent = (BoneId)parent;
  b._position = pos;
  b._origin = pos;
  b._rotation = glVector (0.0f, 0.0f, 0.0f);
  b._children.clear ();
  b._color = glRgbaUnique (id + 1);
  _bone.push_back (b);
  _bone[_bone_index[parent]]._children.push_back (id);

}

void CFigure::BoneInflate (BoneId id, float distance, bool do_children)
{

  Bone*       b;
  unsigned    i;
  unsigned    c;
  unsigned    index;

  b = &_bone[_bone_index[id]];
  for (i = 0; i < b->_vertex_weights.size (); i++) {
    index = b->_vertex_weights[i]._index;
    _skin_deform._vertex[index] = _skin_static._vertex[index] + _skin_static._normal[index] * distance;
  }
  if (!do_children)
    return;
  for (c = 0; c < b->_children.size (); c++) 
    BoneInflate ((BoneId)b->_children[c], distance, do_children);


}

void CFigure::RotationSet (GLvector rot)
{

  _bone[_bone_index[BONE_ROOT]]._rotation = rot;

}

void CFigure::Render ()
{
  
  glColor3f (1,1,1);
  glPushMatrix ();
  glTranslatef (_position.x, _position.y, _position.z); 
  CgUpdateMatrix ();
  _skin_render.Render ();
  glPopMatrix ();
  CgUpdateMatrix ();
  
}

void CFigure::RenderSkeleton ()
{

  unsigned    i;
  unsigned    parent;

  glLineWidth (12.0f);
  glPushMatrix ();
  glTranslatef (_position.x, _position.y, _position.z); 
  CgUpdateMatrix ();
  glDisable (GL_DEPTH_TEST);
  glDisable (GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  for (i = 0; i < _bone.size (); i++) {
    parent = _bone_index[_bone[i]._id_parent];
    if (!parent)
      continue;
    glColor3fv (&_bone[i]._color.red);
    glBegin (GL_LINES);
    GLvector p = _bone[i]._position;
    glVertex3fv (&_bone[i]._position.x);
    glVertex3fv (&_bone[parent]._position.x);
    glEnd ();
  }
  glLineWidth (1.0f);
  glEnable (GL_LIGHTING);
  glEnable (GL_TEXTURE_2D);
  glEnable (GL_DEPTH_TEST);
  glPopMatrix ();
  CgUpdateMatrix ();

}

void CFigure::Prepare ()
{

  _skin_deform = _skin_static;

}

bool CFigure::LoadX (char* filename)
{

  FileXLoad (filename, this);
  Prepare ();
  return true;

}