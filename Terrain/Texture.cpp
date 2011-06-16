/*-----------------------------------------------------------------------------

  Texture.cpp

  2006 Shamus Young

-------------------------------------------------------------------------------
  
  This loads in textures.  Nothin' fancy.
  
-----------------------------------------------------------------------------*/


#include "stdafx.h"
#include <stdio.h>
#include "bmpfile.h"
#include "texture.h"

#define max_STRING          128


static GLtexture*   head_texture;

/*-----------------------------------------------------------------------------
                           t e x t u r e   i d
-----------------------------------------------------------------------------*/

static GLtexture* LoadTexture (char* name, int mask)
{

  char              filename[max_STRING];
  GLtexture*        t;
  BMPFile*  image;
  unsigned          y;
  unsigned          x;
  char*             buffer;
  unsigned char     rgba[4];
  int               index;
  unsigned char*    ptr;


  t = new GLtexture;
  strcpy (t->name, name);
  sprintf (filename, "textures/%s", name);
  image = new BMPFile;
  image->loadFile (filename);
  if (!image) {
    t->id = 0;
    return t;
  }
  glGenTextures (1, &t->id);// Create The Texture
	// Typical Texture Generation Using Data From The Bitmap
	glBindTexture (GL_TEXTURE_2D, t->id);
  // Generate The Texture
  t->bpp = 3;
  if (mask == MASK_PINK) {
    buffer = new char[image->sizeX * image->sizeY * 4];
    for (y = 0; y < image->sizeY; y++) {
      for (x = 0; x < image->sizeX; x++) {
        index = (x + y * image->sizeX) * 3;
        ptr = image->data;
        rgba[2] = ptr[index];
        rgba[1] = ptr[index + 1];
        rgba[0] = ptr[index + 2];
        if (rgba[0] > 250 && rgba[2] > 250 && rgba[1] < 10) {
          rgba[0] = rgba[1] = rgba[2] = rgba[3] = 0;
        } else {
          rgba[3] = 255;
        }
        index = (x + y * image->sizeX) * 4;
        memcpy (&buffer[index], rgba, 4);
      }
    }
	  glTexImage2D (GL_TEXTURE_2D, 0, 4, 
      image->sizeX, 
      image->sizeY, 
      0, GL_RGBA, GL_UNSIGNED_BYTE, 
      buffer);
    delete buffer;
  } else if (mask == MASK_LUminANCE) {
    buffer = new char[image->sizeX * image->sizeY * 4];
    for (y = 0; y < image->sizeY; y++) {
      for (x = 0; x < image->sizeX; x++) {
        index = (x + y * image->sizeX) * 3;
        ptr = image->data;
        rgba[0] = ptr[index];
        rgba[1] = ptr[index + 1];
        rgba[2] = ptr[index + 2];
        rgba[3] = (rgba[0] + rgba[1] + rgba[2]) / 3;
        index = (x + y * image->sizeX) * 4;
        memcpy (&buffer[index], rgba, 4);
      }
    }
	  glTexImage2D (GL_TEXTURE_2D, 0, 4, 
      image->sizeX, 
      image->sizeY, 
      0, GL_RGBA, GL_UNSIGNED_BYTE, 
      buffer);
    delete buffer;  } else {
	  glTexImage2D (GL_TEXTURE_2D, 0, t->bpp, 
      image->sizeX, 
      image->sizeY, 
      0, GL_RGB, GL_UNSIGNED_BYTE, 
      image->data);
  }
  t->width = image->sizeX;
  t->height = image->sizeY;
  free (image);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  t->next = head_texture;
  head_texture = t;
  return t;

}

/* Module Functions **********************************************************/

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

byte* TextureRaw (char* name, int* width, int* height)
{

  char              filename[max_STRING];
  BMPFile           image;

  sprintf (filename, "textures/%s", name);
  image.loadFile (filename);
  *width = image.sizeX;
  *height = image.sizeY;
  return image.data;

}

GLtexture* TextureFromName (char* name, int mask)
{

  GLtexture*       t;

  for (t = head_texture; t; t = t -> next) {
    if (!_stricmp (name, t->name))
      return t;
  }
  t = LoadTexture (name, mask);
  return t;

}

GLtexture* TextureFromName (char* name)
{

  return TextureFromName (name, MASK_PINK);

}

unsigned TextureIdFromName (char* name)
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


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void TextureTerm (void)
{


  TexturePurge ();


}
