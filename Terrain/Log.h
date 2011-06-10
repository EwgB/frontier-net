#define LOG_SDK       RGB(0,0,128)
#define LOG_PROPERTY  RGB(96,96,0)
#define LOG_EVENT     RGB(96,0,96)
#define LOG_ERROR     RGB(160,0,0)

void LogInit (char* log_file_name);
void LogTerm (void);
void Log (char* message, ...);
void Log (long color, char *message, ...);
void LogFile (char* message, ...);
void LogConsole (char*message, ...);


