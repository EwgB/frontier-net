/*-----------------------------------------------------------------------------
  Texture.cpp
  2006 Shamus Young
-------------------------------------------------------------------------------
  This loads in textures.  Nothin' fancy.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include <stdio.h>
#include "file.h"
#include "texture.h"

class GLtexture
{
public:
  GLtexture*        next;
  GLuint            id;
  char              name[32];
  char*             image_name;
  int               width;
  int               height;
  short             bpp;//bytes per pixel
};

unsigned    TextureIdFromName (const char* name);
GLtexture*  TextureFromName (const char* name);
byte*       TextureRaw (char* name, int* width, int* height);
void        TextureInit (void);
void        TextureTerm (void);
void        TexturePurge ();

#define max_STRING          128

static GLtexture*   head_texture;

static GLtexture* LoadTexture (const char* name)
{
  GLtexture*        t;
  char              filename[max_STRING];
  GLcoord           size;
  char*             buffer;

  t = new GLtexture;
  strcpy (t->name, name);
  sprintf (filename, "textures/%s", name);
  glGenTextures (1, &t->id);
	glBindTexture (GL_TEXTURE_2D, t->id);
  buffer = FileImageLoad (filename, &size);
  t->width = size.x;
  t->height = size.y;
  glTexImage2D (GL_TEXTURE_2D, 0, 4, size.x, size.y, 0, GL_RGBA, GL_UNSIGNED_BYTE, buffer);
  gluBuild2DMipmaps (GL_TEXTURE_2D, 4, size.x, size.y,  GL_RGBA, GL_UNSIGNED_BYTE, buffer); 
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  delete buffer;
  t->next = head_texture;
  head_texture = t;
  return t;
}

void TexturePurge ()
{
  GLtexture*       t;

  while (head_texture) {
    t = head_texture;
    glDeleteTextures (1, &t->id); 
    head_texture = t->next;
    delete t;
  }
}

GLtexture* TextureFromName (const char* name)
{
  GLtexture*       t;

  for (t = head_texture; t; t = t -> next) {
    if (!_stricmp (name, t->name))
      return t;
  }
  t = LoadTexture (name);
  return t;
}

unsigned TextureIdFromName (const char* name)
{
  GLtexture*       t;

  t = TextureFromName (name);
  if (t)
    return t->id;
  return 0;
}

void TextureInit (void)
{
}

void TextureTerm (void)
{
  TexturePurge ();
}
*/