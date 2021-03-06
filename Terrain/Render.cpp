/*-----------------------------------------------------------------------------

  render.cpp

-------------------------------------------------------------------------------

  This module kicks off most of the rendering jobs and handles the GL setup.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#include <math.h> 
#include "avatar.h"
#include "cache.h"
#include "cg.h"
#include "console.h"
#include "env.h"
#include "input.h"
#include "render.h"
#include "scene.h"
#include "sdl.h"
#include "texture.h"
#include "text.h"
#include "world.h"

#define RENDER_DISTANCE     1536
#define NEAR_CLIP           0.2f
#define FOV                 120
#define MAP_SIZE            512

static int            view_width;
static int            view_height;
static float          view_aspect;
static SDL_Surface*   screen;
static int            max_dimension;
static bool           show_map;
static GLrgba         current_ambient;
static GLrgba         current_diffuse;
static GLrgba         current_fog;
static float          fog_min;
static float          fog_max;
	
/*** static Functions *******************************************************/

static void draw_water (float tile)
{

  int     edge;

  edge = REGION_SIZE * WORLD_GRID;
  glBegin (GL_QUADS);
  glNormal3f (0, 0, 1);

  glTexCoord2f (0, 0);
  glVertex3i (edge, edge, 0);

  glTexCoord2f (0, -tile);
  glVertex3i (edge, 0, 0);

  glTexCoord2f (tile, -tile);
  glVertex3i (0, 0, 0);

  glTexCoord2f (tile, 0);
  glVertex3i (0, edge, 0);
  glEnd ();


}

/*
void  water_map (bool underwater)
{

  GLtexture*      t;

  glBindTexture (GL_TEXTURE_2D, RegionMap ());
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
  //glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  glEnable (GL_TEXTURE_2D);
  glEnable (GL_BLEND);
//  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glBlendFunc (GL_ONE, GL_ONE);
  glColor3f (1.0f, 1.0f, 1.0f);
  glDepthMask (false);
  draw_water (1);
  glDepthMask (true);
  return;
  if (!underwater) {
    t = TextureFromName ("water1.bmp", MASK_LUMINANCE);
    t = TextureFromName ("water.bmp");
    glBindTexture (GL_TEXTURE_2D, t->id);
  	//glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
    glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_LINEAR);	
    //glBlendFunc (GL_ZERO, GL_SRC_COLOR);
    glBlendFunc (GL_ONE, GL_ONE);
    glColor4f (1.0f, 1.0f, 1.0f, 1.0f);
    draw_water (256);
  }

}*/


/*** Module Functions *******************************************************/


void RenderClick (int x, int y)
{

  GLvector    p;

  if (!show_map)
    return;
  y -= view_height - MAP_SIZE;
  if (y < 0 || x > MAP_SIZE)
    return;
  p.x = (float) x / MAP_SIZE;
  p.y = (float) y / MAP_SIZE;
  p.x *= WORLD_GRID * REGION_SIZE;
  p.y *= WORLD_GRID * REGION_SIZE;
  p.z = REGION_SIZE;
  AvatarPositionSet (p);

}

void RenderCanvasBegin (int left, int right, int bottom, int top, int size)
{

  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glDisable(GL_DEPTH_TEST);
  glDisable(GL_LIGHTING);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glEnable (GL_TEXTURE_2D);
  if (size)
    glViewport (0, 0, size, size);
  else 
    glViewport (0, 0, view_width, view_height);
  glMatrixMode (GL_PROJECTION);
  glPushMatrix ();
  glLoadIdentity ();
  glOrtho (left, right, bottom, top, 0.1f, 2048);
  glMatrixMode (GL_MODELVIEW);
  glPushMatrix ();
  glLoadIdentity();
  glTranslatef(0, 0, -10.0f);

}

void RenderCanvasEnd ()
{

  glMatrixMode (GL_PROJECTION);
  glPopMatrix ();  
	glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MIN_FILTER,GL_NEAREST);	
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  glMatrixMode (GL_MODELVIEW);
  glPopMatrix ();  

}

void RenderInit  (void)		
{

  current_ambient = glRgba (0.0f);
  current_diffuse = glRgba (1.0f);
  current_fog = glRgba (1.0f);
  fog_max = 1000;
  fog_min = 1;
  CgInit ();

}

void RenderCreate (int width, int height, int bits, bool fullscreen)
{

  int         flags;
  float       fovy;
  int         d;
  int         size;

  ConsoleLog ("RenderCreate: Creating %dx%d viewport", width, height);
  SDL_GL_SetAttribute (SDL_GL_DOUBLEBUFFER, 1); 
  view_width = width;
  view_height = height;
  view_aspect = (float)width / (float)height;
  flags = SDL_OPENGL;
  if (fullscreen)
    flags |= SDL_FULLSCREEN;
  else
    flags |= SDL_RESIZABLE;
  screen = SDL_SetVideoMode (width, height, bits, flags); 
  if (!screen) 
	  ConsoleLog ("Unable to set video mode: %s\n", SDL_GetError());

  glMatrixMode (GL_PROJECTION);
  glLoadIdentity ();
  fovy = FOV;
  if (view_aspect > 1.0f) 
    fovy /= view_aspect; 
  gluPerspective (fovy, view_aspect, NEAR_CLIP, RENDER_DISTANCE);
  //gluPerspective (fovy, view_aspect, 0.1f, 400);
	glMatrixMode (GL_MODELVIEW);
  size = min (width, height); 
  d = 128;
  while (d < size) {
    max_dimension = d;
    d *= 2;
  }
  TexturePurge ();
  SceneTexturePurge ();
  WorldTexturePurge ();
  CgCompile ();
  TextCreate (width, height);  

}

//Return the power of 2 closest to the smallest dimension of the canvas
//(This tells you how much room you have for drawing on textures.)
int RenderMaxDimension ()
{

  //return 64;///////////////////////////////////////////
  return max_dimension;

}

void RenderTexture (unsigned id)
{


  glMatrixMode (GL_PROJECTION);
  glPushMatrix ();
  glLoadIdentity ();
  glOrtho (0, view_width, view_height, 0, 0.1f, 2048);
	glMatrixMode (GL_MODELVIEW);
  glPushMatrix ();
  glLoadIdentity();
  glTranslatef(0, 0, -1.0f);				
  glDisable (GL_CULL_FACE);
  glDisable (GL_FOG);
  glDisable(GL_DEPTH_TEST);
  glDisable(GL_LIGHTING);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glDisable (GL_FOG);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glEnable (GL_TEXTURE_2D);


  glColor3f (1, 1, 1);

  glBindTexture (GL_TEXTURE_2D, id);
  glTexParameteri (GL_TEXTURE_2D,GL_TEXTURE_MAG_FILTER,GL_NEAREST);	
  glEnable (GL_TEXTURE_2D);
  glDisable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    
  glBegin (GL_QUADS);

  glTexCoord2f (0, 0);
  glVertex3f (0, (float)view_height, 0);

  glTexCoord2f (0, 1);
  glVertex3f (0, (float)view_height - MAP_SIZE, 0);

  glTexCoord2f (1, 1);
  glVertex3f (MAP_SIZE, (float)view_height - MAP_SIZE, 0);

  glTexCoord2f (1, 0);
  glVertex3f (MAP_SIZE, (float)view_height, 0);
  glEnd ();
  if (1) {
    static int    r;
    GLrgba        c;
    GLvector      pos;

    r++;
    c = glRgbaUnique (r);
    glBindTexture (GL_TEXTURE_2D, 0);
    pos = AvatarCameraPosition ();
    pos /= (WORLD_GRID * REGION_SIZE);
    //pos.y /= (WORLD_GRID * REGION_SIZE);
    pos *= MAP_SIZE;
    pos.y += view_height - MAP_SIZE;
    glColor3fv (&c.red);
    glBegin (GL_QUADS);
    glVertex3f (pos.x, pos.y, 0);
    glVertex3f (pos.x + 10, pos.y, 0);
    glVertex3f (pos.x + 10, pos.y + 10, 0);
    glVertex3f (pos.x, pos.y + 10, 0);
    glEnd ();
  }
  

  glPopMatrix ();
  glMatrixMode (GL_PROJECTION);
  glPopMatrix ();
  glMatrixMode (GL_MODELVIEW);

}


static float    spin;


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void RenderUpdate (void)		
{
  
  Env*        e;

  e = EnvGet ();
  current_diffuse = e->color[ENV_COLOR_LIGHT];
  current_ambient = e->color[ENV_COLOR_AMBIENT];
  current_fog = e->color[ENV_COLOR_FOG];
  fog_min = e->fog.rmin;
  fog_max = e->fog.rmax;
  if (InputKeyPressed (SDLK_TAB)) 
    show_map = !show_map;

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void RenderLoadingScreen (float progress)
{

  glClearColor (0.0f, 0.0f, 0.0f, 1.0f);
  glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  glColor3f (1.0f - progress, progress, 0.0f);
  glLineWidth (10.0f);
  glDisable(GL_LIGHTING);
  glDisable(GL_TEXTURE_2D);
  glDisable (GL_BLEND);
  glBindTexture (GL_TEXTURE_2D, 0);
  RenderCanvasBegin (-20, 120, 0, 100, 0);
  glBegin (GL_LINES);
  glVertex2f (0, 50);
  glVertex2f (progress * 100, 50);
  glEnd ();
  RenderCanvasEnd ();
  TextRender ();
  ConsoleRender ();
  SDL_GL_SwapBuffers ();

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void Render (void)		
{

  GLvector        pos;
  GLvector        angle;
  Env*            e;
  float           water_level;

  pos = AvatarCameraPosition ();
  e = EnvGet ();
  water_level = WorldWaterLevel ((int)pos.x, (int)pos.y);
  water_level = max (water_level, 0);
  if (pos.z >= water_level) {
    //cfog = (current_diffuse + glRgba (0.0f, 0.0f, 1.0f)) / 2;
    //glFogf(GL_FOG_START, RENDER_DISTANCE / 2);				// Fog Start Depth
    //glFogf(GL_FOG_END, RENDER_DISTANCE);				// Fog End Depth
    glFogf(GL_FOG_START, e->fog.rmin);				// Fog Start Depth
    glFogf(GL_FOG_END, e->fog.rmax);				// Fog End Depth
  } else {
    //cfog = glRgba (0.0f, 0.5f, 0.8f);
    glFogf(GL_FOG_START, 1);				// Fog Start Depth
    glFogf(GL_FOG_END, 32);				// Fog End Depth
  }
  glEnable (GL_FOG);
  glFogi (GL_FOG_MODE, GL_LINEAR);
  //glFogi (GL_FOG_MODE, GL_EXP);
  glFogfv (GL_FOG_COLOR, &e->color[ENV_COLOR_FOG].red);
  glClearColor (e->color[ENV_COLOR_FOG].red, e->color[ENV_COLOR_FOG].green, e->color[ENV_COLOR_FOG].blue, 0.0f);
  //glClearColor (0, 0, 0, 1.0f);
  glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  //glClear (GL_DEPTH_BUFFER_BIT);
  {
    float light[4];			

    light[0] = -e->light.x;
    light[1] = -e->light.y;
    light[2] = -e->light.z;
    light[3] = 0.0f;

    glEnable(GL_LIGHT1);
    glEnable(GL_LIGHTING);
    current_ambient = glRgba (0.0f);
    glLightfv (GL_LIGHT1, GL_AMBIENT, &e->color[ENV_COLOR_AMBIENT].red);			
    GLrgba  c = e->color[ENV_COLOR_LIGHT];
    //c *= 20.0f;
    glLightfv (GL_LIGHT1, GL_DIFFUSE, &c.red);	
    glLightfv (GL_LIGHT1, GL_POSITION,light);

  }
  glViewport (0, 0, view_width, view_height);
  glDepthFunc (GL_LEQUAL);
  glEnable(GL_DEPTH_TEST);
  //Culling and shading
  glShadeModel(GL_SMOOTH);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glEnable (GL_CULL_FACE);
  glCullFace (GL_BACK);
  //Alpha blending  
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glEnable (GL_ALPHA_TEST);
  glAlphaFunc (GL_GREATER, 0.0f);

  glLineWidth (2.0f);
  //

  //glMatrixMode (GL_MODELVIEW);

  //Move into our unique coordanate system
  glLoadIdentity();
  pos = AvatarCameraPosition ();
  glScalef (1, -1, 1);
  angle = AvatarCameraAngle ();
  glRotatef (angle.x, 1.0f, 0.0f, 0.0f);
  glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
  glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
  glTranslatef (-pos.x, -pos.y, -pos.z);

  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

  if (CVarUtils::GetCVar<bool> ("render.shaders"))
    CgUpdate ();
  SceneRender ();
  CgShaderSelect (VSHADER_NONE);
  if (CVarUtils::GetCVar<bool> ("render.wireframe"))
    SceneRenderDebug ();
  if (CVarUtils::GetCVar<bool> ("show.pages"))
    CacheRenderDebug ();
  TextRender ();
  if (show_map) 
    RenderTexture (WorldMap ());
  ConsoleRender ();
  SDL_GL_SwapBuffers ();

}


/*
  if (0) { //water reflection effect.  Needs stencil buffer to work right
    glDisable (GL_FOG);
    glPushMatrix ();
    glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("water4.bmp"));
    glColorMask (false, false, false, true);
    draw_water (256);
    glColorMask (true, true, true, false);
    glLoadIdentity();
    pos = CameraPosition ();
    glScalef (1, -1, -1);
    //pos *= -1;
    angle = CameraAngle ();
    glRotatef (angle.x, -1.0f, 0.0f, 0.0f);
    glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
    glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
    glTranslatef (-pos.x, -pos.y, pos.z);
    glDepthFunc (GL_GREATER);
   // glScalef (1, -1, 1);
    glFrontFace (GL_CW);
    glPolygonMode(GL_BACK, GL_FILL);
    //glPolygonMode(GL_FRONT, GL_POINT);
    glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    glDisable (GL_CULL_FACE);
    SceneRender ();
    glDepthFunc (GL_LEQUAL);
    glPopMatrix ();
    glFrontFace (GL_CCW);
    //glEnable (GL_BLEND);
    //glBlendFunc (GL_ONE, GL_ONE);
    //glDisable (GL_LIGHTING);
    //glColor4f (1.0f, 1.0f, 1.0f, 0.5f);
    glBindTexture (GL_TEXTURE_2D, TextureIdFromName ("water4.bmp"));
    glColorMask (false, false, false, false);
    draw_water (256);
    glColorMask (true, true, true, false);
    //draw_water (256);
    glEnable (GL_LIGHTING);
    glPolygonMode(GL_FRONT, GL_FILL);
    glPolygonMode(GL_BACK, GL_LINE);

  }
  */