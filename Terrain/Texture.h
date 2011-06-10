enum
{
  MASK_NONE,
  MASK_PINK,
  MASK_LUMINANCE,
};

class GLtexture
{
public:
  GLtexture*        next;
  GLuint            id;
  char              name[16];
  char*             image_name;
  int               width;
  int               height;
  short             bpp;//bytes per pixel
};

unsigned    TextureIdFromName (char* name);
GLtexture*  TextureFromName (char* name, int mask_type);
GLtexture*  TextureFromName (char* name);
byte*       TextureRaw (char* name, int* width, int* height);
void        TextureInit (void);
void        TextureTerm (void);
void        TexturePurge ();
