/*
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

struct GLquatx
{
  float       x;
  float       y;
  float       z;
  float       w;
};

#endif
*/