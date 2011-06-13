/*-----------------------------------------------------------------------------

  render.cpp


-------------------------------------------------------------------------------

  This module kicks off most of the rendering jobs and handles the GL setup.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#define RENDER_DISTANCE     1024
#define FOV                 90

#include <math.h>
#include "camera.h"
#include "env.h"
#include "log.h"
#include "input.h"
//#include "math.h"
#include "render.h"
#include "region.h"
#include "scene.h"
#include "sdl.h"
#include "sky.h"
#include "texture.h"
#include "text.h"
#include "world.h"
#include "water.h"

static int            view_width;
static int            view_height;
static float          view_aspect;
static SDL_Surface*   screen;
static int            max_dimension;
static int            terrain_debug;
static bool           world_debug;
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

  edge = REGION_SIZE * REGION_GRID;
  glDisable (GL_CULL_FACE);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glBegin (GL_QUADS);
  glNormal3f (0, 0, 1);

  glTexCoord2f (0, 0);
  glVertex3i (0, 0, 0);

  glTexCoord2f (0, -tile);
  glVertex3i (0, edge, 0);

  glTexCoord2f (tile, -tile);
  glVertex3i (edge, edge, 0);

  glTexCoord2f (tile, 0);
  glVertex3i (edge, 0, 0);
  glEnd ();


}

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

}


/*** Module Functions *******************************************************/

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
  glViewport (0, 0, size, size);

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
  

}

void RenderCreate (int width, int height, int bits, bool fullscreen)
{

  int         flags;
  float       fovy;
  int         d;
  int         size;

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
	  Log ("Unable to set video mode: %s\n", SDL_GetError());

  glMatrixMode (GL_PROJECTION);
  glLoadIdentity ();
  fovy = FOV;
  if (view_aspect > 1.0f) 
    fovy /= view_aspect; 
  //gluPerspective (fovy, view_aspect, 0.1f, RENDER_DISTANCE);
  gluPerspective (fovy, view_aspect, 0.1f, 400);
	glMatrixMode (GL_MODELVIEW);
  size = min (width, height); 
  d = 128;
  while (d < size) {
    max_dimension = d;
    d *= 2;
  }
  SceneTexturePurge ();
  TexturePurge ();
  TextCreate (width, height);  

}

//Return the power of 2 closest to the smallest dimension of the canvas
//(This tells you how much room you have for drawing on textures.)
int RenderMaxDimension ()
{

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
  glEnable (GL_TEXTURE_2D);
  glDisable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    
  glBegin (GL_QUADS);

  glTexCoord2f (0, 1);
  glVertex3f (0, 0, 0);

  glTexCoord2f (0, 0);
  glVertex3f (0, 512, 0);

  glTexCoord2f (1, 0);
  glVertex3f (512, 512, 0);

  glTexCoord2f (1, 1);
  glVertex3f (512, 0, 0);
  glEnd ();

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
  
//  GLvector    pos;
  float       elapsed;
//  float       scale;
//  Region      r;
//  GLrgba      desired_diffuse, desired_ambient, desired_fog;
  GLvector2   fog_desired;
  Env*        e;

  e = EnvGet ();
  //spin += elapsed * 30.0f;
  current_diffuse = e->color[ENV_COLOR_LIGHT];
  current_ambient = e->color[ENV_COLOR_AMBIENT];
  current_fog = e->color[ENV_COLOR_FOG];
  fog_min = e->fog_min;
  fog_max = e->fog_max;
  if (InputKeyPressed (SDLK_F3)) {
    terrain_debug++;
    terrain_debug %= DEBUG_RENDER_TYPES;
  }
  if (InputKeyPressed (SDLK_F4)) 
    world_debug = !world_debug;
  if (InputKeyPressed (SDLK_TAB)) 
    show_map = !show_map;


}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void Render (void)		
{

  GLvector        pos;
  GLvector        angle;
  Env*            e;
  float           water_level;

  pos = CameraPosition ();
  e = EnvGet ();
  water_level = RegionWaterLevel ((int)pos.x, (int)pos.y);
  water_level = max (water_level, 0);
  if (pos.z >= water_level) {
    //cfog = (current_diffuse + glRgba (0.0f, 0.0f, 1.0f)) / 2;
    //glFogf(GL_FOG_START, RENDER_DISTANCE / 2);				// Fog Start Depth
    //glFogf(GL_FOG_END, RENDER_DISTANCE);				// Fog End Depth
    glFogf(GL_FOG_START, e->fog_min);				// Fog Start Depth
    glFogf(GL_FOG_END, e->fog_max);				// Fog End Depth
  } else {
    //cfog = glRgba (0.0f, 0.5f, 0.8f);
    glFogf(GL_FOG_START, 1);				// Fog Start Depth
    glFogf(GL_FOG_END, 32);				// Fog End Depth
  }
  glEnable (GL_FOG);
  glFogi (GL_FOG_MODE, GL_LINEAR);
  //glFogi (GL_FOG_MODE, GL_EXP);
  glFogfv (GL_FOG_COLOR, &e->color[ENV_COLOR_FOG].red);
  glClearColor (e->color[ENV_COLOR_FOG].red, e->color[ENV_COLOR_FOG].green, e->color[ENV_COLOR_FOG].blue, 1.0f);
  //glClearColor (0, 0, 0, 1.0f);
  glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  //glClear (GL_DEPTH_BUFFER_BIT);
  {
    //float LightAmbient[]= { current_ambient.red, fog.green, fog.blue, 1.0f }; 				// Ambient Light Values ( NEW )
    //float LightDiffuse[]= { diffuse.red, diffuse.green, diffuse.blue, 1.0f };
    float light[4];			

    light[0] = -e->light.x;
    light[1] = -e->light.y;
    light[2] = -e->light.z;
    light[3] = 0.0f;

    glEnable(GL_LIGHT1);							// Enable Light One
    glEnable(GL_LIGHTING);		// Enable Lighting
    current_ambient = glRgba (0.0f);
    glLightfv (GL_LIGHT1, GL_AMBIENT, &current_ambient.red);				
    glLightfv (GL_LIGHT1, GL_DIFFUSE, &e->color[ENV_COLOR_LIGHT].red);	
    glLightfv (GL_LIGHT1, GL_POSITION,light);			// Position The Light

  }
  //glDisable(GL_LIGHTING);		// Enable Lighting
  glEnable (GL_ALPHA_TEST);
  glAlphaFunc (GL_GREATER, 0.0f);
  glViewport (0, 0, view_width, view_height);
	glHint(GL_PERSPECTIVE_CORRECTION_HINT, GL_NICEST);
  glShadeModel(GL_SMOOTH);
  //glShadeModel (GL_FLAT);
	glDepthFunc (GL_LEQUAL);
  glEnable(GL_DEPTH_TEST);
  glEnable (GL_CULL_FACE);
  glCullFace (GL_BACK);
  glEnable (GL_BLEND);
  glBlendFunc (GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  glMatrixMode (GL_TEXTURE);
  glLoadIdentity();
	glMatrixMode (GL_MODELVIEW);
  glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
  glLoadIdentity();
  glLineWidth (7.0f);
  pos = CameraPosition ();
  //Move into our unique coordanate system
  glScalef (1, -1, 1);
  angle = CameraAngle ();
  glRotatef (angle.x, 1.0f, 0.0f, 0.0f);
  glRotatef (angle.y, 0.0f, 1.0f, 0.0f);
  glRotatef (angle.z, 0.0f, 0.0f, 1.0f);
  glTranslatef (-pos.x, -pos.y, -pos.z);
  SkyRender ();
  if (world_debug) {
    glDisable (GL_FOG);
    glFogf(GL_FOG_START, 2047);				// Fog Start Depth
    glFogf(GL_FOG_END, 2048);				// Fog End Depth
  }
  SceneRender ();
  if (terrain_debug)
    SceneRenderDebug (terrain_debug);
  //WaterRender (pos.z <= 0.0f);
  if (world_debug)
    WorldRenderDebug ();
  TextRender ();
  if (show_map) 
    RenderTexture (RegionMap ());
  SDL_GL_SwapBuffers ();


}

