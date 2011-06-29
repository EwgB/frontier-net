/*-----------------------------------------------------------------------------

  Log.cpp


-------------------------------------------------------------------------------

  Maintain daily log files.

-----------------------------------------------------------------------------*/

#include "stdafx.h"

#define max_MSG_LEN     (1024 * 10)  // 10k
#define NAME_INTERVAL   (60 * 10) // Ten mins

#include <windows.h>
#include <stdio.h>
#include <stdarg.h>
#include <string.h>
#include <time.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/timeb.h>
#include <tchar.h>

//#include "ini.h"
//#include "console.h"
#include "log.h"

static char   prefix[256];
static char   filename[256];

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

static void do_name ()
{

  tm*     current;
  time_t  now;
  char    path[256];

  time (&now);  
  current = gmtime (&now);
  strcpy (path, "");
  sprintf (filename, "%s\\%s-%02d-%02d-%02d.log",
    path,
    prefix,
    current->tm_year - 100,
    current->tm_mon + 1,
    current->tm_mday
    );


  
}

/*-----------------------------------------------------------------------------
                             l o g
-----------------------------------------------------------------------------*/

void LogInit (char* log_file_name)
{

  char*     c;
  FILE*     logfile;

  strcpy (prefix, log_file_name);
  strcpy (filename, log_file_name);
  if (c = strchr (prefix, '.'))
    c[0] = 0;
  logfile = fopen (filename, "w+b");
  if (!logfile) 
    return;
  fprintf (logfile, "\n\nBEGIN LOGGING\r\n");
  fclose (logfile);

}

/*-----------------------------------------------------------------------------
                             l o g
-----------------------------------------------------------------------------*/

void LogTerm (void)
{

  LogFile ("*");
  LogFile ("* End Logging");
  LogFile ("*");
  LogFile ("\n\n\n\n\n\n");

}

void LogFile (char *message, ...)
{

  static char    msg_text[max_MSG_LEN];
  char           time_text[65];
  char*          cr;
  va_list           marker;
  time_t            now;
  FILE*             logfile;

  va_start (marker, message);
  vsprintf (msg_text, message, marker);
  va_end (marker);
  time (&now);
  logfile = fopen (filename, "a+b");
  if (!logfile) 
    return;
  strcpy (time_text, ctime (&now));
  while (cr = strchr (time_text, 10))
    cr[0] = 0;
  fprintf (logfile, "%s: %s\r\n", time_text, msg_text);
  fclose (logfile);

}

void Log (char* message, ...)
{

  static char    msg_text[max_MSG_LEN];
  va_list           marker;

  va_start (marker, message);
  vsprintf (msg_text, message, marker);
  va_end (marker);
  //Console (msg_text);
  LogFile (msg_text);

}

