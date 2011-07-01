/*-----------------------------------------------------------------------------

  File.cpp

                     Copyright 2001-2007 Activeworlds Inc.
          Licensed Material -- Program Property of Activeworlds Inc.

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

#define max_BUF_SIZE            8192
#define max_FILENAME_LEN        256

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

int FileExists (char *name)
{
/*
  if (GetFileAttributes (name) == -1) {
	  return 0;
  }
  */
  return 1;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void FileTouch (char *filename)
{

  _utime (filename, NULL);

}


/*-----------------------------------------------------------------------------
                      f i l e   i s   d i r e c t o r y
-----------------------------------------------------------------------------*/

bool FileIsDirectory (char* name)
{

  DWORD attributes;

  attributes = GetFileAttributes (name);
  if (attributes != 0xffffffff && (attributes & FILE_ATTRIBUTE_DIRECTORY)) {
    return true;
  }
  return false;

}

/*-----------------------------------------------------------------------------
                       f i l e  c r e a t e  f o l d e r
-----------------------------------------------------------------------------*/

void FileCreateFolder (char* folder)
{

  char  dir[max_FILENAME_LEN];
  char* p;
  char* p1;
  int errcode;

  if (!folder)
    return;
  if (FileIsDirectory (folder))
    return;
  if (strlen (folder) >= sizeof (dir))
    return;
  strcpy (dir, folder);
  if (!(p = strchr (dir, '\\')))
    p = strchr (dir, '/');
  p1 = p;
  while (p && p1) {
    *p = '\0';
    _mkdir (dir);
    *p = '\\';
    p1++;
    if (p == NULL || p1 == NULL
    ||  *p == 0x0 || *p1 == 0x0)
      break;
    if (!(p = strchr (p1, '\\')))
      if (!(p = strchr (p1, '/')))
        break;
    p1 = p;
  }

  errcode = 0;
  _mkdir (folder);
  /*
  if (mkdir (folder))
    errcode = errno;
  if (!errcode) {
    SetFileAttributes (folder, FILE_ATTRIBUTE_NOT_CONTENT_INDEXED);
    SetFileAttributes (folder, FILE_ATTRIBUTE_TEMPORARY);
  }
  */

}


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