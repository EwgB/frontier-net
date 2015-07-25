/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: CVar.h 201 2012-10-17 18:43:27Z effer $
*/

namespace CVars {
	using System.IO;
	using Interfaces;

	delegate void SerialisationFunction<T>(StringWriter sw, T val);
	delegate void DeserialisationFunction<T>(StringReader sr, T val);

	class CVar<T> where T : ICVarValue {
		public string VarName { get; set; }
		public T VarData { get; set; }
		public bool Serialise { get; set; }

		private string Help { get; set; }

		private SerialisationFunction<T> Serialisation;
		private DeserialisationFunction<T> Deserialisation;

		///<summary>
		/// If serialise false, this CVar will not be taken into account when serialising (eg saving) the Trie
		///</summary>
		public CVar(string varName, T varValue, string help = "No help available", bool serialise = true,
			SerialisationFunction<T> serialisation = null, DeserialisationFunction<T> deserialisation = null) {
			Serialisation = serialisation;
			Deserialisation = deserialisation;

			VarData = varValue;
			VarName = varName;
			Serialise = serialise;
			Help = help;
		}

		///<summary>
		/// Convert value to string representation
		/// Call the original function that was installed at object creation time,
		/// regardless of current object class type T.
		///</summary>
		public string GetValueAsString() {
			if (Serialisation != null) {
				using (var sw = new StringWriter()) {
					Serialisation(sw, VarData);
					return sw.ToString();
				}
			} else {
				return VarData.ToString();
			}
		}

		// Convert string representation to value
		// Call the original function that was installed at object creation time,
		// regardless of current object class type T.
		void SetValueFromString(string value) {
			if (Deserialisation != null) {
				using (var sr = new StringReader(value)) {
					Deserialisation(sr, VarData);
				}
			} else {
				VarData.Parse(value);
			}
		}
/*
			////////////////////////////////////////////////////////////////////////////////
			// Convert type to string representation
			// Call the original function that was installed at object creation time,
			// regardless of current object class type T.
			string type() {
			return (*m_pTypeStringFuncPtr)(VarData);
		}

		////////////////////////////////////////////////////////////////////////////////
		// Get values to and from a string representation (used for
		// serialization and console interaction)
		bool FromString(string s); //return true if successful

		const string& GetHelp() {
			return Help;
		}


		// pointer to func to get CVar type as a string
		string (* m_pTypeStringFuncPtr)( T* t );

								// pointer to func to get CVar value as a string
								string (* m_pValueStringFuncPtr)( T* t );

								// pointer to func to set CVar Value from a string
								void (* m_pSetValueFuncPtr)( T* t, const string & );

								ostream& (* Serialisation)( ostream &, T );
								istream& (* Deserialisation)( istream &, T ) ;
						};

		}}
				////////////////////////////////////////////////////////////////////////////////
				// A global Trie structure holds all of the variables organized by their string
				// name.  He lives in Trie.cpp.
				Trie& TrieInstance();

				class CVarRef<T> where T : class {
					public CVarRef(T reference = null)  { var = reference; }
					T var;
				}

			////////////////////////////////////////////////////////////////////////////////
			// This is our generic data-to-string function, there is one instantiated for
			// every kind of CVar type that gets declared.  To print, the CVar type merely
			// has to overload <<.  To support reading from the console, just overload >>.
			template<class T>
						string CVarValueString(T* t) {
				ostringstream oss;
				oss << *t;
				return oss.str();
			}

			////////////////////////////////////////////////////////////////////////////////
			// Each time a CVar is constructed we register it's type so we can recover the
			// original type after being cast about (esp. within the Trie data structure).
			template<class T>
						string CVarTypeString(T* t) {
				ostringstream oss;
				oss << typeid(*t).name();
				return oss.str();
			}

			// Each time a CVar is constructed we register it's type so we can recover the
			// original type after being cast about (esp. within the Trie data structure).
			template<class T>
						void StringToCVarValue(T* t, const string &sValue ) {
				istringstream iss(sValue);
				iss >> *t;
			}

			 //  These functions must be called to create a CVar, they return a reference to
			 //  the value saved.
			 //  A default parameter makes it possible to avoid this variable
			 //  from being saved.
			 //  eg. int& nGUIWidth = CVarUtils::CreateCVar<int>( "gui.Width", 10 );
			template<class T> T& CreateCVar(
								const string& s,
						 T val,
						 string sHelp = "No help available",
						 ostream& (*pSerialisationFuncPtr)( ostream &, T ) = NULL,
								istream& (* pDeserialisationFuncPtr)( istream &, T ) = NULL
								);

				template<class T> T& CreateGetCVar(
								const string& s,
							 T val,
							 string sHelp = "No help available",
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ) = NULL,
								istream& (* pDeserialisationFuncPtr)( istream &, T ) = NULL
								);

				template<class T> T& CreateUnsavedCVar(
								const string& s,
							 T val,
								const string& sHelp = "No help available",
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ) = NULL,
								istream& (* pDeserialisationFuncPtr)( istream &, T ) = NULL
								);

				template<class T> T& CreateGetUnsavedCVar(
								const string& s,
							 T val,
								const string& sHelp = "No help available",
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ) = NULL,
								istream& (* pDeserialisationFuncPtr)( istream &, T ) = NULL
								);

					//These functions must be called to attach a CVar to a variable.
					// Use these functions if you do not want to use references to CVars (as
					// created with \c CreateCVar() ).
					// \code
					//   int nGUIWidth = 100; // declare a variable as you usually do
					//   AttachCVar<int>( "gui.Wdith", &nGuiWidth ); // attach the variable

					//   nGUIWidth = 200;     // change the variable as you usually do
					// \endcode

				template<typename T>
				void AttachCVar(const string& s,
												T* ref,
												const string& sHelp = "No help available");

			////////////////////////////////////////////////////////////////////////////////
			/ These functions can be called to obtain a reference to a previously
				 created CVar.

				 The exception "CVarUtils::CVarNonExistant" will be thrown if the value
				 does not exist.
				 eg. int& nGUIWidth = CVarUtils::GetCVarRef<int>( "gui.Width" );
			 /
			template<class T> T& GetCVarRef( const char* s);
			template<class T> T& GetCVarRef(string s);

			////////////////////////////////////////////////////////////////////////////////
			/ These functions can be called to obtain the value of a previously
				 created CVar.

				 The exception "CVarUtils::CVarNonExistant" will be thrown if the value
				 does not exist.
				 eg. int nGUIWidth = CVarUtils::GetCVar<int>( "gui.Width" );
			 /
			template<class T> T GetCVar( const char* s);
			template<class T> T GetCVar(string s);

			////////////////////////////////////////////////////////////////////////////////
			/ This function can be called to determine if a particular CVar exists.
				/
			bool CVarExists(string s);

			////////////////////////////////////////////////////////////////////////////////
			/ These functions can be called to change the value of a previously
				 created CVar.

				 The exception "CVarUtils::CVarNonExistant" will be thrown if
				 the value does not exist.
				 eg. CVarUtils::SetCVar<int>( "gui.Width", 20 );
			 /
			template<class T> void SetCVar( const char* s, T val);
			template<class T> void SetCVar(string s, T val);

			////////////////////////////////////////////////////////////////////////////////
			/ These functions can be called to obtain the help value associated with a
				 previously created CVar.

				 The exception "CVarUtils::CVarNonExistant" will be thrown if
				 the value does not exist.
				 eg. printf( "GUI help: %s\n", CVarUtils::GetHelp( "gui.Width" ) );
			 /
			const string& GetHelp( const char* s);
			const string& GetHelp(string s);

			////////////////////////////////////////////////////////////////////////////////
			/// Changes the input/output types when calling Save and Load, options:
			/// - CVARS_XML_STREAM is the default
			/// - TXT_XML_STREAM is another option where the format is 'cvar_name = cvar_value' per line
			/// with commented lines starting by '#' or '//'
			inline void SetStreamType( const CVARS_STREAM_TYPE& stream_type );

			////////////////////////////////////////////////////////////////////////////////
			/ This function saves the CVars to "sFileName", it takes an optional
				 argument that is a vector of substrings indicating the CVars that should
				 or should not be saved.

				 If this vector is empty, all the CVars are saved.
				 If "not" is add to the list, all the following substrings will not be saved.
				 If "true" is used as the last argument, the saving will be verbose.
			 /
			inline bool Save( const string& sFileName,
												vector<string> vFilterSubstrings = vector < string>() );

				////////////////////////////////////////////////////////////////////////////////
				/ This function loads the CVars from "sFileName", it takes an optional
					 argument that is a vector of substrings indicating the CVars that should
					 or should not be loaded.

					 If this vector is empty, all the CVars are loaded.
					 If "not" is add to the list, all the following substrings will not be loaded.
					 If "true" is used as the last argument, the loading will be verbose.
				 /
				inline bool Load( const string& sFileName,
													vector<string> vFilterSubstrings = vector < string>() );

				/ Utilities for the indentation of XML output /
				inline string CVarSpc();
			inline void CVarIndent();
			inline void CVarUnIndent();
			inline void CVarResetSpc();
		}


		////////////////////////////////////////////////////////////////////////////////
		namespace CVarUtils {
			enum CVarException {
				CVarsNotInitialized,
				CVarNonExistant,
				CVarAlreadyCreated,
				ReservedName
			};
		}

		////////////////////////////////////////////////////////////////////////////////
		namespace CVarUtils {
			namespace CVarUtils {
				////////////////////////////////////////////////////////////////////////////////
				template<class T> T& CreateCVar(
								const string& s,
						 T val,
						 string sHelp,
						 ostream& (*pSerialisationFuncPtr)( ostream &, T ),
								istream& (* pDeserialisationFuncPtr)( istream &, T )
								)
				{
						Trie& trie = TrieInstance();

						if( trie.Exists( s ) ) {
								throw CVarAlreadyCreated;
						}
						if( string( s ) == "true" ||
										string( s ) == "false" ||
										string( s ) == "not") {
								throw ReservedName;
						}
		#ifdef DEBUG_CVAR
						printf( "Creating variable: %s.\n", s  );
		#endif
		CVarUtils::CVar<T>* pCVar = new CVarUtils::CVar<T>(
						s, val, sHelp, true, pSerialisationFuncPtr, pDeserialisationFuncPtr);
		trie.Insert( s, (void*) pCVar );
						return *(pCVar->VarData);
				}

				////////////////////////////////////////////////////////////////////////////////
				template<class T> T& CreateGetCVar(
								const string& s,
							 T val,
							 string sHelp,
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ),
								istream& (* pDeserialisationFuncPtr)( istream &, T )
								)
				{
						try {
								return CreateCVar(s, val, sHelp, pSerialisationFuncPtr, pDeserialisationFuncPtr );
						}
						catch( CVarUtils::CVarException e  ){
								switch( e ) {
								case CVarUtils::CVarAlreadyCreated:
										break;
								default:
										throw e;
										break;
								}
						}
						return CVarUtils::GetCVarRef<T>( s );
				}

				////////////////////////////////////////////////////////////////////////////////
				template<class T> T& CreateGetUnsavedCVar(
								const string& s,
							 T val,
								const string& sHelp,
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ),
								istream& (* pDeserialisationFuncPtr)( istream &, T )
								)
				{
						try {
								return CreateUnsavedCVar(s, val, sHelp, pSerialisationFuncPtr, pDeserialisationFuncPtr );
						}
						catch( CVarUtils::CVarException e  ){
								switch( e ) {
								case CVarUtils::CVarAlreadyCreated:
										break;
								default:
										throw e;
										break;
								}
						}
						return CVarUtils::GetCVarRef<T>( s );
				}

				////////////////////////////////////////////////////////////////////////////////
				template<class T> T& CreateUnsavedCVar(
								const string& s,
							 T val,
								const string& sHelp,
							 ostream& (*pSerialisationFuncPtr)( ostream &, T ),
								istream& (* pDeserialisationFuncPtr)( istream &, T )
								)
				{
						Trie& trie = TrieInstance();

						if( trie.Exists( s ) ) {
								throw CVarAlreadyCreated;
						}
						if( string( s ) == "true" ||
								string( s ) == "false" ||
								string( s ) == "not") {
								throw ReservedName;
						}
		#ifdef DEBUG_CVAR
						printf( "Creating variable: %s.\n", s  );
		#endif
		CVarUtils::CVar<T>* pCVar = new CVarUtils::CVar<T>(s, val, sHelp, false, pSerialisationFuncPtr, pDeserialisationFuncPtr);
		trie.Insert( s, (void*) pCVar );
						return *(pCVar->VarData);
				}

				////////////////////////////////////////////////////////////////////////////////
				template<typename T>
			void AttachCVar(const string& s,
												T* ref,
												const string& sHelp) {
			CreateCVar<CVarRef<T>>(s, CVarRef<T>(ref), sHelp, 0, 0);
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> T& GetCVarRef( const char* s) {
			Trie & trie = TrieInstance();

			if (!trie.Exists(s)) {
				throw CVarNonExistant;
			}
			return *(((CVar<T>*) trie.Find(s)->m_pNodeData)->VarData);
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> T& GetCVarRef(string s) {
			return CVarUtils::GetCVarRef<T>(s.c_str());
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> T GetCVar( const char* s) {
			return CVarUtils::GetCVarRef<T>(s);
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> T GetCVar(string s) {
			return CVarUtils::GetCVarRef<T>(s.c_str());
		}

		////////////////////////////////////////////////////////////////////////////////
		inline bool CVarExists(string s) {
			return TrieInstance().Exists(s);
		}

		////////////////////////////////////////////////////////////////////////////////
		inline string GetCVarString( string s) {
			Trie & trie = TrieInstance();

			if (!trie.Exists(s)) {
				throw CVarNonExistant;
			}
			return
					((CVar<int>*) trie.Find(s)->m_pNodeData)->GetValueAsString();
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> void SetCVar( const char* s, T val) {
			Trie & trie = TrieInstance();
			if (!trie.Exists(s)) {
				throw CVarNonExistant;
			}
			*(((CVar<T>*) trie.Find(s)->m_pNodeData)->VarData) = val;
		}

		////////////////////////////////////////////////////////////////////////////////
		template<class T> void SetCVar(string s, T val) {
			CVarUtils::SetCVar(s.c_str(), val);
		}

		////////////////////////////////////////////////////////////////////////////////
		inline const string& GetHelp( const char* s) {
			Trie & trie = TrieInstance();
			if (!trie.Exists(s)) {
				throw CVarNonExistant;
			}
			return ((CVar<int>*) trie.Find(s)->m_pNodeData)->GetHelp();
		}

		////////////////////////////////////////////////////////////////////////////////
		inline const string& GetHelp(string s) {
			return CVarUtils::GetHelp(s.c_str());
		}

		////////////////////////////////////////////////////////////////////////////////
		inline string GetValueAsString( void* cvar) {
			return ((CVar<int>*) cvar)->GetValueAsString();
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void SetValueFromString(void* cvar, const string &sValue ) {
			((CVar<int>*) cvar)->SetValueFromString(sValue);
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void PrintCVars(
								const char* sRowBeginTag = "",
								const char* sRowEndTag = "",
								const char* sCellBeginTag = "",
								const char* sCellEndTag = ""
						) {
			Trie & trie = TrieInstance();
			TrieNode* node = trie.FindSubStr("");
			if (!node) {
				return;
			}
			cout << "CVars:" << endl;
			// Retrieve suggestions (retrieve all leaves by traversing from current node)
			vector<TrieNode*> suggest = trie.CollectAllNodes(node);
			sort(suggest.begin(), suggest.end());
			//output suggestions
			unsigned int nLongestName = 0;
			unsigned int nLongestVal = 0;
			for (unsigned int ii = 0; ii < suggest.size(); ii++) {
				string sName = ((CVarUtils::CVar<int>*) suggest[ii]->m_pNodeData)->VarName;
				string sVal = CVarUtils::GetValueAsString(suggest[ii]->m_pNodeData);
				if (sName.length() > nLongestName) {
					nLongestName = sName.length();
				}
				if (sVal.length() > nLongestVal) {
					nLongestVal = sVal.length();
				}
			}

			if (suggest.size() > 1) {
				for (unsigned int ii = 0; ii < suggest.size(); ii++) {
					string sName = ((CVarUtils::CVar<int>*) suggest[ii]->m_pNodeData)->VarName;
					string sVal = CVarUtils::GetValueAsString(suggest[ii]->m_pNodeData);
					string sHelp = CVarUtils::GetHelp(sName);
					//                sName.resize( nLongestName, ' ' );
					//                sVal.resize( nLongestVal, ' ' );
					//                printf( "%-s: Default value = %-30s   %-50s\n", sName.c_str(), sVal.c_str(), sHelp.empty() ? "" : sHelp.c_str() );
					printf("%s%s%-s%s%s  %-30s %s%s  %-50s%s%s\n",
									sRowBeginTag,
									sCellBeginTag, sName.c_str(), sCellEndTag,
									sCellBeginTag, sVal.c_str(), sCellEndTag,
									sCellBeginTag, sHelp.empty() ? "" : sHelp.c_str(), sCellEndTag,
									sRowEndTag);
					printf("%s", sRowEndTag);
				}
			}
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void SetStreamType( const CVARS_STREAM_TYPE& stream_type ) {
			TrieInstance().SetStreamType(stream_type);
		}

		////////////////////////////////////////////////////////////////////////////////
		inline bool Save( const string& sFileName, vector<string> vAcceptedSubstrings) {
			ofstream sOut(sFileName.c_str());
			Trie & trie = TrieInstance();
			if (sOut.is_open()) {
				trie.SetVerbose(false);
				trie.SetAcceptedSubstrings(vAcceptedSubstrings);
				sOut << trie;
				sOut.close();
				return true;
			} else {
				//            cerr << "ERROR opening cvars file for saving." << endl;
				return false;
			}
		}

		////////////////////////////////////////////////////////////////////////////////
		inline bool Load( const string& sFileName, vector<string> vAcceptedSubstrings) {
			ifstream sIn(sFileName.c_str());
			Trie & trie = TrieInstance();
			if (sIn.is_open()) {
				trie.SetVerbose(false);
				trie.SetAcceptedSubstrings(vAcceptedSubstrings);
				sIn >> trie;
				sIn.close();
				return true;
			} else {
				//            cerr << "ERROR opening cvars file for loading." << endl;
				return false;
			}
		}

		////////////////////////////////////////////////////////////////////////////////
		inline string CVarSpc() {
			if (CVarUtils::GetCVar<int>("console.CVarIndent") <= 0) {
				return "";
			}
			return string(CVarUtils::GetCVar<int>("console.CVarIndent"), ' ' );
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void CVarIndent() {
			CVarUtils::GetCVarRef<int>("console.CVarIndent") += CVarUtils::GetCVar<int>("console.CVarIndentIncr");
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void CVarUnIndent() {
			CVarUtils::GetCVarRef<int>("console.CVarIndent") -= CVarUtils::GetCVar<int>("console.CVarIndentIncr");
		}

		////////////////////////////////////////////////////////////////////////////////
		inline void CVarResetSpc() {
			CVarUtils::SetCVar<int>("console.CVarIndent", 0);
		}

		////////////////////////////////////////////////////////////////////////////////



		////////////////////////////////////////////////////////////////////////////////
		/// This function parses console input for us -- useful function for glconsole, textconsole, etc
		bool ProcessCommand(
								const string& sCommand,   //< Input:
						string& sResult,          //< Output:
						bool bExecute = 1              //< Input:
						);

		////////////////////////////////////////////////////////////////////////////////
		bool ExecuteFunction(
								const string& sCommand,            //< Input:
						CVarUtils::CVar<ConsoleFunc>* cvar,     //< Input:
						string& sResult,                   //< Output:
						bool bExecute = 1                       //< Input:
						);

		////////////////////////////////////////////////////////////////////////////////
		bool IsConsoleFunc(
						TrieNode* node  //< Input:
						);

		////////////////////////////////////////////////////////////////////////////////
		bool IsConsoleFunc(
								const string sCmd      //< Input
						);

		////////////////////////////////////////////////////////////////////////////////
		/// If 'sCommand' is
		/// - part of an existing unique variable, it will be completed and 'sCommand'
		///   will be the complete variable, sResult will be empty
		/// - part of multiple possible variables, it will be completed up to the common
		///   factor and possible solutions will be put in sResult (TO IMPROVE: differentiate type of
		///   results: command, variable, ...)
		/// - a full variable: 'sCommand' will be completed with '= variable_value' and
		///   sResult will also contain the variable name and its value
		bool TabComplete( const unsigned int nMaxNumCharactersPerLine, ///< Input: use for formatting the sResult when it returns the list of possible completions
													string& sCommand,
											vector<string>& vResult );


		}

		#endif
			}
		}
		*/
	}
}
