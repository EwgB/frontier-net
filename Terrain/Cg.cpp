/*-----------------------------------------------------------------------------

  CTerrain.cpp

-------------------------------------------------------------------------------

  This holds the terrain object class.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "camera.h"
#include "env.h"
#include "log.h"

#include <cg\cg.h>									
#include <cg\cggl.h>

CGcontext	cgContext;								// A Context To Hold Our Cg Program(s)
CGprogram	cgProgram;								// Our Cg Vertex Program
CGprofile	cgVertexProfile;							// The Profile To Use For Our Vertex Shader
CGparameter	position, color, modelViewMatrix, wave, lightpos, lightcol, ambientcol, eyepos;					// The Parameters Needed For Our Shader

void MyErrorCallback (void)
{
 
  const char* errorString = cgGetErrorString (cgGetError());
 
  //Log ("Cg error: %s", errorString);
 
}

void CgInit ()
{

  // Setup Cg
  cgContext = cgCreateContext();							// Create A New Context For Our Cg Program(s)
  cgVertexProfile = cgGLGetLatestProfile(CG_GL_VERTEX);				// Get The Latest GL Vertex Profile
  cgSetErrorCallback(MyErrorCallback);
  // Validate Our Profile Determination Was Successful
  if (cgVertexProfile == CG_PROFILE_UNKNOWN) {
	  Log ("CgInit: Invalid profile type creating vertex profile.");
    return;
  }
  cgGLSetOptimalOptions(cgVertexProfile);						// Set The Current Profile

  // Load And Compile The Vertex Shader From File
  cgProgram = cgCreateProgramFromFile (cgContext, CG_SOURCE, "shaders/test.cg", cgVertexProfile, "main", 0);

  if (cgProgram == NULL) {
    CGerror Error = cgGetError();
    // Show A Message Box Explaining What Went Wrong
    Log ("CgInit: ERROR: %s", cgGetErrorString(Error));
    Log ("CgInit: ERROR: %s", cgGetLastErrorString(&Error));
    return;
  }
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

void CgUpdate ()
{

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

void CgOff ()
{
  cgGLDisableProfile(cgVertexProfile);					// Enable Our Vertex Shader Profile

}