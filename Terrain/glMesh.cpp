/*-----------------------------------------------------------------------------

  glUvbox.cpp

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

  _vertex.push_back (vert);
  _normal.push_back (normal);
  _uv.push_back (uv);

}

void GLmesh::Clear () 
{

  _vertex.clear ();
  _normal.clear ();
  _uv.clear ();
  _index.clear ();

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
    normal = glVectorCrossProduct (edge[2], edge[0] * 1);
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
    normal = glVectorCrossProduct (edge[2], edge[0] * 1);
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
    _normal[i].Normalize ();
  }

}


/*


rw_geometry* rw_geometry_calc_normals_seamless (rw_geometry* geometry)
{

  RwV3d*          verts;
  RwV3d*          verts_merged;
  RwV3d*          normals;
  RwV3d*          normals_merged;
  RwV3d           scaledNormal;
  int*            merge_index;
  int             merge_count;
  int             i;
  int             j;
  RwReal          length;
  RwV3d           edge[3];
  RwV3d           normal;
  RwReal          angle[3];
  RwReal          recip;
  int             tcount;
  int             vcount;
  RpTriangle*     triangle;
  RpMorphTarget*  keyFrame;
  float           dot;
  int             index0;
  int             index1;
  int             index2;

  keyFrame = RpGeometryGetMorphTarget ((RpGeometry*)geometry, 0);
  verts = RpMorphTargetGetVertices (keyFrame);
  normals = RpMorphTargetGetVertexNormals (keyFrame);
  vcount = RpGeometryGetNumVertices ((RpGeometry*)geometry);
  tcount = RpGeometryGetNumTriangles ((RpGeometry*)geometry);
  triangle = RpGeometryGetTriangles ((RpGeometry*)geometry);

  if (!normals || !vcount || !tcount) // there are no normals?
    return (rw_geometry*)NULL;

  verts_merged = (RwV3d*)calloc (vcount, sizeof (RwV3d));
  normals_merged = (RwV3d*)calloc (vcount, sizeof (RwV3d));
  merge_index = (int*)calloc (vcount, sizeof (int));
  merge_count = 0;

  // scan through the vert list, and make an alternate list where
  // verticies that share the same location are merged
  for (i = 0; i < vcount; i++) {
    merge_index[i] = -1;
    //see if there is another vertex in the same position in the merged list
    for (j = 0; j < merge_count; j++) {
      if (rw_v3d_compare (verts[i], verts_merged[j])) {
        merge_index[i] = j;
        break;
      }
    }
    //vertex not found, so add another
    if (merge_index[i] == -1) {
      verts_merged[merge_count].x = verts[i].x;
      verts_merged[merge_count].y = verts[i].y;
      verts_merged[merge_count].z = verts[i].z;
      merge_index[i] = merge_count;
      merge_count++;
    }
  }
  // now do the standard normal calculations on the merged list
  for (i = 0; i < tcount; i++) {
    // Convert the 3 edges of the polygon into vectors 
    index0 = merge_index[triangle[i].vertIndex[0]];
    index1 = merge_index[triangle[i].vertIndex[1]];
    index2 = merge_index[triangle[i].vertIndex[2]];
    RwV3dSub(&edge[0], &verts_merged[index0], &verts_merged[index1]);
    RwV3dSub(&edge[1], &verts_merged[index1], &verts_merged[index2]);
    RwV3dSub(&edge[2], &verts_merged[index2], &verts_merged[index0]);

    // normalize the vectors 
    if (!normalize (&edge[0]))
      continue;
    if (!normalize (&edge[1]))
      continue;
    if (!normalize (&edge[2]))
      continue;

    // now get the normal from the cross product of any two of the edge vectors 
    RwV3dCrossProduct (&normal, &edge[2], &edge[0]);
    // get the length so we can divide the normal by its length in order
    // to make it a unit normal 
    if ((length = RwV3dLength (&normal)) > 0.000001f) {
      recip = 1.0f / length;
      RwV3dScale (&normal, &normal, recip);
      // now calculate the 3 internal angles of this triangle so that we
      // can weight each normal by the size of the angle - this makes it
      // so that the triangle with the largest angle at that vertex has
      // the most "influence" over the direction of the normal at that
      // vertex 
      dot = RwV3dDotProduct(&edge[2], &edge[0]);
      angle[0] = (RwReal)(acos(-dot));
      if (_isnan (angle[0]))
        continue;
      angle[1] = (RwReal)(acos(-RwV3dDotProduct(&edge[0], &edge[1])));
      if (_isnan (angle[1]))
        continue;
      angle[2] = (RwReal)(rwPI) - (angle[0] + angle[1]);
      // for each vertex of the triangle...
      // scale the normal by the angle at that vertex 
      /// and add it to the normal at that vertex 
      RwV3dScale (&scaledNormal, &normal, angle[0]);
      RwV3dAdd (&normals_merged[index0], &normals_merged[index0], &scaledNormal);
      RwV3dScale (&scaledNormal, &normal, angle[1]);
      RwV3dAdd (&normals_merged[index1], &normals_merged[index1], &scaledNormal);
      RwV3dScale (&scaledNormal, &normal, angle[2]);
      RwV3dAdd (&normals_merged[index2], &normals_merged[index2], &scaledNormal);
    }
  }
  // finally, re-normalize all vertex normals
  for (j = 0; j < merge_count; j++) {
    if (!(length = RwV3dLength (&normals_merged[j])))
      continue;
    else
      RwV3dScale (&normals_merged[j], &normals_merged[j], 1.0f / length);
  }
  // now copy the merged normals back into the real normal list
  for (j = 0; j < vcount; j++) 
    normals[j] = normals_merged[merge_index[j]];
  free (verts_merged);
  free (normals_merged);
  return (rw_geometry*)geometry;

}*/


#if 0
rw_geometry* rw_geometry_calc_normals (rw_geometry* geometry)
{

  RwV3d*      vPoints;
  RwV3d*      vNormals;
  int         i;
  int         j;
  RwReal      length;
  RwV3d       edge[3];
  RwV3d       normal;
  RwReal      angle[3];
  RwReal      recip;
  int         tcount;
  int         vcount;
  RpTriangle*     triangle;
  RpMorphTarget*  keyFrame;
  float           dot;

  keyFrame = RpGeometryGetMorphTarget ((RpGeometry*)geometry, 0);
  vPoints = RpMorphTargetGetVertices (keyFrame);
  vNormals = RpMorphTargetGetVertexNormals (keyFrame);
  vcount = RpGeometryGetNumVertices ((RpGeometry*)geometry);
  tcount = RpGeometryGetNumTriangles ((RpGeometry*)geometry);
  triangle = RpGeometryGetTriangles ((RpGeometry*)geometry);

  if (!vNormals || !vcount || !tcount) // there are no normals?
    return (rw_geometry*)NULL;

  /* for each triangle... */
  for (i = 0; i < tcount; i++) {
    /* Convert the 3 edges of the polygon into vectors */
    RwV3dSub(&edge[0], &vPoints[triangle[i].vertIndex[0]],
      &vPoints[triangle[i].vertIndex[1]]);
    RwV3dSub(&edge[1], &vPoints[triangle[i].vertIndex[1]],
      &vPoints[triangle[i].vertIndex[2]]);
    RwV3dSub(&edge[2], &vPoints[triangle[i].vertIndex[2]],
      &vPoints[triangle[i].vertIndex[0]]);

    /* normalize the vectors */
    if (!normalize (&edge[0]))
      continue;
    if (!normalize (&edge[1]))
      continue;
    if (!normalize (&edge[2]))
      continue;

    /* now get the normal from the cross product of any two of the edge 
       vectors */
    RwV3dCrossProduct (&normal, &edge[2], &edge[0]);
    /* get the length so we can divide the normal by its length in order
       to make it a unit normal */
    if ((length = RwV3dLength (&normal)) > 0.000001f) {
      recip = 1.0f / length;
      RwV3dScale (&normal, &normal, recip);
      /* now calculate the 3 internal angles of this triangle so that we
         can weight each normal by the size of the angle - this makes it
         so that the triangle with the largest angle at that vertex has
         the most "influence" over the direction of the normal at that
         vertex */
      dot = RwV3dDotProduct(&edge[2], &edge[0]);
      angle[0] = (RwReal)(acos(-dot));
      if (_isnan (angle[0]))
        continue;
      angle[1] = (RwReal)(acos(-RwV3dDotProduct(&edge[0], &edge[1])));
      if (_isnan (angle[1]))
        continue;
      angle[2] = (RwReal)(rwPI) - (angle[0] + angle[1]);
      /* for each vertex of the triangle... */
      for (j = 0; j < 3; j++) {
        RwInt32     nVert = triangle[i].vertIndex[j];
        RwV3d       scaledNormal;
        /* scale the normal by the angle at that vertex */
        RwV3dScale (&scaledNormal, &normal, angle[j]);
        /* and add it to the normal at that vertex */
        RwV3dAdd (&vNormals[nVert], &vNormals[nVert], &scaledNormal);
      }
    }
  }

  /* finally, re-normalize all vertex normals, since each vertex normal
     is now the sum of normals from all triangles sharing that vertex.
     This creates a new normal that is a weighted average of the normals 
     of all triangles sharing that vertex */
  for (j=0; j<vcount; j++)
    if (!(length = RwV3dLength (&vNormals[j])))
      /* vertex normal is still 0 length, so this vertex must not be in use*/
      continue;
    else
      RwV3dScale (&vNormals[j], &vNormals[j], 1.0f / length);
  return (rw_geometry*)geometry;

}
#endif