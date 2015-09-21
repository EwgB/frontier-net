/*
\file GLFont.h 
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: GLFont.h 183 2010-07-18 15:20:20Z effer $
*/

#define MAX_TEXT_LENGTH 512

namespace CVars.GLConsole {
	using System.Diagnostics;
	using OpenTK.Graphics.OpenGL;

	internal class GLFont {
		//friend inline bool GLFontCheckInit( GLFont* pFont );

		// fixed width
		private int CharWidth { get; set; }
		// fixed width
		private int CharHeight { get; set; }
		// number of display lists
		private int NumLists { get; set; }
		// base number for display lists
		private int DisplayListBase { get; set; }
		private bool InitDone { get; set; }

		private static int GlobalDisplayListBase = -1;

		public GLFont() {
			NumLists = 96;
			CharWidth = 8;
			CharHeight = 13;
			InitDone = false;
		}

		// printf style function take position to print to as well
		// NB: coordinates start from bottom left
		//void glPrintf(int x, int y, const char *fmt, ...);
		//void glPrintf(int x, int y, const std::string fmt, ...){ glPrintf(x,y, fmt.c_str()); }
		//void glPrintfFast(int x, int y, const char *fmt, ...);
		//void glPrintfFast(int x, int y, const std::string fmt, ...){ glPrintfFast(x,y, fmt.c_str()); }

		private bool GLFontCheckInit(GLFont pFont = null) {
			// make sure glutInit has been called
			if (glutGet(GLUT_ELAPSED_TIME) <= 0) {
				//fprintf( stderr, "WARNING: GLFontCheckInit failed after 'glutGet(GLUT_ELAPSED_TIME) <= 0' check\n" );
				return false;
			}

			if (!pFont.InitDone) {
				Debug.Assert(pFont != null);
				// GLUT bitmapped fonts...  
				pFont.DisplayListBase = GL.GenLists(pFont.NumLists);
				if (pFont.DisplayListBase == 0) {
					//    hmm, commented out for now because on my linux box w get here sometimes
					//    even though glut hasn't been initialized.
					//            fprintf( stderr, "%i", pFont.NumLists );
					//Log.Error("GLFontCheckInit() -- out of display lists\n");
					return false;
				}
				for (int nList = pFont.DisplayListBase;
								nList < pFont.DisplayListBase + pFont.NumLists; nList++) {
					GL.NewList(nList, ListMode.Compile);
					OpenTK.
					glutBitmapCharacter(GLUT_BITMAP_8_BY_13, nList + 32 - pFont.DisplayListBase);
					GL.EndList();
				}

				GlobalDisplayListBase = pFont.DisplayListBase;
				pFont.InitDone = true;
				return false;
			} else {
				Debug.Assert(GlobalDisplayListBase > 0);
				pFont.DisplayListBase = GlobalDisplayListBase;
			}
			return true;
		}
	}
}


////////////////////////////////////////////////////////////////////////////////
inline GLFont::~GLFont()
{
    if( InitDone && GLFontCheckInit(this) ) {
        glDeleteLists( DisplayListBase, DisplayListBase + NumLists );
    } 
}
 
////////////////////////////////////////////////////////////////////////////////
// printf style print function
// NB: coordinates start from bottom left
inline void GLFont::glPrintf(int x, int y, const char *fmt, ...)   
{
    GLFontCheckInit(this);

    char        text[MAX_TEXT_LENGTH];                  // Holds Our String
    va_list     ap;                                     // Pointer To List Of Arguments

    if( fmt == null ) {                                 // If There's No Text
        return;                                         // Do Nothing
    }

    va_start( ap, fmt );                                // Parses The String For Variables
    vsnprintf( text, MAX_TEXT_LENGTH, fmt, ap );         // And Converts Symbols To Actual Numbers
    va_end( ap );                                       // Results Are Stored In Text

    glDisable(GL_DEPTH_TEST); //causes text not to clip with geometry
    //position text correctly...

    // This saves our transform (matrix) information and our current viewport information.
    glPushAttrib( GL_TRANSFORM_BIT | GL_VIEWPORT_BIT );
    // Use a new projection and modelview matrix to work with.
    glMatrixMode( GL_PROJECTION );              
    glPushMatrix();                                 
    glLoadIdentity();                               
    glMatrixMode( GL_MODELVIEW );                   
    glPushMatrix();                                     
    glLoadIdentity();                                   
    //create a viewport at x,y, but doesnt have any width (so we end up drawing there...)
    glViewport( x - 1, y - 1, 0, 0 );                   
    //This actually positions the text.
    glRasterPos4f( 0, 0, 0, 1 );
    //undo everything
    glPopMatrix();                                      // Pop the current modelview matrix off the stack
    glMatrixMode( GL_PROJECTION );                      // Go back into projection mode
    glPopMatrix();                                      // Pop the projection matrix off the stack
    glPopAttrib();                                      // This restores our TRANSFORM and VIEWPORT attributes

    //glRasterPos2f(x, y);

    glPushAttrib( GL_LIST_BIT );                        // Pushes The Display List Bits
    glListBase( DisplayListBase - 32 );      // Sets The Base Character to 32
    //glScalef( 0.5, 0.5, 0.5 ); 
    glCallLists( strlen(text), GL_UNSIGNED_BYTE, text );// Draws The Display List Text
    glPopAttrib();                                      // Pops The Display List Bits
    glEnable(GL_DEPTH_TEST);
}

////////////////////////////////////////////////////////////////////////////////
//printf style print function
//NOTE: coordinates start from bottom left
//ASSUMES ORTHOGRAPHIC PROJECTION ALREADY SET UP...
inline void GLFont::glPrintfFast(int x, int y, const char *fmt, ...)   
{
    GLFontCheckInit(this);

    char        text[MAX_TEXT_LENGTH];// Holds Our String
    va_list     ap;                   // Pointer To List Of Arguments

    if( fmt == null ) {               // If There's No Text
        return;                       // Do Nothing
    }

    va_start( ap, fmt );                            // Parses The String For Variables
    vsnprintf( text, MAX_TEXT_LENGTH, fmt, ap );    // And Converts Symbols To Actual Numbers
    va_end( ap );                                   // Results Are Stored In Text

    glDisable( GL_DEPTH_TEST ); // Causes text not to clip with geometry
    glRasterPos2f( x, y );
    //glPushAttrib( GL_LIST_BIT );                        // Pushes The Display List Bits
    glListBase( DisplayListBase - 32 );        // Sets The Base Character to 32
    glCallLists( strlen(text), GL_UNSIGNED_BYTE, text );  // Draws The Display List Text
    //glPopAttrib();                                      // Pops The Display List Bits
    glEnable( GL_DEPTH_TEST );
}
