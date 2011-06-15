/*-----------------------------------------------------------------------------

  Camera.cpp

  2009 Shamus Young

-------------------------------------------------------------------------------


-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include "ini.h"
#include "Region.h"

static GLvector     angle;
static GLvector     position;
static Region       region;

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector CameraPosition (void)		
{
 
  return position;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraReset ()		
{

  position.y = 20.0f;
  position.x = 20;
  position.z = 10;
  angle.x = 0.0f;
  angle.y = 0.0f;
  angle.z = 0.0f;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraPositionSet (GLvector new_pos)		
{

  new_pos.z = CLAMP (new_pos.z, -25, 1024);
  new_pos.x = CLAMP (new_pos.x, -512, (REGION_SIZE * REGION_GRID));
  new_pos.y = CLAMP (new_pos.y, -512, (REGION_SIZE * REGION_GRID));
  position = new_pos;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraMove (GLvector delta)		
{

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLvector CameraAngle (void)		
{

  return angle;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraAngleSet (GLvector new_angle)		
{

  angle = new_angle;
  angle.x = CLAMP (angle.x, 5.0f, 175.0f);
  angle.z = fmod (angle.z, 360.0f);
  if (angle.z < 0.0f)
    angle.z += 360.0f;

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void* CameraRegion ()
{

  return (void*)&region;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraInit (void)		
{

  angle = IniVector ("CameraAngle");
  position = IniVector ("CameraPosition");

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraUpdate (void)		
{

  region = RegionGet (position.x, position.y);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void CameraTerm (void)		
{

  //just store our most recent position in the ini
  IniVectorSet ("CameraAngle", angle);
  IniVectorSet ("CameraPosition", position);
 
}
