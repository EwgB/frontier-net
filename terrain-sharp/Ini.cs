/*-----------------------------------------------------------------------------
  Ini.cpp
  2009 Shamus Young
-------------------------------------------------------------------------------
  This takes various types of data and dumps them into a predefined ini file.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"
#include <stdio.h>
#include "ini.h"
#include "main.h"

int       IniInt (char* entry);
void      IniIntSet (char* entry, int val);
float     IniFloat (char* entry);
void      IniFloatSet (char* entry, float val);
char*     IniString (char* entry);
void      IniStringSet (char* entry, char* val);
void      IniVectorSet (char* entry, GLvector v);
GLvector  IniVector (char* entry);

int       IniInt (char* section, char* entry);
void      IniIntSet (char* section, char* entry, int val);
float     IniFloat (char* section, char* entry);
void      IniFloatSet (char* section, char* entry, float val);
char*     IniString (char* section, char* entry);
void      IniStringSet (char* section, char* entry, char* val);
void      IniVectorSet (char* section, char* entry, GLvector v);
GLvector  IniVector (char* section, char* entry);

#define FORMAT_VECTOR       "%f %f %f"
#define MAX_RESULT          256
#define FORMAT_FLOAT        "%1.2f"
#define INI_FILE            ".\\" APP ".ini"
#define DEFAULT_SECTION     "Settings"

static char                 result[MAX_RESULT];

// Integers

int IniInt (char* section, char* entry)
{
  int         result;

  result = GetPrivateProfileIntA (section, entry, 0, INI_FILE);
  return result;
}

int IniInt (char* entry)
{
  return IniInt (DEFAULT_SECTION, entry);
}

void IniIntSet (char* section, char* entry, int val)
{
  char        buf[20];

  sprintf (buf, "%d", val);
  WritePrivateProfileStringA (section, entry, buf, INI_FILE);
}

void IniIntSet (char* entry, int val)
{
  IniIntSet (DEFAULT_SECTION, entry, val);
}

// Floats

float IniFloat (char* section, char* entry)
{
  float     f;

  GetPrivateProfileStringA (section, entry, "", result, MAX_RESULT, INI_FILE);
  f = (float)atof (result);
  return f;
}

float IniFloat (char* entry)
{
  return IniFloat (DEFAULT_SECTION, entry);
}

void IniFloatSet (char* section, char* entry, float val)
{
  char        buf[20];
  
  sprintf (buf, FORMAT_FLOAT, val);
  WritePrivateProfileStringA (section, entry, buf, INI_FILE);
}

void IniFloatSet (char* entry, float val)
{
  IniFloatSet (DEFAULT_SECTION, entry, val);
}

// Strings

char* IniString (char* section, char* entry)
{
  GetPrivateProfileStringA (section, entry, "", result, MAX_RESULT, INI_FILE);
  return result;
}

char* IniString (char* entry)
{
  return IniString (DEFAULT_SECTION, entry);
}

void IniStringSet (char* section, char* entry, char* val)
{
  WritePrivateProfileStringA (section, entry, val, INI_FILE);
}

void IniStringSet (char* entry, char* val)
{
  IniStringSet (DEFAULT_SECTION, entry, val);
}

// Vectors

void IniVectorSet (char* section, char* entry, GLvector v)
{
  sprintf (result, FORMAT_VECTOR, v.x, v.y, v.z);
  WritePrivateProfileStringA (section, entry, result, INI_FILE);
}

void IniVectorSet (char* entry, GLvector v)
{
  IniVectorSet (DEFAULT_SECTION, entry, v);
}

GLvector IniVector (char* section, char* entry)
{
  GLvector  v;

  v.x = v.y = v.z = 0.0f;
  GetPrivateProfileStringA (section, entry, "0 0 0", result, MAX_RESULT, INI_FILE);
  sscanf (result, FORMAT_VECTOR, &v.x, &v.y, &v.z);
  return v;
}

GLvector IniVector (char* entry)
{
  return IniVector (DEFAULT_SECTION, entry);
}
*/