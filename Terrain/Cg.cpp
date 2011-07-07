/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the terrain object class.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "avatar.h"
#include "console.h"
#include "cg.h"
#include "env.h"
#include "ini.h"
#include "scene.h"
#include "sdl.h"
#include <cg\cg.h>									
#include <cg\cggl.h>

#define SHADER_FILE   "shaders/standard.cg"
#define VSHADER_FILE  "shaders/vertex.cg"
#define MAX_FILE_NAME 100

static char*          vshader_function[] =
{
  "standard",
  "trees",
  "grass",
};

static char*          fshader_function[] =
{
  "green",
};

struct VShader
{
  CGprogram	  program;
  CGprofile	  profile;
  CGparameter	position;
  CGparameter	matrix;
  CGparameter	lightpos;
  CGparameter	lightcol;
  CGparameter	ambientcol;
  CGparameter	eyepos;				
  CGparameter	data;
};


static CGcontext	    cgContext;				// A Context To Hold Our Cg Program(s)
static CGprogram	    cgProgram;				// Our Cg Vertex Program
static CGprofile	    cgp_vertex;	
static CGprofile	    cgp_fragment;	
static VShader        vshader_list[VSHADER_COUNT];
static float          wind;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void checkForCgError(CGerror error, const char* program, const char *situation)
{
  if (error != CG_NO_ERROR) 
    ConsoleLog ("%s: %s: %s", program, situation, cgGetErrorString(error));
  else
    ConsoleLog ("%s: %s... ok.", program, situation);

}

static void vshader_select (int select)
{
  
  VShader*      s;
  GLvector      p;
  Env*          e;
  GLrgba        c;
  float         val1, val2;

  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  if (select == VSHADER_NONE) {
    cgGLDisableProfile (cgp_vertex);
    return;
  }
  val1 = val2 = 0.0f;
  if (select == VSHADER_TREES || select == VSHADER_GRASS) 
    val1 = wind;
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
  cgGLSetStateMatrixParameter(s->matrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MODELVIEW_MATRIX);
  cgGLSetParameter4f (s->data, SceneVisibleRange (), SceneVisibleRange () * 0.05, val1, val2);
  glColor3f (1,1,1);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CgCompile ()
{

  VShader*    s;
  unsigned    i;

  //Setup Cg
  cgContext = cgCreateContext();				
  checkForCgError (cgGetError(), "Init", "Establishing Cg context");
  cgp_fragment = cgGLGetLatestProfile (CG_GL_FRAGMENT);
  if (cgp_fragment == CG_PROFILE_UNKNOWN) {
	  ConsoleLog ("CgCompile: Invalid profile type creating fragment profile.");
    return;
  }
  checkForCgError (cgGetError(), "Init", "Establishing Cg Fragment profile");
  cgGLSetOptimalOptions (cgp_fragment);

  













  cgp_vertex = cgGLGetLatestProfile (CG_GL_VERTEX);			
  checkForCgError (cgGetError(), "Init", "Establishing Cg Vertex profile");
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
	  s->lightpos   = cgGetNamedParameter(s->program, "lightpos");
    s->eyepos     = cgGetNamedParameter(s->program, "eyepos");
	  s->lightcol	  = cgGetNamedParameter(s->program, "lightcol");
    s->ambientcol	= cgGetNamedParameter(s->program, "ambientcol");
    s->data       = cgGetNamedParameter(s->program, "data");
	  s->matrix	    = cgGetNamedParameter(s->program, "ModelViewProj");
  }

}

void CgInit ()
{

  CgCompile ();

}

void CgShaderSelect (int select)
{

  if (select < FSHADER_BASE) {
    vshader_select (select);
    return;
  }

}


void CgUpdate ()
{

  float     elapsed;


  elapsed = SdlElapsed ();
  wind += elapsed * 0.001f;
 
}

void CgUpdateMatrix ()
{

  //cgGLSetStateMatrixParameter(modelViewMatrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MODELVIEW_MATRIX);

}
