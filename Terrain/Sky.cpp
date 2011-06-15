/*-----------------------------------------------------------------------------

  Sky.cpp

-------------------------------------------------------------------------------

  Handles the rendering of the sky.  Gradient horizon, sun, moon, stars, etc.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "camera.h"
#include "env.h"
#include "math.h"
#include "random.h"
#include "sdl.h"
#include "texture.h"
#include "vbo.h"

#define STAR_TILE   3
#define DISC        8
#define SKY_GRID    12
#define SKY_EDGE    (SKY_GRID + 1)
#define SKY_HALF    (SKY_GRID / 2)
#define SKY_DOME    0.5f
#define SKY_TILE    5



static GLvector   sky[DISC];
static VBO        skydome;
static unsigned   texture_stars;
static GLvector   tip;
static float      star_fade;

/*-----------------------------------------------------------------------------


-----------------------------------------------------------------------------*/

static void build_sky ()
{

  vector<GLvector>    vert;
  vector<GLvector>    normal;
  vector<GLvector2>   uv;
  vector<unsigned>    index;
  int                 x, y;
  float               dist;
  GLvector2           distance;

  texture_stars = TextureIdFromName ("stars2.bmp");
  for (y = 0; y < SKY_EDGE; y++) {
    for (x = 0; x < SKY_EDGE; x++) {
      distance = glVector ((float)x - SKY_HALF, (float)y - SKY_HALF);
      //offset = max (abs (x - SKY_HALF), abs (y - SKY_HALF));
      //dist = 1.0f - ((float)offset / (SKY_HALF));
      dist = 1.0f - glVectorLength (distance) / (SKY_HALF - 3);
      //vert.push_back (glVector ((float)x - SKY_HALF, (float)y - SKY_HALF, dist * SKY_DOME) - SKY_DOME / 8);
      vert.push_back (glVector ((float)x - SKY_HALF, (float)y - SKY_HALF, dist * SKY_DOME));
      normal.push_back (glVector (0.0f, 0.0f, 1.0f));
      //uv.push_back (glVector (((float)x / SKY_GRID) * SKY_TILE, ((float)y / SKY_GRID) * SKY_TILE));
      uv.push_back (glVector (((float)(x) / SKY_GRID) * SKY_TILE, ((float)(y) / SKY_GRID) * SKY_TILE));

    }
  }
  for (y = 0; y < SKY_GRID; y++) {
    for (x = 0; x < SKY_GRID; x++) {
      if ((x + y) % 2) {
        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  

        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  
      } else {
        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  

        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  
      }

    }
  }
  skydome.Create (GL_TRIANGLES, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);
  //skydome.Create (GL_LINE_STRIP, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);


}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SkyInit ()
{

  float   angle;

  build_sky ();
  
  tip = glVector (0.0f, 0.0f, 0.2f);
  for (int i = 0; i < DISC; i++) {
    angle = 22.5f + (((float)i / DISC) * 360.0f) * DEGREES_TO_RADIANS; 
    sky[i].x = sin (angle) * 6.0f;
    sky[i].y = -cos (angle) * 6.0f;
    sky[i].z = -0.1f;
  }

  
}

void SkyUpdate ()
{

  Env*    e;

  e = EnvGet ();
  star_fade = e->star_fade;

}


void SkyRender ()
{

  GLvector angle;
  Env*    e;

  e = EnvGet ();
  //return;
  angle = CameraAngle ();
  glPushMatrix ();
  glLoadIdentity ();
  glScalef (1, -1, 1);
  glRotatef (angle.x, 1.0f, 0.0f, 0.0f);
  glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
  glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
  glDepthMask (false);
  glDisable (GL_LIGHTING);
  glDisable (GL_BLEND);
  glDisable (GL_TEXTURE_2D);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  //glBegin (GL_TRIANGLE_FAN);
  
  glBegin (GL_TRIANGLE_FAN);
  glColor3fv (&e->color[ENV_COLOR_SKY].red);
  glVertex3fv (&tip.x);
  glColor3fv (&e->color[ENV_COLOR_HORIZON].red);
  for (int i = 0; i <= DISC; i++) 
    glVertex3fv (&sky[i % DISC].x);
  glEnd ();

  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_ONE, GL_ONE);
  glBindTexture (GL_TEXTURE_2D, texture_stars);
  glColor3f (star_fade,star_fade,star_fade);
  skydome.Render ();

/*
  GLtexture* t;
  t = TextureFromName ("clouds2.bmp", MASK_LUMINANCE);

  //glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("clouds2.bmp"));
  glBindTexture (GL_TEXTURE_2D, t->id);
  //glBindTexture (GL_TEXTURE_2D, 0);
  glColor3f (0.3f,0.3f,0.3f);
  //starcube.Render ();
  glEnable (GL_ALPHA_TEST);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBlendFunc (GL_ONE, GL_ONE);
  glColor3f (0.1f,0.1f,0.1f);

  static float    x;
  float o;

  glMatrixMode (GL_TEXTURE);
  glLoadIdentity ();
  x += 0.001f;
  o = sin (x);

  glAlphaFunc (GL_GREATER, abs (o));

  glTranslatef (x, 0.0f, 5.0f);
  glPolygonMode(GL_FRONT_AND_BACK, GL_LINES);
  //glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBlendFunc (GL_SRC_COLOR, GL_DST_COLOR);
  //glBlendFunc (GL_ONE_MINUS_SRC_ALPHA, GL_SRC_ALPHA);
  glBlendFunc (GL_ONE, GL_ONE);
  //skydome.Render ();
  glLoadIdentity ();


  glMatrixMode (GL_MODELVIEW);
  
  glAlphaFunc (GL_GREATER, 0.0f);
  */
  glEnable (GL_LIGHTING);

  glDepthMask (true);
  glPopMatrix ();
  

}