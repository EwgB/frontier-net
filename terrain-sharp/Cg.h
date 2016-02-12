enum
{
  VSHADER_NONE = -1,
  VSHADER_NORMAL,
  VSHADER_TREES,
  VSHADER_GRASS,
  VSHADER_CLOUDS,
  VSHADER_COUNT,
  FSHADER_NONE,
  FSHADER_GREEN,
  FSHADER_CLOUDS,
  FSHADER_MASK_TRANSFER,
  FSHADER_END,
};

#define FSHADER_BASE  (FSHADER_NONE + 1)
#define FSHADER_COUNT (FSHADER_END - FSHADER_BASE)

void CgCompile ();
void CgInit ();
void CgUpdate ();
void CgUpdateMatrix ();
void CgSetOffset (GLvector offset);
void CgShaderSelect (int shader);
