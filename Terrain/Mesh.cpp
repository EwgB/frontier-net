/*-----------------------------------------------------------------------------

  glMesh.cpp

  2011 Shamus Young

-------------------------------------------------------------------------------
  
  This class is used for storing groups of verts and polygons.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

void GLmesh::PushTriangle (UINT i1, UINT i2, UINT i3)
{

  _index.push_back (i1);
  _index.push_back (i2);
  _index.push_back (i3);

}

void GLmesh::PushQuad (UINT i1, UINT i2, UINT i3, UINT i4)
{

  PushTriangle (i1, i2, i3);
  PushTriangle (i1, i3, i4);

}

void GLmesh::PushVertex (GLvector vert, GLvector normal, GLvector2 uv)
{

  _bbox.ContainPoint (vert);
  _vertex.push_back (vert);
  _normal.push_back (normal);
  _uv.push_back (uv);

}

void GLmesh::PushVertex (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv)
{

  _bbox.ContainPoint (vert);
  _vertex.push_back (vert);
  _normal.push_back (normal);
  _color.push_back (color);
  _uv.push_back (uv);

}

void GLmesh::Clear () 
{

  _bbox.Clear ();
  _vertex.clear ();
  _normal.clear ();
  _uv.clear ();
  _index.clear ();

}

void GLmesh::Render ()
{

  unsigned      i;

  glBegin (GL_TRIANGLES);
  for (i = 0; i < _index.size (); i++) {
    glNormal3fv (&_normal[_index[i]].x);
    glTexCoord2fv (&_uv[_index[i]].x);
    glVertex3fv (&_vertex[_index[i]].x);
  }
  glEnd ();

}

void GLmesh::RecalculateBoundingBox ()
{

  _bbox.Clear ();
  for (unsigned i = 0; i < Vertices (); i++)
    _bbox.ContainPoint (_vertex[i]);


}

void GLmesh::CalculateNormals ()
{
  GLvector    edge[3];
  unsigned    i;
  float       dot;
  float       angle[3];
  unsigned    index;
  unsigned    i0, i1, i2;
  GLvector    normal;

  //Clear any existing normals
  for (i = 0; i < _normal.size (); i++) 
    _normal[i] = glVector (0.0f, 0.0f, 0.0f);
  //For each triangle... 
  for (i = 0; i < Triangles (); i++) {
    index = i * 3;
    i0 = _index[index];
    i1 = _index[index + 1];
    i2 = _index[index + 2];
    // Convert the 3 edges of the polygon into vectors 
    edge[0] = _vertex[i0] - _vertex[i1];
    edge[1] = _vertex[i1] - _vertex[i2];
    edge[2] = _vertex[i2] - _vertex[i0];
    // normalize the vectors 
    edge[0].Normalize ();
    edge[1].Normalize ();
    edge[2].Normalize ();
    // now get the normal from the cross product of any two of the edge vectors 
    normal = glVectorCrossProduct (edge[2], edge[0] * -1);
    normal.Normalize ();
    //calculate the 3 internal angles of this triangle.
    dot = glVectorDotProduct (edge[2], edge[0]);
    angle[0] = acos(-dot);
    if (_isnan (angle[0]))
      continue;
    angle[1] = acos(-glVectorDotProduct (edge[0], edge[1]));
    if (_isnan (angle[1]))
      continue;
    angle[2] = PI - (angle[0] + angle[1]);
    //Now weight each normal by the size of the angle so that the triangle 
    //with the largest angle at that vertex has the most influence over the 
    //direction of the normal.
    _normal[i0] += normal * angle[0];
    _normal[i1] += normal * angle[1];
    _normal[i2] += normal * angle[2];
  }
  //Re-normalize. Done.
  for (i = 0; i < _normal.size (); i++) 
    _normal[i].Normalize ();

}


void GLmesh::CalculateNormalsSeamless ()
{
  GLvector          edge[3];
  unsigned          i, j;
  float             dot;
  float             angle[3];
  unsigned          index;
  unsigned          i0, i1, i2;
  GLvector          normal;
  vector<UINT>      merge_index;
  vector<GLvector>  verts_merged;
  vector<GLvector>  normals_merged;
  unsigned          found;

  //Clear any existing normals
  for (i = 0; i < _normal.size (); i++) 
    normals_merged.push_back (glVector (0.0f, 0.0f, 0.0f));
  
  // scan through the vert list, and make an alternate list where
  // verticies that share the same location are merged
  for (i = 0; i < _vertex.size (); i++) {
    found = -1;
    //see if there is another vertex in the same position in the merged list
    for (j = 0; j < merge_index.size (); j++) {
      if (_vertex[i] == _vertex[merge_index[j]]) {
        merge_index.push_back (j);
        verts_merged.push_back (_vertex[i]);
        found = j;
        break;
      }
    }
    //vertex not found, so add another
    if (found == -1) {
      merge_index.push_back (verts_merged.size ());
      verts_merged.push_back (_vertex[i]);
    }
  }
  //For each triangle... 
  for (i = 0; i < Triangles (); i++) {
    index = i * 3;
    i0 = merge_index[_index[index]];
    i1 = merge_index[_index[index + 1]];
    i2 = merge_index[_index[index + 2]];
    // Convert the 3 edges of the polygon into vectors 
    edge[0] = verts_merged[i0] - verts_merged[i1];
    edge[1] = verts_merged[i1] - verts_merged[i2];
    edge[2] = verts_merged[i2] - verts_merged[i0];
    // normalize the vectors 
    edge[0].Normalize ();
    edge[1].Normalize ();
    edge[2].Normalize ();
    // now get the normal from the cross product of any two of the edge vectors 
    normal = glVectorCrossProduct (edge[2], edge[0] * -1);
    normal.Normalize ();
    //calculate the 3 internal angles of this triangle.
    dot = glVectorDotProduct (edge[2], edge[0]);
    angle[0] = acos(-dot);
    if (_isnan (angle[0]))
      continue;
    angle[1] = acos(-glVectorDotProduct (edge[0], edge[1]));
    if (_isnan (angle[1]))
      continue;
    angle[2] = PI - (angle[0] + angle[1]);
    //Now weight each normal by the size of the angle so that the triangle 
    //with the largest angle at that vertex has the most influence over the 
    //direction of the normal.
    normals_merged[i0] += normal * angle[0];
    normals_merged[i1] += normal * angle[1];
    normals_merged[i2] += normal * angle[2];
  }
  //Re-normalize. Done.
  for (i = 0; i < _normal.size (); i++) {
    _normal[i] = normals_merged[merge_index[i]];
    _normal[i].z *= NORMAL_SCALING;
    _normal[i].Normalize ();
  }

}
