/*-----------------------------------------------------------------------------
  Sky.cpp
-------------------------------------------------------------------------------
  Handles the rendering of the sky.  Gradient horizon, sun, moon, stars, etc.
-----------------------------------------------------------------------------*/
/*
#include "stdafx.h"

#include "avatar.h"
#include "Cg.h"
#include "env.h"
#include "math.h"
#include "random.h"
#include "sdl.h"
#include "texture.h"
#include "vbo.h"

void SkyInit ();
void SkyRender ();
void SkyUpdate ();

#define UP          glVector (0,0,1)
#define STAR_TILE   3
#define DISC        8
#define SKY_GRID    16
#define SKY_EDGE    (SKY_GRID + 1)
#define SKY_HALF    (SKY_GRID / 2)
#define SKY_DOME    0.5f
#define SKY_TILE    3
//How big the sunrise / set is. Higher = smaller. Don't set lower than near clip plane. 
#define SUNSET_SIZE 0.35f
//The size of the sun itself
#define SUN_SIZE    0.5f


static GLvector   sky[DISC];
static VBO        skydome;
static VBO        stardome;
static unsigned   texture_stars;
static GLvector   tip;
static float      star_fade;

//The sun angle begins at zero on the eastern horizon. 90 degrees is high noon.
//180 is west.
static void draw_sun (float sun_angle, float size)
{
  float     x, z;
  float     s, c;

  sun_angle = (sun_angle - 90.0f) * DEGREES_TO_RADIANS;
  s = -sin (sun_angle);
  c = cos (sun_angle);
  x = s * 3;
  z = c * 3;
  s *= size;
  c *= size;
    
  glBegin (GL_QUADS);
  glTexCoord2f (0.0f, 0.0f);
  glVertex3f (x - c,-size, z + s);
  glTexCoord2f (1.0f, 0.0f);
  glVertex3f (x + c,-size, z - s);
  glTexCoord2f (1.0f, 1.0f);
  glVertex3f (x + c, size, z - s);
  glTexCoord2f (0.0f, 1.0f);
  glVertex3f (x - c, size, z + s);
  glEnd ();
}

static void build_sky ()
{
  vector<GLvector>    vert;
  vector<GLvector>    normal;
  vector<GLvector2>   uv;
  vector<GLrgba>      color;
  vector<unsigned>    index;
  int                 x, y;
  float               dist;
  float               alpha;
  GLvector2           distance;
  GLvector2           uv_step;
  GLvector2           uv_map;
  GLmesh              star;
  GLvector            v;
  int                 x1, x2, y1, y2;

  uv_map = glVector (0.0f, 0.0f);
  texture_stars = TextureIdFromName ("stars2.bmp");
  for (y = 0; y < SKY_EDGE; y++) {
    uv_map.x = 0;
    for (x = 0; x < SKY_EDGE; x++) {
      uv_step.x = abs (((float)x - SKY_HALF) / SKY_HALF);
      uv_step.x *= uv_step.x;
      uv_map.x += uv_step.x + 0.1f;
      distance = glVector ((float)x - SKY_HALF, (float)y - SKY_HALF);
      dist = glVectorLength (distance) / SKY_HALF;
      alpha = 1 - dist;
      alpha = clamp (alpha, 0, 1);
      color.push_back (glRgba (alpha, alpha, alpha, alpha));
      dist = 1.0f - glVectorLength (distance) / (SKY_HALF - 3);
      v = glVector ((float)x - SKY_HALF, (float)y - SKY_HALF, 0.1f + dist * SKY_DOME);
      vert.push_back (v);
      normal.push_back (glVector (0.0f, 0.0f, 1.0f));
      uv.push_back (uv_map * 4);
      star.PushVertex (v, UP, glVector (((float)(x) / SKY_GRID) * SKY_TILE, ((float)(y) / SKY_GRID) * SKY_TILE)); 
    }
    uv_step.y = abs (((float)y - SKY_HALF) / SKY_HALF);
    uv_step.y *= uv_step.y;
    uv_map.y += uv_step.y + 0.1f;
  }
  for (y = 0; y < SKY_GRID; y++) {
    for (x = 0; x < SKY_GRID; x++) {
      x1 = x;
      x2 = x + 1;
      y1 = y * SKY_EDGE;
      y2 = (y + 1) * SKY_EDGE;
      if ((x + y) % 2) {
        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  
        
        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  
        star.PushTriangle (x1 + y1, x2 + y1, x1 + y2);
        star.PushTriangle (x2 + y1, x2 + y2, x1 + y2);
      } else {
        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  

        index.push_back (x + y * SKY_EDGE);  
        index.push_back ((x + 1) + (y + 1) * SKY_EDGE);  
        index.push_back (x + (y + 1) * SKY_EDGE);  

        star.PushTriangle (x1 + y1, x2 + y1, x2 + y2);
        star.PushTriangle (x1 + y1, x2 + y2, x1 + y2);
      }

    }
  }
  skydome.Create (GL_TRIANGLES, index.size (), vert.size (), &index[0], &vert[0], &normal[0], &color[0], &uv[0]);
  stardome.Create (&star);
  //skydome.Create (GL_TRIANGLES, index.size (), vert.size (), &index[0], &vert[0], &normal[0], NULL, &uv[0]);
}

void SkyInit ()
{
  float   angle;

  build_sky ();
  
  tip = glVector (0.0f, 0.0f, 2.7f);
  for (int i = 0; i < DISC; i++) {
    angle = 22.5f + (((float)i / DISC) * 360.0f) * DEGREES_TO_RADIANS; 
    sky[i].x = sin (angle) * 6.0f;
    sky[i].y = -cos (angle) * 6.0f;
    sky[i].z = -0.5f;
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
  GLvector  angle;
  Env*      e;
  GLrgba    color;

  //We want to use a camera space that uses the camera orientation, but not its position.
  e = EnvGet ();
  angle = AvatarCameraAngle ();
  glPushMatrix ();
  glLoadIdentity ();
  glScalef (1, -1, 1);
  glRotatef (angle.x, 1.0f, 0.0f, 0.0f);
  glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
  glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
  glDepthMask (false);
  glDisable (GL_LIGHTING);
  glDisable (GL_BLEND);
  glDisable (GL_CULL_FACE);
  glBindTexture (GL_TEXTURE_2D, 0);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

  //Draw the cone, which forms the horizon.  The tip is darker than the edge.
  glBegin (GL_TRIANGLE_FAN);
  glColor3fv (&e->color[ENV_COLOR_SKY].red);
  glVertex3fv (&tip.x);
  glColor3fv (&e->color[ENV_COLOR_HORIZON].red);
  for (int i = 0; i <= DISC; i++) 
    glVertex3fv (&sky[i % DISC].x);
  glEnd ();
  //Render the skydome
  glEnable (GL_BLEND);
  glBlendFunc (GL_ONE, GL_ONE);
  glBindTexture (GL_TEXTURE_2D, texture_stars);
  glColor3f (star_fade,star_fade,star_fade);
  //skydome.Render ();
  stardome.Render ();
  //Possibly render the sunset / sunrise polygon, which puts a huge gradient in the sky.
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("sunrise.bmp"));
  if (e->sunset_fade > 0.0f) {
    color = e->color[ENV_COLOR_LIGHT] * e->sunset_fade;
    glColor3fv (&color.red);
    glBegin (GL_QUADS);
    glTexCoord2f (0.0f, 0.0f);
    glVertex3f (-SUNSET_SIZE,-1.0f, 0.25f);
    glTexCoord2f (1.0f, 0.0f);
    glVertex3f (-SUNSET_SIZE, 1.0f, 0.25f);
    glTexCoord2f (1.0f, 1.0f);
    glVertex3f (-SUNSET_SIZE, 1.0f,-1.0f);
    glTexCoord2f (0.0f, 1.0f);
    glVertex3f (-SUNSET_SIZE,-1.0f,-1.0f);
    glEnd ();
  }
  if (e->sunrise_fade > 0.0f) {
    color = e->color[ENV_COLOR_LIGHT] * e->sunrise_fade;
    glColor3fv (&color.red);
    glBegin (GL_QUADS);
    glTexCoord2f (0.0f, 0.0f);
    glVertex3f (SUNSET_SIZE,-1.0f, 0.25f);
    glTexCoord2f (1.0f, 0.0f);
    glVertex3f (SUNSET_SIZE, 1.0f, 0.25f);
    glTexCoord2f (1.0f, 1.0f);
    glVertex3f (SUNSET_SIZE, 1.0f,-1.0f);
    glTexCoord2f (0.0f, 1.0f);
    glVertex3f (SUNSET_SIZE,-1.0f,-1.0f);
    glEnd ();
  }
  //Draw the sun

  if (e->draw_sun) {
    //color = e->color[ENV_COLOR_LIGHT];
    glColor3f (1.0f, 1.0f, 0.3f);
    glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("sun.bmp"));
    draw_sun (e->sun_angle, SUN_SIZE);  
  }
  //glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("clouds3.bmp"));
  CgShaderSelect (VSHADER_CLOUDS);
  CgShaderSelect (FSHADER_CLOUDS);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  skydome.Render ();
  CgShaderSelect (VSHADER_NONE);
  CgShaderSelect (FSHADER_NONE);
  //Cleanup and put the modelview matrix back where we found it
  glEnable (GL_LIGHTING);
  glEnable (GL_CULL_FACE);

  glDepthMask (true);
  glPopMatrix ();
}
*/