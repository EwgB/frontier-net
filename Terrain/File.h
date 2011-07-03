char* FileBinaryLoad (char* name, long* size);
int   FileCopy (char *from, char *to);
bool  FileDelete (char* name);
int   FileExists (char *name);
char* FileLoad (char* name, long* size);
void  FileMakeDirectory (char* folder);
long  FileModified (char *filename);
bool  FileSave (char *name, char *buf, int size);
void  FileTouch (char *filename);
void  FileCreateFolder (char* folder);
bool  FileXLoad (char* filename, class CFigure* fig);
char* FileImageLoad (char* filename, GLcoord* size_in);

