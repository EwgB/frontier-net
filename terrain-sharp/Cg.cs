/*-----------------------------------------------------------------------------
  CTerrain.cpp
-------------------------------------------------------------------------------
  This holds the terrain object class.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include "avatar.h"
#include "console.h"
#include "cg.h"
#include "env.h"
#include "ini.h"
#include "scene.h"
#include "sdl.h"
#include "texture.h"
#include <cg\cg.h>									
#include <cg\cggl.h>

enum
{
  VSHADER_NONE = -1,
  VSHADER_NORMAL,
  VSHADER_TREES,
  VSHADER_GRASS,
  VSHADER_CLOUDS,
  VSHADER_COUNT,
  FSHADER_NONE,
  FSHADER_GREEN,
  FSHADER_CLOUDS,
  FSHADER_MASK_TRANSFER,
  FSHADER_END,
};

#define FSHADER_BASE  (FSHADER_NONE + 1)
#define FSHADER_COUNT (FSHADER_END - FSHADER_BASE)

void CgCompile ();
void CgInit ();
void CgUpdate ();
void CgUpdateMatrix ();
void CgSetOffset (GLvector offset);
void CgShaderSelect (int shader);

#define VSHADER_FILE  "shaders/vertex.cg"
#define FSHADER_FILE  "shaders/fragment.cg"
#define MAX_FILE_NAME 100

static char*          vshader_function[] =
{
  "standard",
  "trees",
  "grass",
  "clouds",
};

static char*          fshader_function[] =
{
  "green",
  "clouds",
  "mask_transfer",
};

struct VShader
{
  CGprogram	  program;
  CGprofile	  profile;
  CGparameter	position;
  CGparameter	offset;
  CGparameter	matrix;
  CGparameter	lightpos;
  CGparameter	lightcol;
  CGparameter	ambientcol;
  CGparameter	eyepos;
  CGparameter	fog;
  CGparameter	data;
};

struct FShader
{
  CGprogram	  program;
  CGprofile	  profile;
  CGparameter	texture;
  CGparameter	fogcolor;
  CGparameter	data;
};

static CGcontext	    cgContext;				// A Context To Hold Our Cg Program(s)
static CGprogram	    cgProgram;				// Our Cg Vertex Program
static CGprofile	    cgp_vertex;	
static CGprofile	    cgp_fragment;	
static VShader        vshader_list[VSHADER_COUNT];
static FShader        fshader_list[FSHADER_COUNT];
static float          wind;
static int            vshader_selected;
static int            fshader_selected;

static void checkForCgError(CGerror error, const char* program, const char *situation)
{
  if (error != CG_NO_ERROR) 
    ConsoleLog ("%s: %s: %s", program, situation, cgGetErrorString(error));
  else
    ConsoleLog ("%s: %s... ok.", program, situation);
}

static void fshader_select (int select_in)
{
  FShader*      s;
  Env*          e;
   
  fshader_selected = select_in - FSHADER_BASE;
  if (fshader_selected == -1 || !CVarUtils::GetCVar<bool> ("render.textured")) {
    cgGLDisableProfile (cgp_fragment);
    return;
  }
  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  s = &fshader_list[fshader_selected];
  e = EnvGet ();
  cgGLEnableProfile (cgp_fragment);
  cgGLBindProgram (s->program);
  if (select_in == FSHADER_CLOUDS) {
    GLrgba c = (e->color[ENV_COLOR_SKY] + e->color[ENV_COLOR_HORIZON]) / 2.0f;
    cgGLSetParameter3f (s->fogcolor, c.red, c.green, c.blue);
  } else
    cgGLSetParameter3f (s->fogcolor, e->color[ENV_COLOR_FOG].red, e->color[ENV_COLOR_FOG].green, e->color[ENV_COLOR_FOG].blue);
  cgGLSetTextureParameter (s->texture, TextureIdFromName ("clouds.png"));
  cgGLSetParameter4f (s->data, wind, e->cloud_cover, 1 - e->star_fade, 0);
  cgGLEnableTextureParameter (s->texture);
}

static void vshader_select (int select)
{
  VShader*      s;
  GLvector      p;
  Env*          e;
  GLrgba        c;
  float         val1, val2;
     
  vshader_selected = select;
  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  if (select == VSHADER_NONE) {
    cgGLDisableProfile (cgp_vertex);
    return;
  }
  val1 = val2 = 0.0f;
  if (select == VSHADER_TREES || select == VSHADER_GRASS) 
    val1 = wind;
  if (select == VSHADER_CLOUDS) 
    val1 = wind / 5;
  s = &vshader_list[select];
  e = EnvGet ();
  cgGLEnableProfile (cgp_vertex);
  cgGLBindProgram (s->program);
  cgGLSetParameter3f (s->lightpos, -e->light.x, -e->light.y, -e->light.z);
  c = e->color[ENV_COLOR_LIGHT];
  cgGLSetParameter3f (s->lightcol, c.red, c.green, c.blue);
  c = e->color[ENV_COLOR_AMBIENT] * glRgba (0.2f, 0.2f, 1.0f);
  cgGLSetParameter3f (s->ambientcol, c.red, c.green, c.blue);
  p = AvatarCameraPosition ();
  cgGLSetParameter3f (s->eyepos, p.x, p.y, p.z);
  cgGLSetStateMatrixParameter(s->matrix, CG_MatrixMode.Modelview_PROJECTION_MATRIX, CG_MatrixMode.Modelview_MATRIX);
  cgGLSetParameter2f (s->fog, e->fog.rmin, e->fog.rmax);
  cgGLSetParameter4f (s->data, SceneVisibleRange (), SceneVisibleRange () * 0.05, val1, val2);
  glColor3f (1,1,1);
}

void CgCompile ()
{
  VShader*    s;
  FShader*    fs;
  unsigned    i;

  //Setup Cg
  cgContext = cgCreateContext();				
  checkForCgError (cgGetError(), "Init", "Establishing Cg context");
  
  cgp_fragment = cgGLGetLatestProfile (CG_GL_FRAGMENT);
  checkForCgError (cgGetError(), "Init", "Establishing Cg Fragment profile");
  cgGLEnableProfile (cgp_fragment);
  cgGLSetOptimalOptions (cgp_fragment);
  
  //Now set up our list of shaders
  for (i = 0; i < FSHADER_COUNT; i++) {
    fs = &fshader_list[i];
    // Load And Compile The Vertex Shader From File
    fs->program = cgCreateProgramFromFile (cgContext, CG_SOURCE, FSHADER_FILE, cgp_fragment, fshader_function[i], 0);
    checkForCgError (cgGetError(), fshader_function[i], "Compiling");
    // Load The Program
	  cgGLLoadProgram (fs->program);
    cgGLBindProgram (fs->program);
    checkForCgError (cgGetError(), fshader_function[i], "Binding");
    fs->texture   = cgGetNamedParameter (fs->program, "texture2");
    fs->fogcolor  = cgGetNamedParameter (fs->program, "fogcolor");
    fs->data      = cgGetNamedParameter (fs->program, "data");
    checkForCgError (cgGetError(), fshader_function[i], "Loading variables");
  }
  
  cgp_vertex = cgGLGetLatestProfile (CG_GL_VERTEX);	
  checkForCgError (cgGetError(), "Init", "Establishing Cg Vertex profile");
  cgGLEnableProfile (cgp_vertex);
  cgGLSetOptimalOptions (cgp_vertex);// Set The Current Profile
  //Now set up our list of shaders
  for (i = 0; i < VSHADER_COUNT; i++) {
    s = &vshader_list[i];
    // Load And Compile The Vertex Shader From File
    s->program = cgCreateProgramFromFile (cgContext, CG_SOURCE, VSHADER_FILE, cgp_vertex, vshader_function[i], 0);
    checkForCgError (cgGetError(), vshader_function[i], "Compiling");
    // Load The Program
	  cgGLLoadProgram (s->program);
    cgGLBindProgram (s->program);
    // Get Handles To Each Of Our Parameters So That
	  // We Can Change Them At Will Within Our Code
	  s->position   = cgGetNamedParameter(s->program, "IN.position");
    s->offset     = cgGetNamedParameter(s->program, "offset");
	  s->lightpos   = cgGetNamedParameter(s->program, "lightpos");
    s->eyepos     = cgGetNamedParameter(s->program, "eyepos");
	  s->lightcol	  = cgGetNamedParameter(s->program, "lightcol");
    s->ambientcol	= cgGetNamedParameter(s->program, "ambientcol");
    s->fog        = cgGetNamedParameter(s->program, "fogdist");
    s->data       = cgGetNamedParameter(s->program, "data");
	  s->matrix	    = cgGetNamedParameter(s->program, "ModelViewProj");
  }

  cgGLDisableProfile (cgp_fragment);
  cgGLDisableProfile (cgp_vertex);
}

void CgInit ()
{
  CgCompile ();
}

void CgShaderSelect (int select)
{
  if (select < FSHADER_NONE) {
    vshader_select (select);
    return;
  }
  fshader_select (select);
}

void CgUpdate ()
{
  float     elapsed;

  elapsed = SdlElapsed ();
  wind += elapsed * 0.001f;
  if (!CVarUtils::GetCVar<bool> ("render.textured"))
    cgGLSetManageTextureParameters (cgContext,  CG_TRUE);
  else
    cgGLSetManageTextureParameters (cgContext,  CG_FALSE);
}

void CgUpdateMatrix ()
{
  //cgGLSetStateMatrixParameter(modelViewMatrix, CG_MatrixMode.Modelview_PROJECTION_MATRIX, CG_MatrixMode.Modelview_MATRIX);
}

void CgSetOffset (GLvector p)
{
  VShader*    s;

  if (vshader_selected == VSHADER_NONE)
    return;
  s = &vshader_list[vshader_selected];
  cgGLSetParameter3f (s->offset, p.x, p.y, p.z);
}
*/