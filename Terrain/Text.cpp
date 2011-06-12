/*-----------------------------------------------------------------------------

  glText.cpp

  2009 Shamus Young

-------------------------------------------------------------------------------
  
  This module has a (theoretically) cross-platform font-loading system.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "text.h"
#include "texture.h"

#include <stdio.h>
#include <stdarg.h>

#define FONT_GRID     16
#define MAX_CHARS     1024
#define GLYPH         (1.0f / FONT_GRID)
#define MAX_BUFFER    1024
#define SCRATCH_COUNT 20
#define SCRATCH_SIZE  100
#define KILOBYTE      1024
#define MEGABYTE      (KILOBYTE * 1024)

static struct
{
  char            buffer[SCRATCH_SIZE];
} scratch[SCRATCH_COUNT];

static GLcoord        view_size;
static GLvector2      glyph[MAX_CHARS]; 
static char           buffer[MAX_BUFFER];
int                   current_scratch;
static GLfont         font;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void text_draw (char* text)
{

  int       p;
  int       len;
  unsigned  ch;
  GLcoord   cursor;

  font.Select ();
  len = strlen (text);
  cursor.x = 0;
  cursor.y = 0;
  glBegin (GL_QUADS);
  for (p = 0; p < len; p++) {
    ch = (unsigned char)text[p];
    if (cursor.x + font.GlyphWidth (ch) > view_size.x || ch == '\n') {
      cursor.x = 0;
      cursor.y += font.LineHeight ();
      continue;
    }
    glColor3f (0, 0, 0.5f);
    font.GlyphDraw (ch, cursor + 1);
    font.GlyphDraw (ch, cursor - 1);
    glColor3f (1, 1, 0.5f);
    cursor.x += font.GlyphDraw (ch, cursor) + 4;
  }
  glEnd ();





/*
  int       p;
  int       len;
  unsigned  ch;
  GLcoord   cursor;

  GLtexture*  t;

  t = TextureFromName ("font.bmp", MASK_LUMINANCE);
  glBindTexture (GL_TEXTURE_2D, t->id);
  len = strlen (text);
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_LINEAR);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	

  cursor.x = 0;
  cursor.y = 0;
  glBegin (GL_QUADS);
  for (p = 0; p < len; p++) {
    ch = (unsigned char)text[p];
    if (ch == '\n') {
      cursor.x = 0;
      cursor.y++;
      continue;
    }
    glTexCoord2f (glyph[ch].x, glyph[ch].y);
    glVertex2i (cursor.x, cursor.y + 1);
    glTexCoord2f (glyph[ch].x + GLYPH, glyph[ch].y);
    glVertex2i (cursor.x + 1, cursor.y + 1);
    glTexCoord2f (glyph[ch].x + GLYPH, glyph[ch].y + GLYPH);
    glVertex2i (cursor.x + 1, cursor.y);
    glTexCoord2f (glyph[ch].x, glyph[ch].y + GLYPH);
    glVertex2i (cursor.x, cursor.y);
    cursor.x++;
    if (cursor.x >= view_size.x) {
      cursor.x = 0;
      cursor.y++;
    }
  }
  glEnd ();
  */
}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

char* TextBytes (int bytes)
{

  current_scratch++;
  current_scratch %= SCRATCH_COUNT;
  if (bytes > MEGABYTE) 
    sprintf (scratch[current_scratch].buffer, "%1.1fMb", (float)bytes / MEGABYTE);
  else if (bytes > KILOBYTE) 
    sprintf (scratch[current_scratch].buffer, "%1.1fKb", (float)bytes / KILOBYTE);
  else
    sprintf (scratch[current_scratch].buffer, "%d Bytes", bytes);
  return scratch[current_scratch].buffer;
  
}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void TextInit ()
{

  int         x, y;
  int         i;

  for (i = 0; i < MAX_CHARS; i++) {
    x = i % FONT_GRID;
    y = 255 - (i / FONT_GRID);
    glyph[i] = glVector ((float)x * GLYPH, (float)y * GLYPH);
  }
  //view_size.x = 80;
  //view_size.y = 50;

}

void TextCreate (int width, int height)
{

  GLtexture*    t;

  view_size.x = width;
  view_size.y = height;
  t = TextureFromName ("font.bmp", MASK_LUMINANCE);
  font.FaceSet (t->id);

}



void TextRender ()
{

  glMatrixMode (GL_PROJECTION);
  glPushMatrix ();
  glLoadIdentity ();
  glOrtho (0, view_size.x, view_size.y, 0, 0.1f, 2048);
	glMatrixMode (GL_MODELVIEW);
  glPushMatrix ();
  glLoadIdentity();
  glTranslatef(0, 0, -1.0f);				
  
  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glDisable(GL_DEPTH_TEST);
  glDisable(GL_LIGHTING);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glEnable (GL_TEXTURE_2D);
  

  glColor3f (1, 1, 1);
  text_draw (buffer);


  glPopMatrix ();
  glMatrixMode (GL_PROJECTION);
  glPopMatrix ();
  glMatrixMode (GL_MODELVIEW);
  buffer[0] = 0;
  
}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void TextPrint (const char *fmt, ...)				
{

  char		  text[2048];	
  va_list		ap;		
  
  text[0] = 0;
  if (fmt == NULL)			
		  return;						
  va_start(ap, fmt);		
  vsprintf(text, fmt, ap);				
  va_end(ap);	
  if ((strlen (buffer) + strlen (text)) < MAX_BUFFER) 
    strcat (buffer, text);
  strcat (buffer, "\n");

}

