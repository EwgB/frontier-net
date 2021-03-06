/*-----------------------------------------------------------------------------

  glRgba.cpp

  2009 Shamus Young

-------------------------------------------------------------------------------

  Functions for dealing with RGBA color values.

-----------------------------------------------------------------------------*/

#include "stdafx.h"
#include <stdio.h>
#include <math.h>

#include "math.h"

//This is a list of the integers from 0 to 511, in random order. Used for 
//scrambling the unique colors function.

static int    color_mix[] = 
{
  0x17D, 0x1DA, 0x008, 0x00F, 0x12C, 0x110, 0x09F, 0x157, 0x1CF, 0x1A6, 0x17C, 0x1A4, 0x035, 0x141, 0x174, 0x119,
  0x0A7, 0x0B0, 0x1BE, 0x0D2, 0x14D, 0x007, 0x13F, 0x09E, 0x108, 0x030, 0x058, 0x1D0, 0x1F4, 0x0F3, 0x0C0, 0x18C,
  0x0A5, 0x03B, 0x070, 0x1C1, 0x061, 0x0CD, 0x14F, 0x1D3, 0x1DC, 0x08F, 0x1A4, 0x183, 0x0E6, 0x0FB, 0x1E0, 0x1A5,
  0x192, 0x17B, 0x19A, 0x1F0, 0x068, 0x032, 0x0CE, 0x145, 0x1E3, 0x1E2, 0x1BA, 0x102, 0x0EA, 0x002, 0x155, 0x1E9,
  0x170, 0x19C, 0x0E8, 0x1A7, 0x065, 0x14B, 0x1CC, 0x123, 0x03D, 0x1D8, 0x1CD, 0x021, 0x0ED, 0x1A3, 0x0C0, 0x10F,
  0x198, 0x07C, 0x1AC, 0x0A0, 0x1BC, 0x0FE, 0x147, 0x0AB, 0x1D6, 0x186, 0x111, 0x158, 0x11B, 0x0C7, 0x158, 0x0B3,
  0x133, 0x1BE, 0x11F, 0x0BE, 0x193, 0x088, 0x1EE, 0x1F6, 0x06B, 0x169, 0x166, 0x154, 0x0D0, 0x0D8, 0x15F, 0x0BD,
  0x088, 0x00E, 0x161, 0x097, 0x1F2, 0x18F, 0x192, 0x1FB, 0x0DA, 0x0B4, 0x0AF, 0x1A5, 0x179, 0x0DD, 0x1E8, 0x028,
  0x18A, 0x01D, 0x117, 0x1F1, 0x1AC, 0x086, 0x13C, 0x159, 0x0A9, 0x113, 0x09A, 0x186, 0x0DC, 0x143, 0x19D, 0x052,
  0x0D5, 0x061, 0x0BA, 0x0E9, 0x13E, 0x1B4, 0x1FD, 0x0C2, 0x0F6, 0x1DD, 0x150, 0x157, 0x13A, 0x1AD, 0x012, 0x09F,
  0x095, 0x151, 0x036, 0x1E1, 0x0EF, 0x08D, 0x18D, 0x1B9, 0x1E6, 0x176, 0x1C2, 0x0CF, 0x1B6, 0x1B8, 0x1DE, 0x05E,
  0x04E, 0x183, 0x152, 0x078, 0x01B, 0x16A, 0x1FD, 0x0DB, 0x1ED, 0x051, 0x0FE, 0x116, 0x14E, 0x1D1, 0x09B, 0x189,
  0x056, 0x137, 0x091, 0x09D, 0x0E3, 0x1DB, 0x0FC, 0x0F0, 0x1D0, 0x1FE, 0x163, 0x1E9, 0x16F, 0x0AE, 0x05C, 0x1BC,
  0x1E3, 0x10C, 0x1DE, 0x11B, 0x149, 0x0BB, 0x18A, 0x1B5, 0x11D, 0x0AD, 0x1F7, 0x020, 0x119, 0x1D9, 0x108, 0x1AF,
  0x0CC, 0x15C, 0x17D, 0x1F3, 0x118, 0x1CA, 0x168, 0x03F, 0x1A2, 0x0AF, 0x0A1, 0x0F6, 0x0D4, 0x05C, 0x0EC, 0x1FF,
  0x01C, 0x015, 0x1C1, 0x16C, 0x199, 0x100, 0x160, 0x14E, 0x14C, 0x126, 0x095, 0x1EB, 0x083, 0x14A, 0x0CB, 0x1A3,
  0x16B, 0x182, 0x1C2, 0x1C8, 0x13A, 0x170, 0x1E0, 0x04B, 0x1D6, 0x0CB, 0x1B8, 0x14C, 0x02B, 0x179, 0x1B2, 0x1C4,
  0x168, 0x017, 0x18B, 0x19E, 0x0FD, 0x0A5, 0x148, 0x04D, 0x0B1, 0x0B6, 0x1F7, 0x046, 0x1BF, 0x12B, 0x1A8, 0x084,
  0x156, 0x0C4, 0x131, 0x080, 0x0E2, 0x13D, 0x1CB, 0x151, 0x1D9, 0x1F3, 0x02E, 0x116, 0x01E, 0x167, 0x1C3, 0x125,
  0x132, 0x190, 0x199, 0x1FC, 0x1D5, 0x03A, 0x18B, 0x0C5, 0x08E, 0x0EA, 0x039, 0x18E, 0x099, 0x048, 0x001, 0x12D,
  0x1AA, 0x10D, 0x02A, 0x0A9, 0x164, 0x0F5, 0x0EB, 0x1A8, 0x11E, 0x15D, 0x1C7, 0x1AF, 0x188, 0x048, 0x0D1, 0x1E2,
  0x098, 0x1FE, 0x19F, 0x1A1, 0x0A8, 0x050, 0x1B5, 0x06D, 0x160, 0x1EE, 0x15C, 0x16E, 0x163, 0x154, 0x196, 0x1FF,
  0x0F3, 0x038, 0x059, 0x064, 0x181, 0x1D2, 0x133, 0x190, 0x125, 0x0BD, 0x16F, 0x1E5, 0x1AB, 0x101, 0x17F, 0x140,
  0x060, 0x1ED, 0x194, 0x0FF, 0x0C1, 0x1CB, 0x0CA, 0x069, 0x1C6, 0x1D4, 0x1C0, 0x0D9, 0x171, 0x1F2, 0x1D3, 0x128,
  0x173, 0x1D7, 0x17E, 0x191, 0x05A, 0x19F, 0x0DF, 0x189, 0x1E1, 0x1FA, 0x1A2, 0x03E, 0x0FC, 0x1EA, 0x1BB, 0x102,
  0x12C, 0x185, 0x13F, 0x114, 0x191, 0x0DB, 0x1FB, 0x1BF, 0x1A9, 0x066, 0x174, 0x0E8, 0x0A3, 0x197, 0x0CD, 0x134,
  0x18D, 0x0F4, 0x0DC, 0x0FA, 0x1F8, 0x1DF, 0x03D, 0x1D7, 0x000, 0x1D4, 0x1C9, 0x072, 0x055, 0x1C5, 0x1DA, 0x1DB,
  0x173, 0x10A, 0x080, 0x1E6, 0x148, 0x19A, 0x1B9, 0x177, 0x1F6, 0x1B7, 0x1CE, 0x1FC, 0x1C7, 0x175, 0x1DC, 0x1B0,
  0x0FB, 0x107, 0x1EF, 0x111, 0x054, 0x122, 0x1EB, 0x15E, 0x110, 0x129, 0x0BA, 0x162, 0x0C2, 0x136, 0x146, 0x1B6,
  0x184, 0x1F5, 0x1F9, 0x0C8, 0x196, 0x177, 0x07F, 0x112, 0x17F, 0x094, 0x120, 0x0DA, 0x143, 0x109, 0x198, 0x16D,
  0x0D3, 0x1C8, 0x195, 0x10F, 0x06B, 0x1EA, 0x035, 0x031, 0x0D7, 0x12A, 0x14B, 0x0E4, 0x1F9, 0x0BC, 0x152, 0x074,
  0x15D, 0x060, 0x1A1, 0x0C9, 0x043, 0x10E, 0x121, 0x194, 0x1C0, 0x1E4, 0x079, 0x02C, 0x1A9, 0x178, 0x086, 0x1A6, 
};

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaFromHsl (float h, float sl, float l)
{
  
  float v;
  float r,g,b;
  
  
  r = l;   // default to gray
  g = l;
  b = l;
  v = (l <= 0.5f) ? (l * (1.0f + sl)) : (l + sl - l * sl);
  if (v > 0)  {
    float m;
    float sv;
    int sextant;
    float fract, vsf, mid1, mid2;
   
    m = l + l - v;
    sv = (v - m ) / v;
    h *= 6.0f;
    sextant = (int)h;
    fract = h - sextant;
    vsf = v * sv * fract;
    mid1 = m + vsf;
    mid2 = v - vsf;
    switch (sextant) {
    case 0:
      r = v;  g = mid1; b = m;
      break;
    case 1:
      r = mid2; g = v;  b = m;
      break;
    case 2:
      r = m;  g = v;  b = mid1;
      break;
    case 3:
      r = m; g = mid2; b = v;
      break;
    case 4:
      r = mid1; g = m; b = v;
      break;
    case 5:
      r = v;  g = m; b = mid2;
      break;
    }
  }
  return glRgba (r, g, b);

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaInterpolate (GLrgba c1, GLrgba c2, float delta)
{

  GLrgba     result;

  result.red = MathInterpolate (c1.red, c2.red, delta);
  result.green = MathInterpolate (c1.green, c2.green, delta);
  result.blue = MathInterpolate (c1.blue, c2.blue, delta);
  result.alpha = MathInterpolate (c1.alpha, c2.alpha, delta);
  return result;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaAdd (GLrgba c1, GLrgba c2)
{

  GLrgba     result;

  result.red = c1.red + c2.red;
  result.green = c1.green + c2.green;
  result.blue = c1.blue + c2.blue;
  return result;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaSubtract (GLrgba c1, GLrgba c2)
{

  GLrgba     result;

  result.red = c1.red - c2.red;
  result.green = c1.green - c2.green;
  result.blue = c1.blue - c2.blue;
  return result;

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaMultiply (GLrgba c1, GLrgba c2)
{

  GLrgba     result;

  result.red = c1.red * c2.red;
  result.green = c1.green * c2.green;
  result.blue = c1.blue * c2.blue;
  return result;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgbaScale (GLrgba c, float scale)
{

  c.red *= scale;
  c.green *= scale;
  c.blue *= scale;
  return c;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (char* string)
{

  long    color;
  char    buffer[10];
  char*   pound;
  GLrgba  result;

  strncmp (buffer, string, 10);
  if (pound = strchr (buffer, '#'))
    pound[0] = ' ';
  if (sscanf (string, "%x", &color) != 1)
	  return glRgba (0.0f);
  result.red = (float)GetBValue (color) / 255.0f;
  result.green = (float)GetGValue (color) / 255.0f;
  result.blue = (float)GetRValue (color) / 255.0f;
  result.alpha = 1.0f;
  return result;  

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (int red, int green, int blue)
{

  GLrgba     result;

  result.red = (float)red / 255.0f;
  result.green = (float)green / 255.0f;
  result.blue = (float)blue / 255.0f;
  result.alpha = 1.0f;
  return result;  

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (float red, float green, float blue)
{

  GLrgba     result;

  result.red = red;
  result.green = green;
  result.blue = blue;
  result.alpha = 1.0f;
  return result;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (float red, float green, float blue, float alpha)
{

  GLrgba     result;

  result.red = red;
  result.green = green;
  result.blue = blue;
  result.alpha = alpha;
  return result;

}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (long c)
{

  GLrgba     result;

  result.red = (float)GetRValue (c) / 255.0f;
  result.green = (float)GetGValue (c) / 255.0f;
  result.blue = (float)GetBValue (c) / 255.0f;
  result.alpha = 1.0f;
  return result;

}

/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

GLrgba glRgba (float luminance)
{

  GLrgba     result;

  result.red = luminance;
  result.green = luminance;
  result.blue = luminance;
  result.alpha = 1.0f;
  return result;

}

/*-----------------------------------------------------------------------------
Takes the given index and returns a "random" color unique for that index.
512 Unique values: #0 and #512 will be the same, as will #1 and #513, etc
Useful for visual debugging in some situations.
-----------------------------------------------------------------------------*/

GLrgba glRgbaUnique (int i)
{

  GLrgba    c;

  i = color_mix[i % 512];
  c.alpha = 1.0f;
  c.red   = 0.3f + ((i & 1) ? 0.15f : 0.0f) + ((i &  8) ? 0.2f : 0.0f) - ((i &  64) ? 0.35f : 0.0f);
  c.green = 0.3f + ((i & 2) ? 0.15f : 0.0f) + ((i & 32) ? 0.2f : 0.0f) - ((i & 128) ? 0.35f : 0.0f);
  c.blue  = 0.3f + ((i & 4) ? 0.15f : 0.0f) + ((i & 16) ? 0.2f : 0.0f) - ((i & 256) ? 0.35f : 0.0f);
  return c;

}

/*-----------------------------------------------------------------------------
  + operator                          
-----------------------------------------------------------------------------*/

GLrgba GLrgba::operator+ (const GLrgba& c)
{
  return glRgba (red + c.red, green + c.green, blue + c.blue, alpha);
}

GLrgba GLrgba::operator+ (const float& c)
{
  return glRgba (red + c, green + c, blue + c, alpha);
} 

void GLrgba::operator+= (const GLrgba& c)
{
  red += c.red;
  green += c.green;
  blue += c.blue;
}

void GLrgba::operator+= (const float& c)
{
  red += c;
  green += c;
  blue += c;
}

/*-----------------------------------------------------------------------------
  - operator                          
-----------------------------------------------------------------------------*/

GLrgba GLrgba::operator- (const GLrgba& c)
{
  return glRgba (red - c.red, green - c.green, blue - c.blue);
}

GLrgba GLrgba::operator- (const float& c)
{
  return glRgba (red - c, green - c, blue - c, alpha);
}

void GLrgba::operator-= (const GLrgba& c)
{
  red -= c.red;
  green -= c.green;
  blue -= c.blue;
}

void GLrgba::operator-= (const float& c)
{
  red -= c;
  green -= c;
  blue -= c;
}

/*-----------------------------------------------------------------------------
  * operator                          
-----------------------------------------------------------------------------*/

GLrgba GLrgba::operator* (const GLrgba& c)
{
  return glRgba (red * c.red, green * c.green, blue * c.blue);
}

GLrgba GLrgba::operator* (const float& c)
{
  return glRgba (red * c, green * c, blue * c, alpha);
}

void GLrgba::operator*= (const GLrgba& c)
{
  red *= c.red;
  green *= c.green;
  blue *= c.blue;
}

void GLrgba::operator*= (const float& c)
{
  red *= c;
  green *= c;
  blue *= c;
}

/*-----------------------------------------------------------------------------
  / operator                          
-----------------------------------------------------------------------------*/

GLrgba GLrgba::operator/ (const GLrgba& c)
{
  return glRgba (red / c.red, green / c.green, blue / c.blue);
}

GLrgba GLrgba::operator/ (const float& c)
{
  return glRgba (red / c, green / c, blue / c, alpha);
}

void GLrgba::operator/= (const GLrgba& c)
{
  red /= c.red;
  green /= c.green;
  blue /= c.blue;
}

void GLrgba::operator/= (const float& c)
{
  red /= c;
  green /= c;
  blue /= c;
}

bool GLrgba::operator==  (const GLrgba& c)
{
  return (red == c.red && green == c.green && blue == c.blue);
}


/*-----------------------------------------------------------------------------

-----------------------------------------------------------------------------*/

void GLrgba::Clamp ()
{

  red = clamp (red, 0.0f, 1.0f);
  green = clamp (green, 0.0f, 1.0f);
  blue = clamp (blue, 0.0f, 1.0f);
  alpha = clamp (alpha, 0.0f, 1.0f);

}

void GLrgba::Normalize ()
{

  float   n;

  n = max (red, max (green, blue));
  if (n > 1.0f) {
    red /= n;
    green /= n;
    blue /= n;
  }

}
    

float GLrgba::Brighness ()
{

  return (red + blue + green) / 3.0f;

}

  
