/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the terrain object class.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "console.h"
#include "cg.h"
#include "env.h"
#include "ini.h"
#include "sdl.h"
#include <cg\cg.h>									
#include <cg\cggl.h>

#define SHADER_FILE   "shaders/standard.cg"
#define MAX_FILE_NAME 100

static char*          shader_entry[] =
{
  "ShaderNormal",
  "ShaderTrees",
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


CGcontext	cgContext;								// A Context To Hold Our Cg Program(s)
CGprogram	cgProgram;								// Our Cg Vertex Program
CGprofile	cgVertexProfile;							// The Profile To Use For Our Vertex Shader
CGparameter	position, color, modelViewMatrix, wave, lightpos, lightcol, ambientcol, eyepos;					// The Parameters Needed For Our Shader

static Shader         shader_list[SHADER_COUNT];
static float          wind;

void MyErrorCallback (void)
{
 
  const char* errorString = cgGetErrorString (cgGetError());
 
  //Log ("Cg error: %s", errorString);
 
}

void CgCompile ()
{

  Shader*   s;

  // Setup Cg
  cgContext = cgCreateContext();							// Create A New Context For Our Cg Program(s)
  cgVertexProfile = cgGLGetLatestProfile(CG_GL_VERTEX);				// Get The Latest GL Vertex Profile
  cgSetErrorCallback(MyErrorCallback);
  // Validate Our Profile Determination Was Successful
  if (cgVertexProfile == CG_PROFILE_UNKNOWN) {
	  ConsoleLog ("CgCompile: Invalid profile type creating vertex profile.");
    return;
  }
  cgGLSetOptimalOptions(cgVertexProfile);						// Set The Current Profile
  //Now set up our list of shaders
  unsigned      i;

  for (i = 0; i < SHADER_COUNT; i++) {
    s = &shader_list[i];
    // Load And Compile The Vertex Shader From File
    s->program = cgCreateProgramFromFile (cgContext, CG_SOURCE, s->file, cgVertexProfile, "main", 0);
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

  unsigned      i;
  char          name[MAX_FILE_NAME];

  for (i = 0; i < SHADER_COUNT; i++) {
    strcpy (name, IniString ("Shaders", shader_entry[i]));
    IniStringSet ("Shaders", shader_entry[i], name);
    sprintf (shader_list[i].file, "shaders//%s", name);
  }
  CgCompile ();
  // Load And Compile The Vertex Shader From File
  cgProgram = cgCreateProgramFromFile (cgContext, CG_SOURCE, SHADER_FILE, cgVertexProfile, "main", 0);

  if (cgProgram == NULL) {
    CGerror Error = cgGetError();
    // Show A Message Box Explaining What Went Wrong
    ConsoleLog ("CgInit: ERROR: %s", cgGetErrorString(Error));
    return;
  }
  ConsoleLog ("CgInit: Loaded %s", SHADER_FILE);
  // Load The Program
	cgGLLoadProgram(cgProgram);
  cgGLBindProgram(cgProgram);
  // Get Handles To Each Of Our Parameters So That
	// We Can Change Them At Will Within Our Code
	position	= cgGetNamedParameter(cgProgram, "IN.position");
	color		= cgGetNamedParameter(cgProgram, "IN.color");
	lightpos		= cgGetNamedParameter(cgProgram, "lightpos");
  eyepos		= cgGetNamedParameter(cgProgram, "eyepos");
	lightcol		= cgGetNamedParameter(cgProgram, "lightcol");
  ambientcol		= cgGetNamedParameter(cgProgram, "ambientcol");
	modelViewMatrix	= cgGetNamedParameter(cgProgram, "ModelViewProj");


}

static float wwww;

void CgShaderSelect (int select)
{

  Shader*       s;
  GLvector      p;
  Env*          e;
  GLrgba        c;

  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  if (select == SHADER_NONE) {
    cgGLDisableProfile(cgVertexProfile);
    return;
  }
  s = &shader_list[select];
  e = EnvGet ();
  cgGLEnableProfile (cgVertexProfile);
  cgGLBindProgram (s->program);
  cgGLSetParameter3f (s->lightpos, -e->light.x, -e->light.y, -e->light.z);
  c = e->color[ENV_COLOR_LIGHT];
  cgGLSetParameter3f (s->lightcol, c.red, c.green, c.blue);
  c = e->color[ENV_COLOR_AMBIENT] * glRgba (0.2f, 0.2f, 1.0f);
  cgGLSetParameter3f (s->ambientcol, c.red, c.green, c.blue);
  p = CameraPosition ();
  cgGLSetParameter3f (s->eyepos, p.x, p.y, p.z);
  cgGLSetStateMatrixParameter(s->matrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MODELVIEW_MATRIX);

}


void CgUpdate ()
{

  float     elapsed;


  elapsed = SdlElapsed ();
  wind += elapsed * 0.001f;
  cgGLSetParameter3f (shader_list[SHADER_TREES].data, wind, 0.0f, 0.0f);

  Env*            e;

  e = EnvGet ();

  wwww += 0.03f;

  // Set The Modelview Matrix Of Our Shader To Our OpenGL Modelview Matrix
  //cgGLSetStateMatrixParameter(modelViewMatrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MATRIX_IDENTITY);
  cgGLSetStateMatrixParameter(modelViewMatrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MODELVIEW_MATRIX);
  

  cgGLEnableProfile(cgVertexProfile);					// Enable Our Vertex Shader Profile
  cgGLBindProgram(cgProgram);

  // Bind Our Vertex Program To The Current State
  
  // Set The Drawing Color To Light Green (Can Be Changed By Shader, Etc...)

  cgGLSetParameter3f (lightpos, -e->light.x, -e->light.y, -e->light.z);
  GLrgba    c;
  c = e->color[ENV_COLOR_LIGHT];
  cgGLSetParameter3f (lightcol, c.red, c.green, c.blue);
  //c = glRgba (1.0f, 1.0f, 0.0f);
  c = e->color[ENV_COLOR_AMBIENT] * glRgba (0.2f, 0.2f, 1.0f);
  //c = glRgba (0.0f, 0.0f, 1.0f);
  cgGLSetParameter3f (ambientcol, c.red, c.green, c.blue);
  GLvector    p;
  p = CameraPosition ();
  cgGLSetParameter3f (eyepos, p.x, p.y, p.z);

}

void CgUpdateMatrix ()
{

  cgGLSetStateMatrixParameter(modelViewMatrix, CG_GL_MODELVIEW_PROJECTION_MATRIX, CG_GL_MODELVIEW_MATRIX);

}
/*
void CgOff ()
{
  cgGLDisableProfile(cgVertexProfile);					// Enable Our Vertex Shader Profile

}*/