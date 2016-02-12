/*-----------------------------------------------------------------------------
  glFont.cpp
  2009 Shamus Young
-------------------------------------------------------------------------------
  A system for loading bitmap fonts.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"

struct GLglyph
{
  char        chr;
  GLcoord     size;
  GLvector2   uv1;
  GLvector2   uv2;
  int         advance;
  char*       buffer;
};

struct GLfont
{
private:
  GLglyph         _glyph[GL_MAX_GLYPHS];
  int             _id;
  int             _line_height;
public:
  void            FaceSet (unsigned id);
  void            Select ();
  GLglyph         Glyph (int ascii) const { return _glyph[ascii]; };
  int             GlyphDraw (int ascii, GLcoord origin) const;
  int             GlyphWidth (int ascii);
  int             LineHeight () const { return _line_height;}
};

#define GLYPH_GRID  16
#define GLYPH_UNIT  (1.0f / GLYPH_GRID)

void GLfont::Select ()
{
  glBindTexture (GL_TEXTURE_2D, _id);
  glEnable (GL_TEXTURE_2D);
  glEnable (GL_BLEND);
  glDisable (GL_FOG);
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glDisable(GL_DEPTH_TEST);
  glDisable(GL_LIGHTING);
  //glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
}

void GLfont::FaceSet (unsigned id)
{
  GLcoord         size;
  GLcoord         origin;
  unsigned char*  buffer;
  int             i;
  int             x, y;
  int             col, row;
  int             index;
  int             box_size;
  int             left, right;

  _id = id;
  glGetTexLevelParameteriv (GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH, &size.x);
  glGetTexLevelParameteriv (GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT, &size.y);
  buffer = new unsigned char[size.x * size.y * 4];
  glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, buffer);
  box_size = size.x / GLYPH_GRID;
  for (i = 0; i < GL_MAX_GLYPHS; i++) {
    col = i % GLYPH_GRID;
    row = (GLYPH_GRID - 1) - i / GLYPH_GRID;
    origin.x = col * box_size;
    origin.y = row * box_size;
    left = 0;
    x = 0;
    y = 0;
    while (x < box_size) {
      index = (origin.x + x + (origin.y + y) * size.y) * 4;
      if (buffer[index])
        break;
      y++;
      if (y == box_size) {
        y = 0;
        x++;
        left++;
      }
    }
    x = box_size - 1;
    y = 0;
    right = box_size;
    while (x >= 0 && right > 0 && x >= 0) {
      index = (origin.x + x + (origin.y + y) * size.y) * 4;
      if (buffer[index])
        break;
      y++;
      if (y == box_size) {
        y = 0;
        x--;
        right--;
      }
    }
    if (left >= right) {
      left = 0;
      right = box_size / 2;
    }
    //left++;
    //right--;
    //left = 0;
    //right = box_size;

    _glyph[i].size.x = (right - left);
    _glyph[i].size.y = box_size;
    _glyph[i].uv1 = glVector ((float)(origin.x + left) / (float)size.x, (float)origin.y / (float)size.y);
    _glyph[i].uv2 = glVector ((float)(origin.x + right) / (float)size.x, (float)(origin.y + box_size) / (float)size.y);
  }
  delete buffer;
  _line_height = box_size;
  //_glyph[32].
}

int GLfont::GlyphWidth (int ascii)
{
  ascii %= GL_MAX_GLYPHS;
  if (ascii < 0)
    ascii = GL_MAX_GLYPHS + ascii;
  return _glyph[ascii].size.x;
}

int GLfont::GlyphDraw (int ascii, GLcoord origin) const
{
  ascii %= GL_MAX_GLYPHS;
  if (ascii < 0)
    ascii = GL_MAX_GLYPHS + ascii;
  glTexCoord2f (_glyph[ascii].uv1.x, _glyph[ascii].uv1.y);
  glVertex2i (origin.x, origin.y + _glyph[ascii].size.y);
  
  glTexCoord2f (_glyph[ascii].uv2.x, _glyph[ascii].uv1.y);
  glVertex2i (origin.x + _glyph[ascii].size.x, origin.y + _glyph[ascii].size.y);

  glTexCoord2f (_glyph[ascii].uv2.x, _glyph[ascii].uv2.y);
  glVertex2i (origin.x + _glyph[ascii].size.x, origin.y);

  glTexCoord2f (_glyph[ascii].uv1.x, _glyph[ascii].uv2.y);
  glVertex2i (origin.x, origin.y);
  return _glyph[ascii].size.x;
}

#include "texture.h"

static GLfont      f;

void glFontInit ()
{
  GLtexture*  t;

  return;
  t = TextureFromName ("font.png");
  f.FaceSet (t->id);
}

void glFontDraw ()
{
  return;
  f.Select ();

  glMatrixMode (GL_PROJECTION);
  glPushMatrix ();
  glLoadIdentity ();
  glOrtho (0, 2200, 1800, 0, 0.1f, 2048);
	glMatrixMode (GL_MODELVIEW);
  glPushMatrix ();
  glLoadIdentity();
  glTranslatef(0, 0, -1.0f);				
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

  //text_draw (buffer);
  char  buffer[] = "AbCdEf@1234";
  GLcoord pos;

  pos.Clear ();
  glBegin (GL_QUADS);
  for (int i = 0; i < (int)strlen (buffer); i++) {
    glColor3f (0.1f, 0.1f, 0.1f);
    f.GlyphDraw (buffer[i], pos + 2);
    glColor3f (0.5f, 0.5f, 0.5f);
    f.GlyphDraw (buffer[i], pos + -2);
    glColor3f (0.9f, 0.9f, 0.9f);
    pos.x += f.GlyphDraw (buffer[i], pos) + 0;
  }
  glEnd (); 

  glPopMatrix ();
  glMatrixMode (GL_PROJECTION);
  glPopMatrix ();
  glMatrixMode (GL_MODELVIEW);
  buffer[0] = 0;
}
*/