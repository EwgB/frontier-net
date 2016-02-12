/*-----------------------------------------------------------------------------
  glVector3.cpp
  2006 Shamus Young
-------------------------------------------------------------------------------
  Functions for dealing with 3d vectors.
-----------------------------------------------------------------------------*/

/*
#include "stdafx.h"

#include <float.h>
#include <math.h>

#include "math.h"

struct GLvector
{
  float       x;
  float       y;
  float       z;
  void        Normalize ();
  float       Length ();
  OPERATORS(GLvector);
};

std::ostream &operator<<(std::ostream &stream, GLvector &point);
std::istream &operator>>(std::istream &stream, GLvector &point);

typedef GLvector       GLvector3;

GLvector  glVector (float x, float y, float z);
GLvector  glVectorCrossProduct (GLvector v1, GLvector v2);
float     glVectorDotProduct (GLvector v1, GLvector v2);
void      glVectorGl (GLvector v);
GLvector  glVectorInterpolate (GLvector v1, GLvector v2, float scalar);
float     glVectorLength (GLvector v);
GLvector  glVectorNormalize (GLvector v);
GLvector  glVectorReflect (GLvector3 ray, GLvector3 normal);

GLvector glVectorReflect (GLvector3 ray, GLvector3 normal)
{
  float       dot;

  dot = glVectorDotProduct (ray, normal);
  return ray - (normal * (2.0f * dot));
}

GLvector3 glVector (float x, float y, float z)
{
  GLvector3 result;

  result.x = x;
  result.y = y;
  result.z = z;
  return result;
}

GLvector3 glVectorInterpolate (GLvector3 v1, GLvector3 v2, float scalar)
{
  GLvector3 result;

  result.x = MathInterpolate (v1.x, v2.x, scalar);
  result.y = MathInterpolate (v1.y, v2.y, scalar);
  result.z = MathInterpolate (v1.z, v2.z, scalar);
  return result;
}  

float glVectorLength (GLvector3 v)
{
  return (float)sqrt (v.x * v.x + v.y * v.y + v.z * v.z);
}

float glVectorDotProduct (GLvector3 v1, GLvector3 v2)
{
  return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
}

GLvector3 glVectorCrossProduct (GLvector3 v1, GLvector3 v2)
{
  GLvector3 result;
  
  result.x = v1.y * v2.z - v2.y * v1.z;
  result.y = v1.z * v2.x - v2.z * v1.x;
  result.z = v1.x * v2.y - v2.x * v1.y;
  return result;
}

GLvector3 glVectorInvert (GLvector3 v)
{
  v.x *= -v.x;
  v.y *= -v.y;
  v.z *= -v.z;
  return v;
}

GLvector3 glVectorScale (GLvector3 v, float scale)
{
  v.x *= scale;
  v.y *= scale;
  v.z *= scale;
  return v;
}

GLvector3 glVectorNormalize (GLvector3 v)
{
  float length;

  length = glVectorLength (v);
  if (length < 0.000001f)
    return v;
  return glVectorScale (v, 1.0f / length);
}

GLvector GLvector::operator+ (const GLvector& c)
{
  return glVector (x + c.x, y + c.y, z + c.z);
}

GLvector GLvector::operator+ (const float& c)
{
  return glVector (x + c, y + c, z + c);
}

void GLvector::operator+= (const GLvector& c)
{
  x += c.x;
  y += c.y;
  z += c.z;
}

void GLvector::operator+= (const float& c)
{
  x += c;
  y += c;
  z += c;
}

GLvector GLvector::operator- (const GLvector& c)
{
  return glVector (x - c.x, y - c.y, z - c.z);
}

GLvector GLvector::operator- (const float& c)
{
  return glVector (x - c, y - c, z - c);
}

void GLvector::operator-= (const GLvector& c)
{
  x -= c.x;
  y -= c.y;
  z -= c.z;
}

void GLvector::operator-= (const float& c)
{
  x -= c;
  y -= c;
  z -= c;
}

GLvector GLvector::operator* (const GLvector& c)
{
  return glVector (x * c.x, y * c.y, z * c.z);
}

GLvector GLvector::operator* (const float& c)
{
  return glVector (x * c, y * c, z * c);
}

void GLvector::operator*= (const GLvector& c)
{
  x *= c.x;
  y *= c.y;
  z *= c.z;
}

void GLvector::operator*= (const float& c)
{
  x *= c;
  y *= c;
  z *= c;
}

GLvector GLvector::operator/ (const GLvector& c)
{
  return glVector (x / c.x, y / c.y, z / c.z);
}

GLvector GLvector::operator/ (const float& c)
{
  return glVector (x / c, y / c, z / c);
}

void GLvector::operator/= (const GLvector& c)
{
  x /= c.x;
  y /= c.y;
  z /= c.z;
}

void GLvector::operator/= (const float& c)
{
  x /= c;
  y /= c;
  z /= c;
}

bool GLvector::operator== (const GLvector& c)
{
  if (x == c.x && y == c.y && z == c.z)
    return true;
  return false;
}

float GLvector::Length ()
{
  return sqrt (x * x + y * y + z * z);
}

void GLvector::Normalize ()
{
  float norm;
  float len;

  len = Length ();
  if (len < 0.000001f)
    return;
  norm = 1.0f / len;
  x *= norm;
  y *= norm;
  z *= norm;
}

std::ostream &operator<<(std::ostream &stream, GLvector &v)
{
  stream << "[ " << v.x << ",  " << v.y << ",  " << v.z << " ]";
  return stream;
}
*/

/**
 * Overloaded stream in for Point3D. Converts it from a string
 */
/*
std::istream &operator>>(std::istream &stream, GLvector &v)
{
    char str[NAME_MAX] = {0};
    stream.readsome( str, NAME_MAX );
    sscanf( str, "[ %f, %f, %f ]", &v.x, &v.y, &v.z );

    return stream;
}
*/