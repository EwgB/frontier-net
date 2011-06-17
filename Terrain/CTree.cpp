/*-----------------------------------------------------------------------------

  CTree.cpp

-------------------------------------------------------------------------------

  

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ctree.h"
#include "vbo.h"

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CTree::Build (GLvector pos)
{

  int       ring, tier;
  int       steps, edge;
  float     height;
  float     angle;
  float     radius;
  float     x, y, z;
  float     tier_height;
  GLvector  core;

  _vertex.clear ();
  _normal.clear ();
  _uv.clear ();
  _index.clear ();
  steps = 8;
  edge = steps + 1;
  radius = 1.0f;
  tier_height = 1.0f;
  height = 0.0f;
  core = glVector (0.0f, 0.0f, 0.0f);
  for (tier = 0; tier < 5; tier++) {
    for (ring = 0; ring <= steps; ring++) {
      angle = (float)ring * (360.0f / (float)steps);
      angle *= DEGREES_TO_RADIANS;
      x = sin (angle);
      y = cos (angle);
      z = height;
      if (tier == 0 && ring % 2)
        _vertex.push_back (glVector (x * radius * 4, y * radius * 4, z));
      else if (tier == 1 && ring % 2)
        _vertex.push_back (glVector (x * 3 * radius, y * 3 * radius, z));
      else
        _vertex.push_back (glVector (x * radius, y * radius, z));
      _normal.push_back (glVector (x, y, 0.0f));
      _uv.push_back (glVector (((float)ring / (float) steps) * 0.5f, z/ 5.0f));

    }
    height += tier_height;
    tier_height += 0.5f;
    //radius *= 0.99f;
    radius -= 0.2f;
  }
  for (tier = 0; tier < 4; tier++) {
    for (ring = 0; ring < steps; ring++) {
      _index.push_back ((ring + 0) + (tier + 0) * (edge));
      _index.push_back ((ring + 1) + (tier + 0) * (edge));
      _index.push_back ((ring + 1) + (tier + 1) * (edge));
      
      _index.push_back ((ring + 0) + (tier + 0) * (edge));
      _index.push_back ((ring + 1) + (tier + 1) * (edge));
      _index.push_back ((ring + 0) + (tier + 1) * (edge));
    }
  }
  for (unsigned i = 0; i < _vertex.size (); i++) 
    _vertex[i] += pos;
  _vbo.Create (GL_TRIANGLES, _index.size (), _vertex.size (), &_index[0], &_vertex[0], &_normal[0], NULL, &_uv[0]);

}

void CTree::Render ()
{

  _vbo.Render ();

}
