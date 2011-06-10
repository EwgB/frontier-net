
class BMPFile
{

  void convertBGRtoRGB();

public:
  byte*   data;
  DWORD   sizeX;
  DWORD   sizeY;
  bool    NoErrors;

  BMPFile(): NoErrors(false), data(NULL) {};
  BMPFile(const char *FileName);
  ~BMPFile();

  bool loadFile(const char *FileName);

  friend BMPFile *ImageLoad(const char *FileName);

};