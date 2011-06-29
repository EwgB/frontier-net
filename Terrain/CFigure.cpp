/*-----------------------------------------------------------------------------

  Figure.cpp

-------------------------------------------------------------------------------

  Animated models.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "cfigure.h"
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

  unsigned    i;

  for (i = 0; i < BONE_COUNT; i++) 
    _bone_index[i] = BONE_INVALID;
  PushBone (BONE_ORIGIN, BONE_ORIGIN, glVector (0.0f, 0.0f, 0.0f));

}

void CFigure::Animate (CAnim* anim, float delta)
{

  unsigned      frame;

  frame = (unsigned)(delta * (float)anim->Frames ());
  frame %= anim->Frames ();
  for (unsigned i = 0; i < anim->Joints (); i++) 
     RotateBone (anim->Id (frame, i), anim->Rotation (frame, i));

}

void CFigure::RotateBone (unsigned id, GLvector angle)
{

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
    _skin._vertex[index] = glMatrixTransformPoint (m, _skin._vertex[index] - offset) + offset;
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

  _skin = _skin_static;
  for (i = 1; i < _bone.size (); i++) 
    _bone[i]._position = _bone[i]._origin;
  for (i = _bone.size () - 1; i > 0; i--) {
    b = &_bone[i];
    if (b->_rotation == glVector (0.0f, 0.0f, 0.0f))
      continue;
    m.Identity ();
    m.Rotate (b->_rotation.x, 1.0f, 0.0f, 0.0f);
    m.Rotate (b->_rotation.z, 0.0f, 0.0f, 1.0f);
    m.Rotate (b->_rotation.y, 0.0f, 1.0f, 0.0f);
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

void CFigure::PushBone (unsigned id, unsigned parent, GLvector pos)
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

void CFigure::Render ()
{

  unsigned    i;
  unsigned    parent;

  glLineWidth (17.0f);
  glColor3f (1,1,1);
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
  glLineWidth (1.0f);
  glColor3f (1,1,1);
  _skin.Render ();
  glPopMatrix ();
  
}


bool CFigure::LoadX (char* filename)
{

  char*           buffer;
  char*           token;
  char*           find;
  bool            done;
  long            size;
  unsigned        count;
  unsigned        i;
  GLvector        pos;
  int             i1, i2, i3, i4;
  int             poly;

  buffer = FileLoad (filename, &size);
  if (!buffer)
    return false;
  _strupr (buffer);
  token = strtok (buffer, NEWLINE);
  done = false;
  while (!done) {
    //We begin reading the vertex positions
    if (find = strstr (token, "MESH ")) {
      token = strtok (NULL, NEWLINE);
      count = atoi (token);
      for (i = 0; i < count; i++) {
        token = strtok (NULL, NEWLINE);
        clean_chars (token, ";,");
        sscanf (token, "%f %f %f", &pos.x, &pos.y, &pos.z);
        _skin_static.PushVertex (pos, glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
      }
      //Directly after the verts are the polys
      token = strtok (NULL, NEWLINE);
      count = atoi (token);
      for (i = 0; i < count; i++) {
        token = strtok (NULL, NEWLINE);
        clean_chars (token, ";,");
        poly = atoi (token);
        if (poly == 3) {
          sscanf (token + 2, "%d %d %d", &i1, &i2, &i3);
          _skin_static.PushTriangle (i1, i2, i3);
        } else if (poly == 4) {
          sscanf (token + 2, "%d %d %d %d", &i1, &i2, &i3, &i4);
          _skin_static.PushQuad (i1, i2, i3, i4);
        }
      }
    }
    //Reading the Normals
    if (find = strstr (token, "MeshNormals ")) {
      token = strtok (NULL, NEWLINE);
      clean_chars (token, ";,");
      count = atoi (token);
      for (i = 0; i < count; i++) {
        token = strtok (NULL, NEWLINE);
        sscanf (token, "%f %f %f", &pos.x, &pos.y, &pos.z);
        _skin_static._normal[i] = pos;
      }
    }
    //Reading the UV values
    if (find = strstr (token, "MeshTextureCoords ")) {
      token = strtok (NULL, NEWLINE);
      clean_chars (token, ";,");
      count = atoi (token);
      for (i = 0; i < count; i++) {
        token = strtok (NULL, NEWLINE);
        sscanf (token, "%f %f %f", &pos.x, &pos.y, &pos.z);
        _skin_static._normal[i] = pos;
      }
    }
    token = strtok (NULL, NEWLINE);
    if (!token)
      done = true;
  }
  free (buffer);
  return true;

}