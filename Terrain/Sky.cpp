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

static VBO        starcube;
static unsigned   texture_stars;
static float      stars;

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

  build_sky ();

}

void SkyUpdate ()
{

  float   elapsed;

  elapsed = SdlElapsedSeconds () * ENV_TRANSITION;
  stars = MathInterpolate (stars, EnvStars (), elapsed);


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
  glEnable (GL_BLEND);
  glBlendFunc (GL_ONE, GL_ONE);
  glBindTexture (GL_TEXTURE_2D, texture_stars);
  glColor3f (1,1,1);

  starcube.Render ();

  glEnable (GL_LIGHTING);
  glDepthMask (true);
  glPopMatrix ();

}