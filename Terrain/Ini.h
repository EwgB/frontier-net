int       IniInt (char* entry);
void      IniIntSet (char* entry, int val);
float     IniFloat (char* entry);
void      IniFloatSet (char* entry, float val);
char*     IniString (char* entry);
void      IniStringSet (char* entry, char* val);
void      IniVectorSet (char* entry, GLvector v);
GLvector  IniVector (char* entry);

int       IniInt (char* section, char* entry);
void      IniIntSet (char* section, char* entry, int val);
float     IniFloat (char* section, char* entry);
void      IniFloatSet (char* section, char* entry, float val);
char*     IniString (char* section, char* entry);
void      IniStringSet (char* section, char* entry, char* val);
void      IniVectorSet (char* section, char* entry, GLvector v);
GLvector  IniVector (char* section, char* entry);
