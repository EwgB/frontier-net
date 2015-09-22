#ifndef _CVARS_CONFIG_H_
#define _CVARS_CONFIG_H_

/// Version

#define CVARS_MAJOR_REV   2
#define CVARS_MINOR_REV   5
#define CVARS_PATCH_REV   0
#define CVARS_VERSION_STRING "2.5.0"

/// Platform
/* #undef _UNIX_ */
#define _WIN_
/* #undef _OSX_ */
/* #undef _LINUX_ */
/* #undef _ANDROID_ */

/// Compiler
/* #undef _GCC_ */
#define _MSVC_

/// Optional Libraries
/* #undef HAVE_APPLE_OPENGL_FRAMEWORK */
#define HAVE_GLEW
/* #undef HAVE_GLES */
#define HAVE_FREEGLUT
/* #undef HAVE_MODIFIED_OSXGLUT */

#endif //_CVARS_CONFIG_H_
