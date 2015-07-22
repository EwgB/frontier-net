/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: Trie.cpp 162 2010-02-15 19:11:05Z gsibley $
 */

namespace CVars {
	using System.Collections.Generic;
	using System.Linq;

	// Trie data structure implementation.
	class Trie {
		public int VerboseCVarNamePaddingWidth { get; set; }

		private TrieNode Root { get; set; }
		private IList<string> AcceptedSubstrings { get; set; }
		private IList<string> NotAcceptedSubstrings { get; set; }
		private IList<string> CVarNames { get; set; } // Keep a list of CVar names
		private bool Verbose { get; set; }
		private CVARS_STREAM_TYPE StreamType { get; set; }

		public Trie() {
			Root = null;
			Verbose = false;
			StreamType = CVARS_XML_STREAM;
		}

		public void Init() {
			if (null != Root) {
				Root = new TrieNode(TrieNodeType.Root);

				string varName = "console.VerbosePaddingWidth";
				CVar<int> CVar1 = new CVar<int>(varName, 30);
				VerboseCVarNamePaddingWidth = CVar1.VarData;
				Insert(varName, CVar1);

				varName = "console.CVarIndent";
				CVar<int> CVar2 = new CVar<int>(varName, 0);
				VerboseCVarNamePaddingWidth = CVar2.VarData;
				Insert(varName, CVar2);

				varName = "console.CVarIndentIncr";
				CVar<int> CVar3 = new CVar<int>(varName, 4);
				VerboseCVarNamePaddingWidth = CVar3.VarData;
				Insert(varName, CVar3);
			}
		}

		void Insert(string s, object data) {
			if (Root == null) {
				//Log.Error( "ERROR in Insert, root == NULL!!!!!\n" );
				return;
			}

			CVarNames.Add(s);

			TrieNode traverseNode = Root;
			foreach (var c in s) {
				traverseNode = traverseNode.TraverseInsert(c);
			}

			//add leaf node
			TrieNode newNode = new TrieNode(s);
			newNode.NodeData = data;
			traverseNode.Children.Add(newNode); //create leaf node at end of chain
		}

		public TrieNode Find(string s) {
			TrieNode node = FindSubStr(s);
			if (node != null && node.NodeType == TrieNodeType.Leaf) {
				return node;
			}
			return null;
		}

		public object FindData(string s) {
			return Find(s).NodeData;
		}

		public bool Exists(string s) {
			return Find(s) != null;
		}

		// Finds all the CVarNames that contain s as a substring.
		IEnumerable<string> FindListSubStr(string s) {
			return CVarNames.Where(n => n.Contains(s));
		}

		// Finds s in the tree and returns the node (may not be a leaf), returns null
		// otherwise.
		TrieNode FindSubStr(string s) {
			if (Root == null) {
				//Log.Error("ERROR in FindSubStr, root == NULL!!!!!\n");
				return null;
			}

			if (s.Length == 0)
				return Root;

			TrieNode traverseNode = Root;

			for (unsigned int i = 0; i < s.length(); i++) {
				traverseNode = traverseNode.TraverseFind(s[i]);
				if (traverseNode) {
					continue;
				} else {
					return NULL;
				}
			}

			// Look for a leaf node here and return it if no leaf node just return this
			// node.
			list<TrieNode>::iterator it;
			for (it = traverseNode.children.begin(); it != traverseNode.children.end(); it++) {
				//found child
				if ((it).nNodeType == TRIE_LEAF) {
					return (it);
				}
			}

			return traverseNode;
		}


// Chris, please comment this guy for me.  GTS
void SetAcceptedSubstrings( vector< string > vFilterSubstrings )
{
    vAcceptedSubstrings.clear();
    vNotAcceptedSubstrings.clear();

    // Check if verbose should be set
    if( vFilterSubstrings.size() > 0 ) {
        if( vFilterSubstrings[ vFilterSubstrings.size()-1 ] == "true" ) {
            bVerbose = true;
            vFilterSubstrings.pop_back();
        }
        else if( vFilterSubstrings[ vFilterSubstrings.size()-1 ] == "false" ) {
            bVerbose = false;
            vFilterSubstrings.pop_back();
        }
    }

    // Split the list between acceptable and not acceptable substrings.
    int nAccIndex = 0;
    for( nAccIndex=0; nAccIndex<int(vFilterSubstrings.size()); nAccIndex++ ) {
        if( vFilterSubstrings[nAccIndex].find( "not" ) != string::npos ) {
            nAccIndex++;
            break;
        }
        vAcceptedSubstrings.push_back( vFilterSubstrings[nAccIndex] );
    }
    for( int nNotAccIndex=nAccIndex; nNotAccIndex<int(vFilterSubstrings.size()); nNotAccIndex++ ) {
        vNotAcceptedSubstrings.push_back( vFilterSubstrings[nNotAccIndex] );
    }
}


bool IsNameAcceptable(  string sVarName )
{
    if( vAcceptedSubstrings.size() == 0 &&
        vNotAcceptedSubstrings.size() == 0 ) {
        return true;
    }
    bool bAcceptable;
    if( vAcceptedSubstrings.size() == 0 ) {
        bAcceptable = true;
    }
    else {
        bAcceptable = false;
    }

    for( size_t i=0; i<vAcceptedSubstrings.size(); i++ ) {
        if( sVarName.find( vAcceptedSubstrings[i] ) == 0 ) {
            bAcceptable = true;
            break;
        }
    }
    for( size_t i=0; i<vNotAcceptedSubstrings.size(); i++ ) {
        if( sVarName.find( vNotAcceptedSubstrings[i] ) == 0 ) {
            bAcceptable = false;
            break;
        }
    }
    return bAcceptable;
}


bool IsVerbose()
{
    return bVerbose;
}


void SetVerbose( bool bVerbose )
{
    bVerbose = bVerbose;
}


// Does an in order traversal starting at node and printing all leaves to a list
vector<string> CollectAllNames( TrieNode node )
{
    vector<string> res;
    node.PrintToVector( res );

    return res;
}


// Does an in order traversal starting at node and printing all leaves to a list
vector<TrieNode> CollectAllNodes( TrieNode node )
{
    vector<TrieNode> res;
    node.PrintNodeToVector( res );
    return res;
}


static ostream &TrieToTXT( ostream &stream, Trie &rTrie )
{
    vector<TrieNode> vNodes = rTrie.CollectAllNodes( rTrie.GetRoot() );
    for( size_t ii = 0; ii < vNodes.size(); ii++ ){
        string sVal = ((CVar<int>)vNodes[ii].NodeData).GetValueAsString();

        if( !sVal.empty() ) {
            string sCVarName = ((CVar<int>)vNodes[ii].NodeData).sVarName;
            if( !rTrie.IsNameAcceptable( sCVarName ) ) {
                if( rTrie.IsVerbose() ) {
                    printf( "NOT saving %s (not in acceptable name list).\n", sCVarName.c_str() );
                }
                continue;
            }
            if( !((CVar<int>)vNodes[ii].NodeData).bSerialise ) {
                if( rTrie.IsVerbose() ) {
                    printf( "NOT saving %s (set as not savable at construction time).\n", sCVarName.c_str() );
                }
                continue;
            }
            if( rTrie.IsVerbose() ) {
                printf( "Saving \"%-s\" with value \"%s\".\n", rTrie.VerboseCVarNamePaddingWidth,
                        sCVarName.c_str(), sVal.c_str() );
            }
            stream << sCVarName << " = " << sVal << endl;
        }
    }
	return stream;
}


static ostream &TrieToXML( ostream &stream, Trie &rTrie )
{
    vector<TrieNode> vNodes = rTrie.CollectAllNodes( rTrie.GetRoot() );
    stream << CVarSpc() << "<cvars>" << endl;
    for( size_t ii = 0; ii < vNodes.size(); ii++ ){
        string sVal = ((CVar<int>)vNodes[ii].NodeData).GetValueAsString();

        if( !sVal.empty() ) {
            string sCVarName = ((CVar<int>)vNodes[ii].NodeData).sVarName;
            if( !rTrie.IsNameAcceptable( sCVarName ) ) {
                if( rTrie.IsVerbose() ) {
                    printf( "NOT saving %s (not in acceptable name list).\n", sCVarName.c_str() );
                }
                continue;
            }
            if( !((CVar<int>)vNodes[ii].NodeData).bSerialise ) {
                if( rTrie.IsVerbose() ) {
                    printf( "NOT saving %s (set as not savable at construction time).\n", sCVarName.c_str() );
                }
                continue;
            }
            if( rTrie.IsVerbose() ) {
                printf( "Saving \"%-s\" with value \"%s\".\n", rTrie.VerboseCVarNamePaddingWidth,
                        sCVarName.c_str(), sVal.c_str() );
            }
            CVarIndent();
            stream << CVarSpc() << "<" << sCVarName << ">  ";
            CVarIndent();
            stream << sVal;
            CVarUnIndent();
            stream << CVarSpc() << "</" << sCVarName << ">" << endl;
            CVarUnIndent();
        }
    }
    stream << CVarSpc() << "</cvars>" << endl;

    return stream;
}


ostream &operator<<( ostream &stream, Trie &rTrie )
{
  switch( rTrie.GetStreamType() ) {
  case CVARS_XML_STREAM:
    return TrieToXML( stream, rTrie );
    break;
  case CVARS_TXT_STREAM:
    return TrieToTXT( stream, rTrie );
    break;
  default:
    cerr << "ERROR: unknown stream type" << endl;
    }
  return stream;
}


static istream &XMLToTrie( istream &stream, Trie &rTrie )
{
    TiXmlDocument doc;
    stream >> doc;

    TiXmlNode pCVarsNode = doc.FirstChild( "cvars" );

    if( pCVarsNode == NULL ) {
        cerr <<  "ERROR: Could not find <cvars> node." << endl;
        return stream;
    }

    for( TiXmlNode pNode = pCVarsNode.FirstChild();
         pNode != NULL;
         pNode = pNode.NextSibling() ) {
        string sCVarName( pNode.Value() );

        if( !rTrie.Exists( sCVarName ) ) {
            if( rTrie.IsVerbose() ) {
                printf( "NOT loading %s (not in Trie).\n", sCVarName.c_str() );
            }
            continue;
        }

        if( !rTrie.IsNameAcceptable( sCVarName ) ) {
            if( rTrie.IsVerbose() ) {
                printf( "NOT loading %s (not in acceptable name list).\n", sCVarName.c_str() );
            }
            continue;
        }

        CVar<int> pCVar = (CVar<int>)rTrie.Find( sCVarName ).NodeData;
        TiXmlNode pChild = pNode.FirstChild();

        if( pCVar != NULL && pChild != NULL ) {
            string sCVarValue;
            sCVarValue << pChild;
            pCVar.SetValueFromString( sCVarValue );

            if( rTrie.IsVerbose() ) {
                printf( "Loading \"%-s\" with value \"%s... \".\n", rTrie.VerboseCVarNamePaddingWidth,
                        sCVarName.c_str(), sCVarValue.substr(0,40).c_str() );
            }
        }
        else {
            cerr << "WARNING: found a cvar in file with no value (name: " << sCVarName << ").\n" << endl;
        }
    }
    return stream;
}


static string remove_spaces( string str )
{
  str.erase( str.find_last_not_of( ' ' ) + 1 );
  str.erase( 0, str.find_first_not_of( ' ' ) );
  return str;
}


static bool get_not_comment_line( istream& iStream, string sLineNoComment )
{
  string sLine;
  if( !iStream.good() ) { return false; }
  while( !iStream.eof() ) {
    getline( iStream, sLine );
    sLine.erase( 0, sLine.find_first_not_of( ' ' ) );
    if( sLine.empty() ) {
      continue;
    }
    else if( sLine[0] != '#' && sLine[0] != '/' ) {
      sLineNoComment = sLine;
      return true;
    }
  }
  return false;
}


static bool get_name_val(  string sLine,
			  string sName,
			  string sVal )
{
  size_t sEq = sLine.find( "=" );
  if( sEq == string::npos || sEq == sLine.length()-1 ) { return false; }
  sVal = sLine;
  sVal.erase( 0, sEq+1 );
  sVal = remove_spaces( sVal );
  sName = sLine;
  sName.erase( sEq );
  sName = remove_spaces( sName );
  return true;
}


static istream &TXTToTrie( istream &stream, Trie &rTrie )
{
  string sLine, sCVarName, sCVarValue;
  while( get_not_comment_line( stream, sLine ) )
    {
      if( get_name_val( sLine, sCVarName, sCVarValue ) )
	{
	  if( !rTrie.Exists( sCVarName ) ) {
            if( rTrie.IsVerbose() ) {
	      printf( "NOT loading %s (not in Trie).\n", sCVarName.c_str() );
            }
            continue;
	  }
	  if( !rTrie.IsNameAcceptable( sCVarName ) ) {
            if( rTrie.IsVerbose() ) {
	      printf( "NOT loading %s (not in acceptable name list).\n", sCVarName.c_str() );
            }
            continue;
	  }

	  CVar<int> pCVar = (CVar<int>)rTrie.Find( sCVarName ).NodeData;

	  if( pCVar != NULL ) {
            pCVar.SetValueFromString( sCVarValue );
            if( rTrie.IsVerbose() ) {
	      printf( "Loading \"%-s\" with value \"%s... \".\n", rTrie.VerboseCVarNamePaddingWidth,
		      sCVarName.c_str(), sCVarValue.substr(0,40).c_str() );
            }
	  }
	  else {
            cerr << "WARNING: found a cvar in file with no value (name: " << sCVarName << ").\n" << endl;
	  }
	}
    }
    return stream;
}


istream &operator>>( istream &stream, Trie &rTrie )
{
  switch( rTrie.GetStreamType() ) {
  case CVARS_XML_STREAM:
    return XMLToTrie( stream, rTrie );
    break;
  case CVARS_TXT_STREAM:
    return TXTToTrie( stream, rTrie );
    break;
  default:
    cerr << "ERROR: unknown stream type" << endl;
    }
  return stream;
}
	}
}
