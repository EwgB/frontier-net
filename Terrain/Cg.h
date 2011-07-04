enum
{
  SHADER_NONE = -1,
  SHADER_NORMAL,
  SHADER_TREES,
  SHADER_COUNT,
};

void CgInit ();
//void CgOff ();
void CgUpdate ();
void CgUpdateMatrix ();
void CgShaderSelect (int shader);
