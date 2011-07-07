enum
{
  SHADER_NONE = -1,
  SHADER_NORMAL,
  SHADER_TREES,
  SHADER_GRASS,
  SHADER_COUNT,
};

void CgInit ();
void CgUpdate ();
void CgUpdateMatrix ();
void CgShaderSelect (int shader);
