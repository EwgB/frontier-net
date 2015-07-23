/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: Trie.cpp 162 2010-02-15 19:11:05Z gsibley $
 */

namespace CVars {
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	// Trie data structure implementation.
	class Trie {
		private enum CVarsStreamType { Xml, Text }

		public int VerboseCVarNamePaddingWidth { get; set; }

		private TrieNode Root { get; set; }
		private List<string> AcceptedSubstrings { get; set; }
		private List<string> NotAcceptedSubstrings { get; set; }
		private List<string> CVarNames { get; set; } // Keep a list of CVar names
		private bool Verbose { get; set; }
		private CVarsStreamType StreamType { get; set; }

		public Trie() {
			Root = null;
			Verbose = false;
			StreamType = CVarsStreamType.Xml;
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

			foreach (var c in s) {
				traverseNode = traverseNode.TraverseFind(c);
				if (null == traverseNode) {
					return null;
				}
			}

			// Look for a leaf node here and return it if no leaf node just return this node.
			var child = traverseNode.Children.FirstOrDefault(c => c.NodeType == TrieNodeType.Leaf);
			if (child != null) {
				return child;
			}
			return traverseNode;
		}

		// Chris, please comment this guy for me.  GTS
		void SetAcceptedSubstrings(IList<string> filterSubstrings) {
			AcceptedSubstrings.Clear();
			NotAcceptedSubstrings.Clear();

			// Check if verbose should be set
			if (filterSubstrings.Any()) {
				if (filterSubstrings.Last() == "true") {
					Verbose = true;
					filterSubstrings.RemoveAt(filterSubstrings.Count - 1);
				} else if (filterSubstrings.Last() == "false") {
					Verbose = false;
					filterSubstrings.RemoveAt(filterSubstrings.Count - 1);
				}
			}

			// Split the list between acceptable and not acceptable substrings.
			int accIndex = filterSubstrings.IndexOf(filterSubstrings.First(s => s.Contains("not")));
			if (accIndex >= 0) {
				AcceptedSubstrings.AddRange(filterSubstrings.Where((v, i) => i < accIndex));
				NotAcceptedSubstrings.AddRange(filterSubstrings.Where((v, i) => i > accIndex));
			}
		}

		bool IsNameAcceptable(string varName) {
			return
				// Both lists are empty
				(!(AcceptedSubstrings.Any() || NotAcceptedSubstrings.Any())) ||
				// AcceptedSubstrings is empty or varName starts with a string from AcceptedSubstrings
				((false == AcceptedSubstrings.Any()) || AcceptedSubstrings.Any(s => varName.StartsWith(s)))
				// NotAcceptedSubstrings does not contain a string with which varName starts
				&& (false == NotAcceptedSubstrings.Any(s => varName.StartsWith(s)));
		}

		// Does an in order traversal starting at node and printing all leaves to a list
		List<string> CollectAllNames(TrieNode node) {
			var res = new List<string>();
			node.PrintToVector(res);
			return res;
		}

		// Does an in order traversal starting at node and printing all leaves to a list
		List<TrieNode> CollectAllNodes(TrieNode node) {
			var res = new List<TrieNode>();
			node.PrintNodeToVector(res);
			return res;
		}

		public override string ToString() {
			var sb = new StringBuilder();
			List<TrieNode> nodes = CollectAllNodes(Root);
			foreach (var node in nodes) {
				string sVal = ((CVar<int>) node.NodeData).GetValueAsString();

				if (sVal.Any()) {
					string cVarName = ((CVar<int>) node.NodeData).sVarName;
					if (!IsNameAcceptable(cVarName)) {
						if (Verbose) {
							//Log("NOT saving %s (not in acceptable name list).\n", cVarName);
						}
						continue;
					}
					if (!((CVar<int>) node.NodeData).bSerialise) {
						if (Verbose) {
							//Log("NOT saving %s (set as not savable at construction time).\n", cVarName);
						}
						continue;
					}
					if (Verbose) {
						//Log("Saving \"%-s\" with value \"%s\".\n", VerboseCVarNamePaddingWidth, cVarName, sVal);
					}
					sb.AppendLine(cVarName + " = " + sVal);
				}
			}
			return sb.ToString();
		}

		public string TrieToXML() {
			var sb = new StringBuilder();
			List<TrieNode> nodes = CollectAllNodes(Root);

			sb.AppendLine(CVarSpc() << "<cvars>");
			foreach (var node in nodes) {
				string sVal = ((CVar<int>) node.NodeData).GetValueAsString();

				if (sVal.Any()) {
					string sCVarName = ((CVar<int>) node.NodeData).sVarName;
					if (!IsNameAcceptable(sCVarName)) {
						if (Verbose) {
							//Log("NOT saving %s (not in acceptable name list).\n", sCVarName);
						}
						continue;
					}
					if (!((CVar<int>) node.NodeData).bSerialise) {
						if (Verbose) {
							//Log("NOT saving %s (set as not savable at construction time).\n", sCVarName);
						}
						continue;
					}
					if (Verbose) {
						//Log("Saving \"%-s\" with value \"%s\".\n", rTrie.VerboseCVarNamePaddingWidth, sCVarName, sVal);
					}
					CVarIndent();
					sb.Append(CVarSpc() + "<" + sCVarName + ">  ");
					CVarIndent();
					sb.Append(sVal);
					CVarUnIndent();
					sb.AppendLine(CVarSpc() + "</" + sCVarName + ">");
					CVarUnIndent();
				}
			}
			sb.AppendLine(CVarSpc() + "</cvars>");

			return sb.ToString();
		}

		private static bool GetNotCommentLine(Stream stream, out string lineNoComment) {
			string line;
			lineNoComment = "";
			if (!stream.CanRead) {
				return false;
			}

			using (var sr = new StreamReader(stream)) {
				while (sr.Peek() >= 0) {
					line = sr.ReadLine().TrimStart();
					if (!(line.StartsWith("#") || line.StartsWith("/"))) {
						lineNoComment = line;
						return true;
					}
				}
			}
			return false;
		}

		private static bool GetNameVal(string line, out string name, out string val) {
			name = "";
			val = "";
			int pos = line.IndexOf("=");
			if (pos < 0 || pos == line.Length - 1) { return false; }
			val = line.Substring(pos + 1).Trim();
			name = line.Substring(0, pos).Trim();
			return true;
		}

		// TODO: convert stream operator
		public string operator_out() {
			switch (StreamType) {
				case CVarsStreamType.Xml:
					return TrieToXML();
				case CVarsStreamType.Text:
					return ToString();
				default:
					// Log.Error("ERROR: unknown stream type");
					break;
			}
			return "";
		}

		// TODO: convert stream operator
		//istream &operator_in( istream &stream, Trie &rTrie )
		//{
		//  switch( rTrie.GetStreamType() ) {
		//  case CVARS_STREAM_TYPE.Xml:
		//    return XMLToTrie(stream, rTrie );
		//    break;
		//  case CVARS_STREAM_TYPE.Text:
		//    return TXTToTrie(stream, rTrie );
		//    break;
		//  default:
		//    cerr << "ERROR: unknown stream type" << endl;
		//    }
		//  return stream;
		//}

		// TODO: convert read from xml
		//static istream &XMLToTrie(istream &stream, Trie &rTrie ) {
		//	TiXmlDocument doc;
		//	stream >> doc;

		//	TiXmlNode pCVarsNode = doc.FirstChild("cvars");

		//	if (pCVarsNode == NULL) {
		//		cerr << "ERROR: Could not find <cvars> node." << endl;
		//		return stream;
		//	}

		//	for (TiXmlNode pNode = pCVarsNode.FirstChild();
		//			 pNode != NULL;
		//			 pNode = pNode.NextSibling()) {
		//		string sCVarName(pNode.Value());

		//		if (!rTrie.Exists(sCVarName)) {
		//			if (rTrie.IsVerbose()) {
		//				printf("NOT loading %s (not in Trie).\n", sCVarName);
		//			}
		//			continue;
		//		}

		//		if (!rTrie.IsNameAcceptable(sCVarName)) {
		//			if (rTrie.IsVerbose()) {
		//				printf("NOT loading %s (not in acceptable name list).\n", sCVarName);
		//			}
		//			continue;
		//		}

		//		CVar<int> pCVar = (CVar<int>) rTrie.Find(sCVarName).NodeData;
		//		TiXmlNode pChild = pNode.FirstChild();

		//		if (pCVar != NULL && pChild != NULL) {
		//			string sCVarValue;
		//			sCVarValue << pChild;
		//			pCVar.SetValueFromString(sCVarValue);

		//			if (rTrie.IsVerbose()) {
		//				printf("Loading \"%-s\" with value \"%s... \".\n", rTrie.VerboseCVarNamePaddingWidth,
		//								sCVarName, sCVarValue.substr(0, 40));
		//			}
		//		} else {
		//			cerr << "WARNING: found a cvar in file with no value (name: " << sCVarName << ").\n" << endl;
		//		}
		//	}
		//	return stream;
		//}

		// TODO: convert read from txt
		//static istream &TXTToTrie(istream &stream, Trie &rTrie ) {
		//	string sLine, sCVarName, sCVarValue;
		//	while (get_not_comment_line(stream, sLine)) {
		//		if (get_name_val(sLine, sCVarName, sCVarValue)) {
		//			if (!rTrie.Exists(sCVarName)) {
		//				if (rTrie.IsVerbose()) {
		//					printf("NOT loading %s (not in Trie).\n", sCVarName);
		//				}
		//				continue;
		//			}
		//			if (!rTrie.IsNameAcceptable(sCVarName)) {
		//				if (rTrie.IsVerbose()) {
		//					printf("NOT loading %s (not in acceptable name list).\n", sCVarName);
		//				}
		//				continue;
		//			}

		//			CVar<int> pCVar = (CVar<int>) rTrie.Find(sCVarName).NodeData;

		//			if (pCVar != NULL) {
		//				pCVar.SetValueFromString(sCVarValue);
		//				if (rTrie.IsVerbose()) {
		//					printf("Loading \"%-s\" with value \"%s... \".\n", rTrie.VerboseCVarNamePaddingWidth,
		//						sCVarName, sCVarValue.substr(0, 40));
		//				}
		//			} else {
		//				cerr << "WARNING: found a cvar in file with no value (name: " << sCVarName << ").\n" << endl;
		//			}
		//		}
		//	}
		//	return stream;
		//}
	}
}