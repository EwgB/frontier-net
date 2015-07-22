/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: Timestamp.cpp 162 2010-02-15 19:11:05Z gsibley $
 */

# include <time.h>
# include <iostream>
# include <stdio.h>
# include <CVars/config.h>

namespace CVars {

	class TimeStamp {
		private int Start { get; set; }

		private double PrevTime { get; set; }
		private double StartTime { get; set; }
		private double PauseTime { get; set; }
		private bool IsPaused { get; set; }

		private double Overflow { get; set; }

		public TimeStamp() {
			Start = 1;
			Overflow = 0;
			IsPaused = false;
		}

		public void Stamp() {
			if (IsPaused)
				UnPause();

			long time, freq;
			QueryPerformanceCounter(&time);
			QueryPerformanceFrequency(&freq);
			prevTime = (double) time.QuadPart / (double) freq.QuadPart;

			//overflow = 0;
			if (start == 1) {
				start = 0;
				startTime = prevTime;
			}
		}
	}
}

#ifdef _WIN_
#endif

#ifndef _WIN_
double TotalElapsed()
{
	if(start == 1)
	{
	   start = 0;
	   return 0; 
	}
	
	//get current time
	struct timeval currTime;
	struct timezone currTz;
	gettimeofday(&currTime, &currTz);
	
	double t1 = (double)startTime.tv_sec + (double) startTime.tv_usec/(1000*1000);
	double t2 =  (double)currTime.tv_sec + (double)currTime.tv_usec/(1000*1000);
	return t2-t1;
}
#endif

#ifdef _WIN_
double TotalElapsed()
{
	if(start == 1)
	{
	   start = 0;
	   return 0; 
	}
	
	//get current time
	LARGE_INTEGER time, freq;
    QueryPerformanceCounter(&time);
    QueryPerformanceFrequency(&freq);
	double currTime = (double)time.QuadPart / (double) freq.QuadPart;

	return currTime-startTime;
}
#endif

#ifndef _WIN_
//returns very precise time in seconds since last "stamp"
double Elapsed() 
{
	if(start == 1)
	{
	   start = 0;
	   return 0;
 
	}
	
	//get current time
	struct timeval currTime;
	struct timezone currTz;
	gettimeofday(&currTime, &currTz);
	
	double t1 = (double)prevTime.tv_sec + (double) prevTime.tv_usec/(1000*1000);
	double t2 =  (double)currTime.tv_sec + (double)currTime.tv_usec/(1000*1000);
	return t2-t1;
}
#endif

#ifdef _WIN_
double Elapsed() 
{
	if(start == 1)
	{
	   start = 0;
	   return 0;
 
	}
	

	//get current time
	LARGE_INTEGER time, freq;
    QueryPerformanceCounter(&time);
    QueryPerformanceFrequency(&freq);
	double currTime = (double)time.QuadPart / (double) freq.QuadPart;
	
	double elapsed;
	if(isPaused)
	{
		UnPause();
		elapsed = currTime-prevTime;
		Pause();
	}
	else
		elapsed = currTime-prevTime;

	return elapsed;
}
#endif

//returns the # of frames that have elapsed since the last "stamp"
//frameTime is the time per frame in milliseconds
//factor is the scaling factor used to speed and slow the timer
int ElapsedFrames(double frameTime, double factor)
{
  //double elapSec = Elapsed();
  
  double total =  ((Elapsed() / (frameTime/1000)) + overflow)*factor;
  int result = (int) total;
  overflow = total - result;
  
  return result;
}

#ifdef _WIN_
//allow timer to be pauses in between "stamps"
void Pause()
{
	if(isPaused)
		return;

	//get current time
	LARGE_INTEGER time, freq;
    QueryPerformanceCounter(&time);
    QueryPerformanceFrequency(&freq);

	pauseTime = (double)time.QuadPart / (double) freq.QuadPart;
	isPaused = true;
}

//unpause the timer...
void UnPause()
{
	if(!isPaused)
		return;

	//get current time
	LARGE_INTEGER time, freq;
    QueryPerformanceCounter(&time);
    QueryPerformanceFrequency(&freq);
	double currTime = (double)time.QuadPart / (double) freq.QuadPart;

	prevTime += currTime - pauseTime;
	isPaused = false;
}
#endif
