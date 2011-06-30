/*-----------------------------------------------------------------------------

  FileXcpp

-------------------------------------------------------------------------------

  Reading of Direct X files, which were creatively named .x

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "cfigure.h"
#include "file.h"

#define DELIMIT   "\n\r\t ;,\""

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_frames (char* buffer, CFigure* fig)
{

  char*             token;
  char*             find;
  bool              done;
  GLmatrix          matrix;
  GLvector          pos;
  vector<GLmatrix>  matrix_stack;
  vector<BoneId>    bone_stack;
  BoneId            queued_bone;
  BoneId            queued_parent;
  unsigned          i;
  unsigned          depth;

  depth = 0;
  done = false;
  matrix.Identity ();
  matrix_stack.push_back (matrix);
  fig->_bone.clear ();
  token = strtok (NULL, DELIMIT);
  while (strcmp (token, "FRAME"))
    token = strtok (NULL, DELIMIT);
  while (!done) {
    if (find = strstr (token, "}")) {
      depth--;
      bone_stack.pop_back ();
      matrix_stack.pop_back ();
      if (depth < 2)
        done = true;
    }
    if (find = strstr (token, "FRAMETRANSFORMMATRIX")) {
      //eat the opening brace
      token = strtok (NULL, DELIMIT);
      matrix.Identity ();
      for (int x = 0; x < 4; x++) {
        for (int y = 0; y < 4; y++) {
          token = strtok (NULL, DELIMIT);
          matrix.elements[x][y] = (float)atof (token);
        }
      }
      matrix_stack.push_back (matrix);
      matrix.Identity ();
      for (i = 0; i < matrix_stack.size (); i++)           
        matrix = glMatrixMultiply (matrix,  matrix_stack[i]);
      pos = glMatrixTransformPoint (matrix, glVector (0.0f, 0.0f, 0.0f));
      pos.x = -pos.x;
      fig->PushBone (queued_bone, queued_parent, pos);
      //Now plow through until we find the closing brace
      while (!(find = strstr (token, "}"))) 
        token = strtok (NULL, DELIMIT);
    }
    if (find = strstr (token, "FRAME")) {
      //Grab the name
      token = strtok (NULL, DELIMIT);
      queued_bone = fig->IdentifyBone (token);
      //eat the open brace
      token = strtok (NULL, DELIMIT);
      depth++;
      bone_stack.push_back (queued_bone);
      matrix.Identity ();
      for (i = 0; i < matrix_stack.size (); i++)           
        matrix = glMatrixMultiply (matrix,  matrix_stack[i]);
      pos = glMatrixTransformPoint (matrix, glVector (0.0f, 0.0f, 0.0f));
      //Find the last valid bone in the chain.
      vector<BoneId>::reverse_iterator rit;
      queued_parent = BONE_ROOT;
      for (rit = bone_stack.rbegin(); rit < bone_stack.rend(); ++rit) {
        if (*rit != BONE_INVALID && *rit != queued_bone) {
            queued_parent = *rit;
            break;
        }
      }
    }
    token = strtok (NULL, DELIMIT);
  }
    

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_mesh (char* buffer, CFigure* fig)
{

  char*             token;
  int               count;
  int               poly;
  int               i;
  GLvector          pos;
  int               i1, i2, i3, i4;

  token = strtok (NULL, DELIMIT);
  while (strcmp (token, "MESH"))
    token = strtok (NULL, DELIMIT);
  //eat the open brace
  token = strtok (NULL, DELIMIT);
  //get the vert count
  token = strtok (NULL, DELIMIT);
  count = atoi (token);
  //We begin reading the vertex positions
  for (i = 0; i < count; i++) {
    token = strtok (NULL, DELIMIT);
    pos.x = -(float)atof (token);
    token = strtok (NULL, DELIMIT);
    pos.y = (float)atof (token);
    token = strtok (NULL, DELIMIT);
    pos.z = (float)atof (token);
    fig->_skin_static.PushVertex (pos, glVector (0.0f, 0.0f, 0.0f), glVector (0.0f, 0.0f));
  }
  //Directly after the verts are the polys
  token = strtok (NULL, DELIMIT);
  count = atoi (token);
  for (i = 0; i < count; i++) {
    token = strtok (NULL, DELIMIT);
    poly = atoi (token);
    if (poly == 3) {
      i1 = atoi (strtok (NULL, DELIMIT));
      i2 = atoi (strtok (NULL, DELIMIT));
      i3 = atoi (strtok (NULL, DELIMIT));
      fig->_skin_static.PushTriangle (i1, i2, i3);
    } else if (poly == 4) {
      i1 = atoi (strtok (NULL, DELIMIT));
      i2 = atoi (strtok (NULL, DELIMIT));
      i3 = atoi (strtok (NULL, DELIMIT));
      i4 = atoi (strtok (NULL, DELIMIT));
      fig->_skin_static.PushQuad (i1, i2, i3, i4);
    }
  }

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_normals (char* buffer, CFigure* fig)
{

  char*             token;
  int               count;
  int               i;
  GLvector          pos;

  token = strtok (NULL, DELIMIT);
  while (strcmp (token, "MESHNORMALS"))
    token = strtok (NULL, DELIMIT);
  //eat the open brace
  token = strtok (NULL, DELIMIT);
  //get the vert count
  token = strtok (NULL, DELIMIT);
  count = atoi (token);
  //We begin reading the normals
  for (i = 0; i < count; i++) {
    token = strtok (NULL, DELIMIT);
    pos.x = -(float)atof (token);
    token = strtok (NULL, DELIMIT);
    pos.y = (float)atof (token);
    token = strtok (NULL, DELIMIT);
    pos.z = (float)atof (token);
    fig->_skin_static._normal[i] = pos;
  }

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_uvs (char* buffer, CFigure* fig)
{

  char*             token;
  int               count;
  int               i;
  GLvector2         pos;

  token = strtok (NULL, DELIMIT);
  while (strcmp (token, "MESHTEXTURECOORDS"))
    token = strtok (NULL, DELIMIT);
  //eat the open brace
  token = strtok (NULL, DELIMIT);
  //get the vert count
  token = strtok (NULL, DELIMIT);
  count = atoi (token);
    //We begin reading the normals
  for (i = 0; i < count; i++) {
    token = strtok (NULL, DELIMIT);
    pos.x = (float)atof (token);
    token = strtok (NULL, DELIMIT);
    pos.y = (float)atof (token);
    fig->_skin_static._uv[i] = pos;
  }

}



/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_weights (char* buffer, CFigure* fig)
{

  char*             token;
  int               count;
  int               i;
  BoneId            bid;
  vector<BWeight>   bw_list;
  BWeight           bw;

  while (true) {
    token = strtok (NULL, DELIMIT);
    while (strcmp (token, "SKINWEIGHTS")) {
      token = strtok (NULL, DELIMIT);
      if (token == NULL)
        return;
    }
    //eat the open brace
    token = strtok (NULL, DELIMIT);
    //get the name of this bone
    token = strtok (NULL, DELIMIT);
    bid = CAnim::BoneFromString (token);
    if (bid == BONE_INVALID)
      continue;
    //get the vert count
    token = strtok (NULL, DELIMIT);
    count = atoi (token);
    bw_list.clear ();
    //get the indicies
    for (i = 0; i < count; i++) {
      token = strtok (NULL, DELIMIT);
      bw._index = atoi (token);
      bw_list.push_back (bw);
    }
    //get the weights
    for (i = 0; i < count; i++) {
      token = strtok (NULL, DELIMIT);
      bw_list[i]._weight = (float)atof (token);
    }
    //Store them
    for (i = 0; i < count; i++) {
      //if (bw_list[i]._weight < 0.9f)
        //continue;
      if (bw_list[i]._weight < 0.001f)
        continue;
      fig->_bone[fig->_bone_index[bid]]._vertex_weights.push_back (bw_list[i]);
    }
  }
}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

bool FileXLoad (char* filename, CFigure* fig)
{
  /*
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
  */
  long              size;
  char*             buffer;

  buffer = FileLoad (filename, &size);
  if (!buffer)
    return false;
  _strupr (buffer);
  strtok (buffer, DELIMIT);
  do_frames (buffer, fig);
  do_mesh (buffer, fig);
  do_normals (buffer, fig);
  do_uvs (buffer, fig);
  do_weights (buffer, fig);
  free (buffer);
  return true;




  /*
  while (!done) {
    //Read the skeleton
    if ((!skel_done) && (find = strstr (token, "FRAME"))) {
      GLvector pusher = glVector (0.0f, 0.0f, 0.0f);
      frame_depth = 0;
      //_bone[_bone_index[BONE_ROOT]]._matrix.Identity ();
      matrix.Identity ();
      matrix_stack.push_back (matrix);
      _bone.clear ();
      while (!skel_done) {

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
  _skin_static.CalculateNormalsSeamless ();
  free (buffer);
  return true;
  */
}
