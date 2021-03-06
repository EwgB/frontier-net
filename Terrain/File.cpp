/*-----------------------------------------------------------------------------

  File.cpp

-------------------------------------------------------------------------------

  Various useful file i/o functions.

-----------------------------------------------------------------------------*/

#include "StdAfx.h"
#include <windows.h>
#include <direct.h>
#include <stdio.h>
#include <io.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <stdlib.h>
#include <string.h>
#include <sys/utime.h>

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

long FileModified (char *filename)
{

  long                search;
  struct _finddata_t  info;
  DWORD               timestamp;

  timestamp = 0;
  if ((search = _findfirst (filename, &info)) != -1) {
    timestamp = info.time_write;
    _findclose (search);
  } 
  return timestamp;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

bool FileDelete (char* name)
{

  if (!_unlink (name))
    return true;
  return false;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

bool FileSave (char *name, char *buf, int size)
{

  int fd;

  if ((fd = _open (name, O_WRONLY | O_BINARY | O_CREAT | O_TRUNC, S_IREAD | S_IWRITE)) == -1)
    return false;
  if (_write (fd, buf, size) != size) {
    _close (fd);
    _unlink (name);
    return false;
  }
  _close (fd);
  return true;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

char* FileLoad (char* name, long* size)
{
  FILE*     f;
  char*     buffer;
  int       h;
  int       len;

  buffer = NULL;
  len = 0;
  h = _open (name, _O_RDONLY | O_BINARY);
  if (h != -1) {
    //set file size
    len = _filelength (h) + 1;
    _close (h);
    buffer = (char*)malloc (len);
    f = fopen (name, "rb");
    fread (buffer, 1, len, f);
    fclose (f);
    //terminate string
    buffer[len - 1] = 0;
  }
  if (size)
    *size = len;
  return buffer;

}


char* FileBinaryLoad (char* name, long* size)
{
  FILE*     f;
  char*     buffer;
  int       h;
  int       len;

  buffer = NULL;
  len = 0;
  h = _open (name, _O_RDONLY | O_BINARY);
  if (h != -1) {
    //set file size
    len = _filelength (h);
    _close (h);
    buffer = (char*)malloc (len);
    f = fopen (name, "rb");
    fread (buffer, 1, len, f);
    fclose (f);
  }
  if (size)
    *size = len;
  return buffer;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

bool FileExists (const char *name)
{


  FILE*     f;

  f = fopen (name, "rb");
  if (f == NULL)
    return false;
  fclose (f);
  return true;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void FileTouch (char *filename)
{

  _utime (filename, NULL);

}

/*-----------------------------------------------------------------------------
                       f i l e  c r e a t e  f o l d e r
-----------------------------------------------------------------------------*/

void FileMakeDirectory (char* folder)
{

  char*   dir;
  char*   p;
  char*   p1;
  int     errcode;

  dir = (char*)malloc (strlen (folder) + 1);
  strcpy (dir, folder);
  if (!(p = strchr (dir, '\\')))
    p = strchr (dir, '/');
  p1 = p;
  while (p && p1) {
    *p = '\0';
    _mkdir (dir);
    *p = '\\';
    p1++;
    if (p == NULL || p1 == NULL ||  *p == 0x0 || *p1 == 0x0)
      break;
    if (!(p = strchr (p1, '\\')))    
      if (!(p = strchr (p1, '/')))
        break;
    p1 = p;
  }

  errcode = 0;
  if (_mkdir (folder))
    errcode = errno;
  free (dir);

}