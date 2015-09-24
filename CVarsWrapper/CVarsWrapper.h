// CVarsWrapper.h

#pragma once

#include "..\CVars\include\CVars\config.h"
#include "..\CVars\include\CVars\CVar.h"
#include "..\CVars\include\CVars\CVarMapIO.h"
#include "..\CVars\include\CVars\cvars_tinyxml.h"
#include "..\CVars\include\CVars\CVarVectorIO.h"
#include "..\CVars\include\CVars\glplatform.h"
#include "..\CVars\include\CVars\Timestamp.h"
#include "..\CVars\include\CVars\Trie.h"
#include "..\CVars\include\CVars\TrieNode.h"

#include "..\CVars\src\CVar.cpp"
#include "..\CVars\src\CVarParse.cpp"
#include "..\CVars\src\cvars_tinyxml.cpp"
#include "..\CVars\src\cvars_tinyxmlerror.cpp"
#include "..\CVars\src\cvars_tinyxmlparser.cpp"
#include "..\CVars\src\Timestamp.cpp"
#include "..\CVars\src\Trie.cpp"
#include "..\CVars\src\TrieNode.cpp"

using namespace System;

namespace CVarsWrapper {

	public ref class Class1
	{
		// TODO: Add your methods for this class here.
	};
}
