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
  PushBone (BONE_ROOT, BONE_ROOT, glVector (0.0f, 0.0f, 0.0f));
  _unknown_count = 0;

}

void CFigure::Animate (CAnim* anim, float delta)
{

  unsigned      frame;

  frame = (unsigned)(delta * (float)anim->Frames ());
  frame %= anim->Frames ();
  for (unsigned i = 0; i < anim->Joints (); i++) 
     RotateBone ((BoneId)anim->Id (frame, i), anim->Rotation (frame, i));

}

//We take a string and turn it into a BoneId, using unknowns as needed
BoneId CFigure::IdentifyBone (char* name)
{
  
  BoneId    bid;

  bid = CAnim::BoneFromString (name);
  //If CAnim couldn't make sense of the name, or if that id is already in use...
  if (bid == BONE_INVALID || _bone_index[bid] != BONE_INVALID) {
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
  glTranslatef (_position.x, _position.y, _position.z); 
  _skin.Render ();
  glDisable (GL_DEPTH_TEST);
  glDisable (GL_TEXTURE_2D);
  glDisable (GL_LIGHTING);
  for (i = 0; i < _bone.size (); i++) {
    parent = _bone_index[_bone[i]._id_parent];
    if (!parent)
      continue;
    glColor3fv (&_bone[i]._color.red);
    glBegin (GL_LINES);
    glVertex3fv (&_bone[i]._position.x);
    glVertex3fv (&_bone[parent]._position.x);
    glEnd ();
  }
  glLineWidth (1.0f);
  glEnable (GL_LIGHTING);
  glEnable (GL_TEXTURE_2D);
  glEnable (GL_DEPTH_TEST);
  glPopMatrix ();
  
}


bool CFigure::LoadX (char* filename)
{

  char*             buffer;
  char*             token;
  char*             find;
  bool              done;
  long              size;
  unsigned          count;
  unsigned          i;
  GLvector          pos;
  int               i1, i2, i3, i4;
  int               poly;
  vector<BoneId>    hierarchy;
  bool              skel_done;
  BoneId            bone;
  BoneId            parent_id;
  unsigned          frame_depth;
  GLmatrix          matrix;
  vector<GLmatrix>  matrix_stack;

  buffer = FileLoad (filename, &size);
  if (!buffer)
    return false;
  _strupr (buffer);
  token = strtok (buffer, NEWLINE);
  done = false;
  skel_done = false;
  while (!done) {
    //Read the skeleton
    if ((!skel_done) && (find = strstr (token, "FRAME "))) {
      GLvector pusher = glVector (0.0f, 0.0f, 0.0f);
      frame_depth = 0;
      _bone[_bone_index[BONE_ROOT]]._matrix.Identity ();
      while (!skel_done) {
        if (find = strstr (token, "}")) {
          frame_depth--;
          hierarchy.pop_back ();
        }
        if (find = strstr (token, "FRAMETRANSFORMMATRIX ")) {
          //We don't care about the rotational data
          matrix.Identity ();
          token = strtok (NULL, NEWLINE);
          clean_chars (token, ";,");
          sscanf (token, "%f %f %f", &matrix.elements[0][0], &matrix.elements[0][1], &matrix.elements[0][2], &matrix.elements[0][3]);
          token = strtok (NULL, NEWLINE);
          clean_chars (token, ";,");
          sscanf (token, "%f %f %f", &matrix.elements[1][0], &matrix.elements[1][1], &matrix.elements[1][2], &matrix.elements[1][3]);
          token = strtok (NULL, NEWLINE);
          clean_chars (token, ";,");
          sscanf (token, "%f %f %f", &matrix.elements[2][0], &matrix.elements[2][1], &matrix.elements[2][2], &matrix.elements[2][3]);
          //Grab the positional data
          token = strtok (NULL, NEWLINE);
          clean_chars (token, ";,");
          sscanf (token, "%f %f %f", &matrix.elements[3][0], &matrix.elements[3][1], &matrix.elements[3][2], &matrix.elements[3][3]);
          sscanf (token, "%f %f %f", &pos.x, &pos.y, &pos.z);
          //only add this to our model if it's actually a known bone
          bone = hierarchy.back ();
          if (bone != BONE_INVALID) {
            //Find the last valid bone in the chain.
            vector<BoneId>::reverse_iterator rit;
            parent_id = BONE_ROOT;
            for (rit = hierarchy.rbegin(); rit < hierarchy.rend(); ++rit) {
              if (*rit != BONE_INVALID && *rit != bone) {
                  parent_id = *rit;
                  break;
              }
            }
            
            pos = glVector (0.0f, 0.0f, 0.0f);
            //pos.x = matrix.elements[3][0];
            //pos.y = matrix.elements[3][1];
            //pos.z = matrix.elements[3][2];
            
            //pos = glMatrixTransformPoint (matrix, pos);
            /*
            for (rit = hierarchy.rbegin(); rit < hierarchy.rend(); ++rit) {
              if (*rit != BONE_INVALID && *rit != bone) {
                pos = glMatrixTransformPoint (_bone[_bone_index[*rit]]._matrix, pos);
                break;
              }
            }
            */
            if (bone != BONE_ROOT) {
              matrix = glMatrixMultiply (_bone[_bone_index[parent_id]]._matrix, matrix);
              //pos = pos - _bone[_bone_index[parent_id]]._position;
              pos = glMatrixTransformPoint (_bone[_bone_index[parent_id]]._matrix, pos);
              //pos = glMatrixTransformPoint (matrix, _bone[_bone_index[parent_id]]._position);
              //pos = glMatrixTransformPoint (matrix, pos) + _bone[_bone_index[parent_id]]._position;
              //pos += _bone[_bone_index[parent_id]]._position;
            }

            //matrix = glMatrixMultiply (matrix,  _bone[_bone_index[parent_id]]._matrix);
            
            pos += pusher;
            //pusher += glVector (0.0f, 0.0f, 0.01f);
            //pos += _bone[_bone_index[parent_id]]._position ;
            PushBone (bone, parent_id, pos);
            _bone[_bone_index[bone]]._matrix = matrix;
          }
          //Now plow through until we find the closing brace
          while (!(find = strstr (token, "}"))) 
            token = strtok (NULL, NEWLINE);
        }
        if (find = strstr (token, "FRAME ")) {
          frame_depth++;
          bone = IdentifyBone (find + 6);
          //bone = CAnim::BoneFromString (find + 6);
          hierarchy.push_back (bone);
        }
        token = strtok (NULL, NEWLINE);
        if (find = strstr (token, "MESH ")) 
          skel_done = true;
      }
    }
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
          _skin_static.PushTriangle (i3, i2, i1);
        } else if (poly == 4) {
          sscanf (token + 2, "%d %d %d %d", &i1, &i2, &i3, &i4);
          _skin_static.PushQuad (i4, i3, i2, i1);
        }
      }
    }
    //Reading the Normals
    if (find = strstr (token, "MESHNORMALS ")) {
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
    if (find = strstr (token, "MESHTEXTURECOORDS ")) {
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