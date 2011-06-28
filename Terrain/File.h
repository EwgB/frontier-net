char* FileBinaryLoad (char* name, long* size);
int   FileCopy (char *from, char *to);
bool  FileDelete (char* name);
int   FileExists (char *name);
char* FileLoad (char* name, long* size);
long  FileModified (char *filename);
bool  FileSave (char *name, char *buf, int size);
void  FileTouch (char *filename);
void  FileCreateFolder (char* folder);
