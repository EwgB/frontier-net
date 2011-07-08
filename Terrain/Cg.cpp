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
#include "texture.h"
#include <cg\cg.h>									
#include <cg\cggl.h>

#define VSHADER_FILE  "shaders/vertex.cg"
#define FSHADER_FILE  "shaders/fragment.cg"
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

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void checkForCgError(CGerror error, const char* program, const char *situation)
{
  if (error != CG_NO_ERROR) 
    ConsoleLog ("%s: %s: %s", program, situation, cgGetErrorString(error));
  else
    ConsoleLog ("%s: %s... ok.", program, situation);

}

static void fshader_select (int select)
{
  
  FShader*      s;
  Env*          e;

  fshader_selected = select;
  if (select == -1) {
    cgGLDisableProfile (cgp_fragment);
    return;
  }
  if (!CVarUtils::GetCVar<bool> ("render.shaders"))
    return;
  s = &fshader_list[select];
  e = EnvGet ();
  cgGLEnableProfile (cgp_fragment);
  cgGLBindProgram (s->program);
  cgGLSetParameter3f (s->fogcolor, e->color[ENV_COLOR_FOG].red, e->color[ENV_COLOR_FOG].green, e->color[ENV_COLOR_FOG].blue);
  //cgGLSetParameter3f (s->fogcolor, 1,0,1);
  cgGLSetTextureParameter (s->texture, TextureIdFromName ("fade.png"));
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
  cgGLSetParameter2f (s->fog, e->fog_min, e->fog_max);
  cgGLSetParameter4f (s->data, SceneVisibleRange (), SceneVisibleRange () * 0.05, val1, val2);
  glColor3f (1,1,1);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

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
    fs->texture = cgGetNamedParameter (fs->program, "texture2");
    fs->fogcolor = cgGetNamedParameter (fs->program, "fogcolor");
    checkForCgError (cgGetError(), fshader_function[i], "Loading variables");
  }
  

  cgGLSetManageTextureParameters (cgContext,  CG_TRUE);










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
  fshader_select (select - FSHADER_BASE);


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

void CgSetOffset (GLvector p)
{

  VShader*    s;


  if (vshader_selected == VSHADER_NONE)
    return;
  s = &vshader_list[vshader_selected];
  cgGLSetParameter3f (s->offset, p.x, p.y, p.z);

}