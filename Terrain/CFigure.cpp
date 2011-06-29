/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "cfigure.h"


Figure::Figure ()
{

  unsigned    i;

  for (i = 0; i < BONE_COUNT; i++) 
    _bone_index[i] = BONE_INVALID;
  PushBone (BONE_ORIGIN, BONE_ORIGIN, glVector (0.0f, 0.0f, 0.0f));

}

void Figure::RotateBone (unsigned id, GLvector angle)
{

  _bone[_bone_index[id]]._rotation = angle;

}

void Figure::RotatePoints (unsigned id, GLvector offset, GLmatrix m)
{

  Bone*       b;
  unsigned    i;
  unsigned    index;
  
  b = &_bone[_bone_index[id]];
  for (i = 0; i < b->_vertex_weights.size (); i++) {
    index = b->_vertex_weights[i]._index;
    _skin._vertex[index] = glMatrixTransformPoint (m, _skin._vertex[index] - offset) + offset;
  }

}

void Figure::RotateHierarchy (unsigned id, GLvector offset, GLmatrix m)
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

void Figure::Update ()
{

  unsigned    i;
  unsigned    c;
  GLmatrix    m;
  Bone*       b;

  _skin = _skin_static;
  for (i = 1; i < _bone.size (); i++) 
    _bone[i]._position = _bone[i]._origin;
  for (i = _bone.size () - 1; i > 0; i--) {
    b = &_bone[i];
    if (b->_rotation == glVector (0.0f, 0.0f, 0.0f))
      continue;
    m.Identity ();
    m.Rotate (b->_rotation.x, 1.0f, 0.0f, 0.0f);
    m.Rotate (b->_rotation.y, 0.0f, 1.0f, 0.0f);
    m.Rotate (b->_rotation.z, 0.0f, 0.0f, 1.0f);
    RotatePoints (b->_id, b->_position, m);
    for (c = 0; c < b->_children.size (); c++) 
      RotateHierarchy (b->_children[c], b->_position, m);
  }
  
}

void Figure::PushWeight (unsigned id, unsigned index, float weight)
{

  BWeight   bw;

  bw._index = index;
  bw._weight = weight;  
  _bone[_bone_index[id]]._vertex_weights.push_back (bw);

}

void Figure::PushBone (unsigned id, unsigned parent, GLvector pos)
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
  if (parent) 
    _bone[_bone_index[parent]]._children.push_back (id);

}

void Figure::Render ()
{

  unsigned    i;
  unsigned    parent;

  glLineWidth (17.0f);
  glPushMatrix ();
  //glTranslatef (-_position.x, -_position.y, -_position.z); 
  glTranslatef (_position.x, _position.y, _position.z); 
  glBegin (GL_LINES);
  for (i = 1; i < _bone.size (); i++) {
    parent = _bone_index[_bone[i]._id_parent];
    if (!parent)
      continue;
    glColor3fv (&_bone[i]._color.red);
    glVertex3fv (&_bone[i]._position.x);
    glVertex3fv (&_bone[parent]._position.x);
  }
  glEnd ();
  _skin.Render ();
  glPopMatrix ();
  
}
