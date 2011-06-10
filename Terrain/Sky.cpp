/*-----------------------------------------------------------------------------

  Sky.cpp

-------------------------------------------------------------------------------

  Handles the rendering of the sky.  Gradient horizon, sun, moon, stars, etc.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include "camera.h"
#include "env.h"
#include "math.h"
#include "sdl.h"
#include "texture.h"
#include "vbo.h"

#define STAR_TILE   3
#define DISC        8

struct skyvert
{
  GLvector      pos;
  GLrgba        color;
  int           env_color;
};

static VBO        starcube;
static unsigned   texture_stars;
static skyvert    sky[DISC];
static skyvert    tip;
static float      star_fade;

/*-----------------------------------------------------------------------------


-----------------------------------------------------------------------------*/

static void build_sky ()
{

  vector<GLvector>    vert;
  vector<GLvector>    normal;
  vector<GLvector2>   uv;
  vector<unsigned>    index;

  texture_stars = TextureIdFromName ("stars2.bmp");
  //0 NW corner
  vert.push_back (glVector (-1.0f, -1.0f, -0.2f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (0.0f, 0.0f));
  //1 NE corner
  vert.push_back (glVector (1.0f, -1.0f, -0.2f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (STAR_TILE, 0.0f));
  //2 SE corner
  vert.push_back (glVector (1.0f, 1.0f, -0.2f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (STAR_TILE, STAR_TILE));
  //3 SW corner
  vert.push_back (glVector (-1.0f, 1.0f, -0.2f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (0.0f, STAR_TILE));
  //4 Top
  vert.push_back (glVector (0.0f, 0.0f, 0.5f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (STAR_TILE / 2, STAR_TILE / 2));
  //5 bottom
  vert.push_back (glVector (0.0f, 0.0f, -1.0f));
  normal.push_back (glVector (0.0f, 0.0f, 1.0f));
  uv.push_back (glVector (STAR_TILE / 2, STAR_TILE / 2));
  //West triangle
  index.push_back (0);  index.push_back (4);  index.push_back (3);
  //North Triangle
  index.push_back (1);  index.push_back (4);  index.push_back (0);
  //East Triangle
  index.push_back (2);  index.push_back (4);  index.push_back (1);
  //south Triangle
  index.push_back (3);  index.push_back (4);  index.push_back (2);
  //Now the bottom side
  //West triangle
  index.push_back (3);  index.push_back (5);  index.push_back (0);
  //North Triangle
  index.push_back (0);  index.push_back (5);  index.push_back (1);
  //East Triangle
  index.push_back (1);  index.push_back (5);  index.push_back (2);
  //south Triangle
  index.push_back (2);  index.push_back (5);  index.push_back (3);

  starcube.Create (GL_TRIANGLES, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);


}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void SkyInit ()
{

  float   angle;

  build_sky ();
  tip.env_color = ENV_COLOR_TOP;
  tip.pos = glVector (0.0f, 0.0f, 0.15f);
  for (int i = 0; i < DISC; i++) {
    angle = 22.5f + (((float)i / DISC) * 360.0f) * DEGREES_TO_RADIANS; 
    sky[i].pos.x = sin (angle);
    sky[i].pos.y = -cos (angle);
    sky[i].pos.z = -0.1f;
    if (abs (sky[i].pos.y) > abs (sky[i].pos.x)) { //North or south
      if (sky[i].pos.y < 0.0f) 
        sky[i].env_color = ENV_COLOR_NORTH;
      else
        sky[i].env_color = ENV_COLOR_SOUTH;
    } else { //east or west
      if (sky[i].pos.x < 0.0f) 
        sky[i].env_color = ENV_COLOR_WEST;
      else
        sky[i].env_color = ENV_COLOR_EAST;
    }
  }


}

void SkyUpdate ()
{

  Env*    e;

  e = EnvGet ();
  for (int i = 0; i < DISC; i++) {
    sky[i].color = e->color[sky[i].env_color];  
  }
  tip.color = e->color[ENV_COLOR_TOP];
  star_fade = e->star_fade;

}


void SkyRender ()
{

  GLvector angle;

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
  glColor3fv (&tip.color.red);
  glVertex3fv (&tip.pos.x);
  for (int i = 0; i <= DISC; i++) {
    glColor3fv (&sky[i % DISC].color.red);
    glVertex3fv (&sky[i % DISC].pos.x);
  }
  glEnd ();
  /*
  glBegin (GL_QUAD_STRIP);
  for (int i = 0; i <= DISC; i++) {
    glColor3fv (&sky[i % DISC][1].color.red);
    glVertex3fv (&sky[i % DISC][1].pos.x);
    glColor3fv (&sky[i % DISC][0].color.red);
    glVertex3fv (&sky[i % DISC][0].pos.x);
  }
  glEnd ();
  */
  glEnable (GL_BLEND);
  glEnable (GL_TEXTURE_2D);
  glBlendFunc (GL_ONE, GL_ONE);
  glBindTexture (GL_TEXTURE_2D, texture_stars);
  glColor3f (star_fade,star_fade,star_fade);

  starcube.Render ();

  glEnable (GL_LIGHTING);
  glDepthMask (true);
  glPopMatrix ();

}