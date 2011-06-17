/*-----------------------------------------------------------------------------

  VBO.cpp

-------------------------------------------------------------------------------

  This class manages vertex buffer objects.  Take a list of verticies and 
  indexes, and store them in GPU memory for fast rendering.
 
-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "VBO.h"



// VBO Extension Definitions, From glext.h
#define GL_ARRAY_BUFFER_ARB 0x8892
#define GL_STATIC_DRAW_ARB 0x88E4
typedef void (APIENTRY * PFNGLBINDBUFFERARBPROC) (GLenum target, GLuint buffer);
typedef void (APIENTRY * PFNGLDELETEBUFFERSARBPROC) (GLsizei n, const GLuint *buffers);
typedef void (APIENTRY * PFNGLGENBUFFERSARBPROC) (GLsizei n, GLuint *buffers);
typedef void (APIENTRY * PFNGLBUFFERDATAARBPROC) (GLenum target, int size, const GLvoid *data, GLenum usage);

// VBO Extension Function Pointers
PFNGLGENBUFFERSARBPROC    glGenBuffersARB = NULL;					// VBO Name Generation Procedure
PFNGLBINDBUFFERARBPROC    glBindBufferARB = NULL;					// VBO Bind Procedure
PFNGLBUFFERDATAARBPROC    glBufferDataARB = NULL;					// VBO Data Loading Procedure
PFNGLDELETEBUFFERSARBPROC glDeleteBuffersARB = NULL;				// VBO Deletion Procedure

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void vbo_init ()
{

	// Get Pointers To The GL Functions
	glGenBuffersARB = (PFNGLGENBUFFERSARBPROC) wglGetProcAddress("glGenBuffersARB");
	glBindBufferARB = (PFNGLBINDBUFFERARBPROC) wglGetProcAddress("glBindBufferARB");
	glBufferDataARB = (PFNGLBUFFERDATAARBPROC) wglGetProcAddress("glBufferDataARB");
	glDeleteBuffersARB = (PFNGLDELETEBUFFERSARBPROC) wglGetProcAddress("glDeleteBuffersARB");

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

VBO::VBO ()
{

  _id_vertex = _id_index = _size_vertex = _size_uv = _size_normal = _size_buffer = _index_count = 0;
  _ready = false;
  _id_vertex = 0;
  _id_index = 0;
  _use_color = false;
  _size_color = 0;
  _polygon = 0;

}

VBO::~VBO ()
{

  if (_id_index)
    glDeleteBuffersARB(1, &_id_index);
  if (_id_vertex)
    glDeleteBuffersARB(1, &_id_vertex);


}

void VBO::Clear ()
{

  if (_id_vertex)
    glDeleteBuffersARB (1, &_id_vertex);
  if (_id_index)
    glDeleteBuffersARB (1, &_id_index);
  _id_vertex = 0;
  _id_index = 0;
  _ready = false;

}

void VBO::Create (int polygon, int index_count, int vert_count, unsigned* index_list, GLvector* vert_list, GLvector* normal_list, GLrgba* color_list, GLvector2* uv_list)
{

  char*     buffer;

  if (glGenBuffersARB == NULL)
    vbo_init ();
  if (glGenBuffersARB == NULL)
    return;
  if (_id_vertex)
    glDeleteBuffersARB (1, &_id_vertex);
  if (_id_index)
    glDeleteBuffersARB (1, &_id_index);
  _id_vertex = 0;
  _id_index = 0;
  if (!index_count || !vert_count)
    return;
  _polygon = polygon;
  _use_color = color_list != NULL;
  _size_vertex = sizeof (GLvector) * vert_count;
  _size_normal = sizeof (GLvector) * vert_count;
  _size_uv = sizeof (GLvector2) * vert_count;
  _size_buffer = _size_vertex + _size_normal + _size_uv; 
  if (_use_color) {
    _size_color = sizeof (GLrgba) * vert_count;
    _size_buffer += _size_color;
  } else 
    _size_color = 0;
  //Allocate the array and pack the bytes into it.
  buffer = new char [_size_buffer];
  memcpy (buffer, vert_list, _size_vertex);
  memcpy (buffer + _size_vertex, normal_list, _size_normal);
  if (_use_color) 
    memcpy (buffer + _size_vertex + _size_normal, color_list, _size_color);
  memcpy (buffer + _size_vertex + _size_normal + _size_color, uv_list, _size_uv);
	//Create and load the buffer
  glGenBuffersARB (1, &_id_vertex);
	glBindBufferARB (GL_ARRAY_BUFFER_ARB, _id_vertex);			// Bind The Buffer
	glBufferDataARB (GL_ARRAY_BUFFER_ARB, _size_buffer, buffer, GL_STATIC_DRAW_ARB);
  //Create and load the indicies
  glGenBuffersARB (1, &_id_index);
	glBindBufferARB (GL_ELEMENT_ARRAY_BUFFER_ARB, _id_index);
	glBufferDataARB (GL_ELEMENT_ARRAY_BUFFER_ARB, index_count * sizeof(int), index_list, GL_STATIC_DRAW_ARB);
  _index_count = index_count;
  delete[] buffer;
  _ready = true;

}

void VBO::Render ()
{

  if (!_ready)
    return;
  // bind VBOs for vertex array and index array
  glBindBufferARB (GL_ARRAY_BUFFER_ARB, _id_vertex);  
  glEnableClientState(GL_VERTEX_ARRAY);
  glEnableClientState(GL_NORMAL_ARRAY);
  if (_use_color)
    glEnableClientState(GL_COLOR_ARRAY);
  else
    glDisableClientState(GL_COLOR_ARRAY);
  glEnableClientState(GL_TEXTURE_COORD_ARRAY);
  glVertexPointer (3, GL_FLOAT, 0, 0);
  glNormalPointer (GL_FLOAT, 0, (void*)(_size_vertex));
  if (_use_color)
    glColorPointer(4, GL_FLOAT, 0, (void*)(_size_vertex + _size_normal));
  glTexCoordPointer (2, GL_FLOAT, 0,(void*)(_size_vertex + _size_normal + _size_color));
  //glEnableClientState(GL_COLOR_ARRAY);
  //glColorPointer(3, GL_FLOAT, 0, (void*)(sizeof(vertices)+sizeof(normals)));
  //Draw it
  glBindBufferARB (GL_ELEMENT_ARRAY_BUFFER_ARB, _id_index); // for indices
  glEnableClientState (GL_VERTEX_ARRAY);             // activate vertex coords array
  glDrawElements (_polygon, _index_count, GL_UNSIGNED_INT, 0);
  glDisableClientState (GL_VERTEX_ARRAY);            // deactivate vertex array
  // bind with 0, so, switch back to normal pointer operation
  glBindBufferARB(GL_ARRAY_BUFFER_ARB, 0);
  glBindBufferARB(GL_ELEMENT_ARRAY_BUFFER_ARB, 0);

}