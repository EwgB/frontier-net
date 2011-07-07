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

static char*          shader_function[] =
{
  "standard",
  "trees",
  "grass",
};

struct Shader
{
  char        file[MAX_FILE_NAME];
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
static CGprofile	    cgVertexProfile;	// The Profile To Use For Our Vertex Shader
static Shader         shader_list[SHADER_COUNT];
static float          wind;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CgCompile ()
{

  Shader*     s;
  unsigned    i;

  //Setup Cg
  cgContext = cgCreateContext();				
  cgVertexProfile = cgGLGetLatestProfile(CG_GL_VERTEX);			
  if (cgVertexProfile == CG_PROFILE_UNKNOWN) {
	  ConsoleLog ("CgCompile: Invalid profile type creating vertex profile.");
    return;
  }
  cgGLSetOptimalOptions(cgVertexProfile);// Set The Current Profile
  //Now set up our list of shaders
  for (i = 0; i < SHADER_COUNT; i++) {
    s = &shader_list[i];
    // Load And Compile The Vertex Shader From File
    s->program = cgCreateProgramFromFile (cgContext, CG_SOURCE, VSHADER_FILE, cgVertexProfile, shader_function[i], 0);
    if (!s->program) {
      CGerror Error = cgGetError();
      ConsoleLog ("CgCompile: ERROR: %s", cgGetErrorString(Error));
      continue;
    }
    ConsoleLog ("CgCompile: Loaded %s", s->file);
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

  Shader*       s;
  GLvector      p;
  Env*          e;
  GLrgba        c;
  float         val1, val2;

  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  if (select == SHADER_NONE) {
    cgGLDisableProfile(cgVertexProfile);
    return;
  }
  val1 = val2 = 0.0f;
  if (select == SHADER_TREES || select == SHADER_GRASS) 
    val1 = wind;
  s = &shader_list[select];
  e = EnvGet ();
  cgGLEnableProfile (cgVertexProfile);
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
