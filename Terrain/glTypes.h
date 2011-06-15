#ifndef glTYPES
#define glTYPES

#define GL_CLAMP_TO_EDGE 0x812F
#define GL_MAX_GLYPHS     256

#define OPERATORS(type)     \
  type    operator+  (const type& c); \
  type    operator+  (const float& c);\
  void    operator+= (const type& c);\
  void    operator+= (const float& c);\
  type    operator-  (const type& c);\
  type    operator-  (const float& c);\
  void    operator-= (const type& c);\
  void    operator-= (const float& c);\
  type    operator*  (const type& c);\
  type    operator*  (const float& c);\
  void    operator*= (const type& c);\
  void    operator*= (const float& c);\
  type    operator/  (const type& c);\
  type    operator/  (const float& c);\
  void    operator/= (const type& c);\
  void    operator/= (const float& c);\
  bool    operator== (const type& c);

struct GLcoord
{
  int         x;
  int         y;
  void        Clear ();
  bool        Walk (int x_width, int y_width);
  bool        Walk (int size);
  
  bool        operator== (const GLcoord& c);
  
  GLcoord     operator+  (const int& c);
  GLcoord     operator+  (const GLcoord& c);
  void        operator+= (const float& c) { x += (int)c; y += (int)c; };
  void        operator+= (const int& c) { x += c; y += c; };
  void        operator+= (const GLcoord& c) { x += c.x; y += c.y; };

  GLcoord     operator-  (const int& c);
  GLcoord     operator-  (const GLcoord& c);
  void        operator-= (const float& c) { x -= (int)c; y -= (int)c; };
  void        operator-= (const int& c) { x -= c; y -= c; };
  void        operator-= (const GLcoord& c) { x -= c.x; y -= c.y; };

  GLcoord     operator*  (const int& c);
  GLcoord     operator*  (const GLcoord& c);
  void        operator*= (const int& c) { x -= c; y -= c; };
  void        operator*= (const GLcoord& c) { x -= c.x; y -= c.y; };

};

struct GLquat
{
  float       x;
  float       y;
  float       z;
  float       w;
};

struct GLvector
{
  float       x;
  float       y;
  float       z;
  OPERATORS(GLvector);
};

typedef GLvector       GLvector3;

struct GLvector2
{
  float       x;
  float       y;
  OPERATORS(GLvector2);
};

struct GLrgba
{
  float       red;
  float       green;
  float       blue;
  float       alpha;
  OPERATORS(GLrgba);
  void        Clamp ();
  void        Normalize ();
};

struct GLmatrix
{
  float       elements[4][4];
};

struct GLbbox
{
  GLvector3   min;
  GLvector3   max;

  void        ContainPoint (GLvector point);
  void        Clear ();
  void        Render ();
};

struct GLvertex
{
  GLvector3   position;
  GLvector3   normal;
  GLvector2   uv;
  GLrgba      color;
};

struct GLrect
{
  float       left;
  float       top;
  float       right;
  float       bottom;
};

struct GLtriangle
{
  int         v[3];
};

struct GLquad
{
  int         v[4];
};

struct GLglyph
{
  char        chr;
  GLcoord     size;
  GLvector2   uv1;
  GLvector2   uv2;
  int         advance;
  char*       buffer;
  
};

struct GLfont
{
private:
  GLglyph         _glyph[GL_MAX_GLYPHS];
  int             _id;
  int             _line_height;
public:
  void            FaceSet (unsigned id);
  void            Select ();
  GLglyph         Glyph (int ascii) { return _glyph[ascii]; };
  int             GlyphDraw (int ascii, GLcoord origin);
  int             GlyphWidth (int ascii);
  int             LineHeight () { return _line_height;}
};

GLbbox    glBboxClear (void);
GLbbox    glBboxContainPoint (GLbbox box, GLvector point);
bool      glBboxTestPoint (GLbbox box, GLvector point);

GLrgba    glRgba (char* string);
GLrgba    glRgba (float red, float green, float blue);
GLrgba    glRgba (float luminance);
GLrgba    glRgba (float red, float green, float blue, float alpha);
GLrgba    glRgba (long c);
GLrgba    glRgba (int red, int green, int blue);
GLrgba    glRgbaAdd (GLrgba c1, GLrgba c2);
GLrgba    glRgbaSubtract (GLrgba c1, GLrgba c2);
GLrgba    glRgbaInterpolate (GLrgba c1, GLrgba c2, float delta);
GLrgba    glRgbaScale (GLrgba c, float scale);
GLrgba    glRgbaMultiply (GLrgba c1, GLrgba c2);
GLrgba    glRgbaUnique (int i);
GLrgba    glRgbaFromHsl (float h, float s, float l);

GLmatrix  glMatrixIdentity (void);
void      glMatrixElementsSet (GLmatrix* m, float* in);
GLmatrix  glMatrixMultiply (GLmatrix a, GLmatrix b);
GLmatrix  glMatrixScale (GLmatrix m, GLvector in);
GLvector  glMatrixTransformPoint (GLmatrix m, GLvector in);
GLmatrix  glMatrixTranslate (GLmatrix m, GLvector in);
GLmatrix  glMatrixRotate (GLmatrix m, float theta, float x, float y, float z);
GLvector  glMatrixToEuler (GLmatrix mat, int order);

GLquat    glQuat (float x, float y, float z, float w);
GLvector  glQuatToEuler (GLquat q, int order);

GLvector  glVector (float x, float y, float z);
GLvector  glVectorCrossProduct (GLvector v1, GLvector v2);
float     glVectorDotProduct (GLvector v1, GLvector v2);
void      glVectorGl (GLvector v);
GLvector  glVectorInterpolate (GLvector v1, GLvector v2, float scalar);
float     glVectorLength (GLvector v);
GLvector  glVectorNormalize (GLvector v);
GLvector  glVectorReflect (GLvector3 ray, GLvector3 normal);

GLvector2 glVector (float x, float y);
GLvector2 glVectorAdd (GLvector2 val1, GLvector2 val2);
GLvector2 glVectorSubtract (GLvector2 val1, GLvector2 val2);
GLvector2 glVectorNormalize (GLvector2 v);
GLvector2 glVectorInterpolate (GLvector2 v1, GLvector2 v2, float scalar);
GLvector2 glVectorSinCos (float angle);
float     glVectorLength (GLvector2 v);


#endif

