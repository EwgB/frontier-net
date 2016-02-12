#ifndef glTYPES
#define glTYPES

#define GL_CLAMP_TO_EDGE 0x812F
#define GL_MAX_GLYPHS     256

enum
{
  GLUV_TOP_LEFT,
  GLUV_TOP_RIGHT,
  GLUV_BOTTOM_RIGHT,
  GLUV_BOTTOM_LEFT,
  GLUV_LEFT_EDGE,
  GLUV_RIGHT_EDGE,
  GLUV_TOP_EDGE,
  GLUV_BOTTOM_EDGE,
};

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
  bool    operator!= (const type& c);\
  bool    operator== (const type& c);

/*-----------------------------------------------------------------------------
GLcoord
-----------------------------------------------------------------------------*/

struct GLcoord
{
  int         x;
  int         y;
  void        Clear ();
  bool        Walk (int x_width, int y_width);
  bool        Walk (int size);
  
  bool        operator== (const GLcoord& c);
  bool        operator!= (const GLcoord& c);
  
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
  void        operator*= (const int& c) { x *= c; y *= c; };
  void        operator*= (const GLcoord& c) { x *= c.x; y *= c.y; };

};

/*-----------------------------------------------------------------------------
GLcoord
-----------------------------------------------------------------------------*/

struct GLquatx
{
  float       x;
  float       y;
  float       z;
  float       w;
};

/*-----------------------------------------------------------------------------
GLvector
-----------------------------------------------------------------------------*/

struct GLvector
{
  float       x;
  float       y;
  float       z;
  void        Normalize ();
  float       Length ();
  OPERATORS(GLvector);
};

std::ostream &operator<<(std::ostream &stream, GLvector &point);
std::istream &operator>>(std::istream &stream, GLvector &point);

typedef GLvector       GLvector3;

GLvector  glVector (float x, float y, float z);
GLvector  glVectorCrossProduct (GLvector v1, GLvector v2);
float     glVectorDotProduct (GLvector v1, GLvector v2);
void      glVectorGl (GLvector v);
GLvector  glVectorInterpolate (GLvector v1, GLvector v2, float scalar);
float     glVectorLength (GLvector v);
GLvector  glVectorNormalize (GLvector v);
GLvector  glVectorReflect (GLvector3 ray, GLvector3 normal);

/*-----------------------------------------------------------------------------
GLvector2
-----------------------------------------------------------------------------*/

struct GLvector2
{
  float       x;
  float       y;
  float       Length ();
  void        Normalize ();
  OPERATORS(GLvector2);
};

GLvector2 glVector (float x, float y);
GLvector2 glVectorAdd (GLvector2 val1, GLvector2 val2);
GLvector2 glVectorSubtract (GLvector2 val1, GLvector2 val2);
GLvector2 glVectorNormalize (GLvector2 v);
GLvector2 glVectorInterpolate (GLvector2 v1, GLvector2 v2, float scalar);
GLvector2 glVectorSinCos (float angle);
float     glVectorLength (GLvector2 v);

/*-----------------------------------------------------------------------------
GLuvbox
-----------------------------------------------------------------------------*/

struct GLuvbox
{
  GLvector2 ul;
  GLvector2 lr;
  void      Set (GLvector2 ul, GLvector2 lr);
  void      Set (int x, int y, int columns, int rows);
  void      Set (float repeats);
  GLvector2 Corner (unsigned index);
  GLvector2 Center ();

};

/*-----------------------------------------------------------------------------
GLrgba
-----------------------------------------------------------------------------*/

struct GLrgba
{
  float       red;
  float       green;
  float       blue;
  float       alpha;
  void        Clamp ();
  void        Normalize ();
  float       Brighness ();
  OPERATORS(GLrgba);
};

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

/*-----------------------------------------------------------------------------
GLmatrix
-----------------------------------------------------------------------------*/

struct GLmatrix
{
  float       elements[4][4];
  void        Identity ();
  void        Rotate (float theta, float x, float y, float z);
  void        Multiply (GLmatrix m);
  GLvector    TransformPoint (GLvector pt);
};


GLmatrix  glMatrixIdentity (void);
void      glMatrixElementsSet (GLmatrix* m, float* in);
GLmatrix  glMatrixMultiply (GLmatrix a, GLmatrix b);
GLmatrix  glMatrixScale (GLmatrix m, GLvector in);
GLvector  glMatrixTransformPoint (GLmatrix m, GLvector in);
GLmatrix  glMatrixTranslate (GLmatrix m, GLvector in);
GLmatrix  glMatrixRotate (GLmatrix m, float theta, float x, float y, float z);
GLvector  glMatrixToEuler (GLmatrix mat, int order);

/*-----------------------------------------------------------------------------
GLbbox
-----------------------------------------------------------------------------*/

struct GLbbox
{
  GLvector3   pmin;
  GLvector3   pmax;

  GLvector    Center ();
  void        ContainPoint (GLvector point);
  void        Clear ();
  void        Render ();
  GLvector    Size ();
};

/*-----------------------------------------------------------------------------
GLfont
-----------------------------------------------------------------------------*/

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
  GLglyph         Glyph (int ascii) const { return _glyph[ascii]; };
  int             GlyphDraw (int ascii, GLcoord origin) const;
  int             GlyphWidth (int ascii);
  int             LineHeight () const { return _line_height;}
};

/*-----------------------------------------------------------------------------
GLmesh
-----------------------------------------------------------------------------*/

struct GLmesh
{
  GLbbox            _bbox;
  vector<UINT>      _index;
  vector<GLvector>  _vertex;
  vector<GLvector>  _normal;
  vector<GLrgba>    _color;
  vector<GLvector2> _uv;

  void              CalculateNormals ();
  void              CalculateNormalsSeamless ();
  void              Clear ();
  void              PushTriangle (UINT i1, UINT i2, UINT i3);
  void              PushQuad (UINT i1, UINT i2, UINT i3, UINT i4);
  void              PushVertex (GLvector vert, GLvector normal, GLvector2 uv);
  void              PushVertex (GLvector vert, GLvector normal, GLrgba color, GLvector2 uv);
  void              RecalculateBoundingBox  ();
  void              Render ();
  unsigned          Triangles () { return _index.size () / 3; };
  unsigned          Vertices () { return _vertex.size (); };

  void    operator+= (const GLmesh& c);

};


#endif

