class GLtexture
{
public:
  GLtexture*        next;
  GLuint            id;
  char              name[32];
  char*             image_name;
  int               width;
  int               height;
  short             bpp;//bytes per pixel
};

unsigned    TextureIdFromName (const char* name);
GLtexture*  TextureFromName (const char* name);
byte*       TextureRaw (char* name, int* width, int* height);
void        TextureInit (void);
void        TextureTerm (void);
void        TexturePurge ();
