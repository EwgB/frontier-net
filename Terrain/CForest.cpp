/*-----------------------------------------------------------------------------

  CForest.cpp

-------------------------------------------------------------------------------

  This class will generate a group of trees for the given area.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cache.h"
#include "cforest.h"
#include "ctree.h"
#include "sdl.h"
#include "world.h"

//static CTree      tree;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

CForest::CForest ()
{

  _stage = FOREST_STAGE_BEGIN;
  _valid = false;
  _walk.Clear ();
  _mesh_list.clear ();
  _vbo_list.clear ();

}

unsigned CForest::MeshFromTexture (unsigned texture_id)
{

  unsigned      i;
  TreeMesh      tm;

  //if (!_mesh_list.empty ())
    //return 0;
  
  for (i = 0; i < _mesh_list.size (); i++) {
    if (_mesh_list[i]._texture_id == texture_id)
      return i;
  }
  
  tm._texture_id = texture_id;
  tm._mesh.Clear ();
  _mesh_list.push_back (tm);
  return _mesh_list.size () - 1;

}

bool CForest::ZoneCheck ()
{

  if (!CachePointAvailable (_origin.x, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + FOREST_SIZE, _origin.y))
    return false;
  if (!CachePointAvailable (_origin.x + FOREST_SIZE,_origin.y + FOREST_SIZE))
    return false;
  if (!CachePointAvailable (_origin.x, _origin.y + FOREST_SIZE))
    return false;
  return true;

}

void CForest::Set (int x, int y)
{

  if (_origin.x == x * FOREST_SIZE && _origin.y == y * FOREST_SIZE)
    return;
  //if (_stage > FOREST_STAGE_BEGIN && _stage < FOREST_STAGE_DONE)
    //return;
  _compile_step = 0;
  _position.x = x;
  _position.y = y;
  _origin.x = x * FOREST_SIZE;
  _origin.y = y * FOREST_SIZE;
  _stage = FOREST_STAGE_BEGIN;
  for (unsigned i = 0; i < _mesh_list.size (); i++) 
    _mesh_list[i]._mesh.Clear ();
  _mesh_list.clear ();

}

static int  vvv;

void CForest::Build (long stop)
{

  unsigned      i;
  GLvector      origin;
  GLvector      newpos;
  GLvector      newnorm;
  GLmesh*       tm;
  CTree*        tree;
  unsigned      tree_id;
  unsigned      texture_id;
  unsigned      base_index;
  int           world_x, world_y;
  GLmatrix      mat;
  unsigned      mesh_index;

  world_x = _origin.x + _walk.x;
  world_y = _origin.y + _walk.y;
  tree_id = CacheTree (world_x, world_y);
  if (tree_id) {
    mat.Identity ();
    mat.Rotate (WorldNoisef (_walk.x + _walk.y * FOREST_SIZE) * 360.0f, 0.0f, 0.0f, 1.0f);
    origin = CachePosition (world_x, world_y);
    tree = WorldTree (tree_id);
    tm = tree->Mesh ();
    texture_id = tree->Texture ();
    mesh_index = MeshFromTexture (texture_id);
    base_index = _mesh_list[mesh_index]._mesh.Vertices ();
    for (i = 0; i < tm->Vertices (); i++) {
      newpos = glMatrixTransformPoint (mat, tm->_vertex[i]);
      newpos.z *= 0.5f + WorldNoisef (2 + _walk.x + _walk.y * FOREST_SIZE) * 1.0f;
      newnorm = glMatrixTransformPoint (mat, tm->_normal[i]);
      _mesh_list[mesh_index]._mesh.PushVertex (newpos + origin, newnorm, tm->_uv[i]);
    }
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (world_x, world_y, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    //_mesh_list[mesh_index]._mesh.PushVertex (glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
    for (i = 0; i < tm->Triangles (); i++) {
      unsigned i1, i2, i3;
      i1 = base_index + tm->_index[i * 3];
      i2 = base_index + tm->_index[i * 3 + 1];
      i3 = base_index + tm->_index[i * 3 + 2];
      _mesh_list[mesh_index]._mesh.PushTriangle (i1, i2, i3);
    }
    
    //_mesh_list[mesh_index]._mesh = *tm;
    //for (i = 0; i < tm->Vertices (); i++) 
      //_mesh_list[mesh_index]._mesh._vertex[i] += origin;
    base_index = _mesh_list[mesh_index]._mesh.Vertices ();
    {
      base_index = base_index;
    }
  }
  if (_walk.Walk (FOREST_SIZE))
    _stage++;

}

void CForest::Compile ()
{

  unsigned    i;

  //First, purge the existing VBO
  for (i = 0; i < _vbo_list.size (); i++) 
    _vbo_list[i]._vbo.Clear ();
  _vbo_list.clear ();
  //Now compile the new list
  _vbo_list.resize (_mesh_list.size ());
  for (i = 0; i < _mesh_list.size (); i++) {
    _vbo_list[i]._vbo.Clear ();
    _vbo_list[i]._bbox = _mesh_list[i]._mesh._bbox;
    _vbo_list[i]._texture_id= _mesh_list[i]._texture_id;
    if (!_mesh_list[i]._mesh._vertex.empty ())
      _vbo_list[i]._vbo.Create (GL_TRIANGLES, 
      _mesh_list[i]._mesh.Triangles () * 3, 
      _mesh_list[i]._mesh.Vertices (), 
      &_mesh_list[i]._mesh._index[0], 
      &_mesh_list[i]._mesh._vertex[0], 
      &_mesh_list[i]._mesh._normal[0], NULL, 
      &_mesh_list[i]._mesh._uv[0]);
  }
  //Now purge the mesh list, so it can begin building again in the background
  //when the time comes.
  for (i = 0; i < _mesh_list.size (); i++) 
    _mesh_list[i]._mesh.Clear ();
  _mesh_list.clear ();
  _valid = true;
  _stage++;
  

}

void CForest::Update (long stop)
{

  while (SdlTick () < stop && !Ready ()) {
    switch (_stage) {
    case FOREST_STAGE_BEGIN:
      if (!ZoneCheck ())
        return;
      _stage++;
    case FOREST_STAGE_BUILD:
      Build (stop);
      break;
    case FOREST_STAGE_COMPILE:
      Compile ();
      break;
    }
  }


}


void CForest::Render ()
{

  unsigned    i;
  //We need at least one successful build before we can draw.
  if (!_valid)
    return;
  glEnable (GL_BLEND);

  //glDisable (GL_LIGHTING);
  //glDisable (GL_FOG);
  //glBlendFunc (GL_ONE, GL_ONE);



  glEnable (GL_LIGHTING);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glDisable (GL_CULL_FACE);
  for (i = 0; i < _vbo_list.size (); i++) {
    glBindTexture (GL_TEXTURE_2D, _vbo_list[i]._texture_id);
    //_vbo_list[i]._vbo.Render ();
  }

  //GLbbox    bb;
  //GLrgba    c = glRgbaUnique (_origin.x + _origin.y * 5);

  //glDisable (GL_LIGHTING);
  //glBlendFunc (GL_ONE, GL_ONE);
  //bb.Clear ();
  //bb.ContainPoint (glVector ((float)_origin.x, (float)_origin.y, 0.0f));
  //bb.ContainPoint (glVector ((float)_origin.x + FOREST_SIZE, (float)_origin.y + FOREST_SIZE,  + FOREST_SIZE));
  //glColor3fv (&c.red);
  //bb.Render ();
  for (i = 0; i < _vbo_list.size (); i++) {
    glBindTexture (GL_TEXTURE_2D, _vbo_list[i]._texture_id);
    _vbo_list[i]._vbo.Render ();
  }

}
