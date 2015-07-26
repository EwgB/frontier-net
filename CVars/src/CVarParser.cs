/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: CVarParse.cpp 189 2011-07-26 14:29:48Z effer $
*/

namespace CVars {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CVarTypes;

	internal static class CVarParser {
		internal static bool ProcessCommand(string command, ref string result, bool execute) {
			Trie trie = TrieInstance();
			bool success = true;

			// remove leading and trailing spaces
			command = command.Trim();

			// Simply print value if the command is just a variable
			TrieNode<CVar> node;
			if ((node = trie.Find(command)) != null) {
				//execute function if this is a function cvar
				if (node.NodeData is ConsoleFunc) {
					success &= ExecuteFunction(command, (CVar<ConsoleFunc>) node.NodeData, result, execute);
				} else {
					//print value associated with this cvar
					result = node.NodeData.GetValueAsString();
				}
			} else {
				//see if it is an assignment or a function execution (with arguments)
				int pos;
				if ((pos = command.IndexOf('=')) >= 0) {
					string func = command.Substring(0, pos - 1).TrimEnd();
					string value = command.Substring(pos + 1).TrimStart();
					if (value.Any()) {
						if ((node = trie.Find(func)) != null) {
							if (execute) {
								node.NodeData.SetValueFromString(value);
							}
							result = node.NodeData.GetValueAsString();
						} else {
							result = func + ": variable not found";
							success = false;
						}
					} else {
						if (execute) {
							result = func + ": command not found";
						}
						success = false;
					}
				} else if ((pos = command.IndexOf(' ')) >= 0) {
					//check if this is a function
					string function = command.Substring(0, pos - 1);
					//check if this is a valid function name
					if ((node = trie.Find(function)) && (node.NodeData is ConsoleFunc)) {
						success &= ExecuteFunction(command, (CVar<ConsoleFunc>) node.NodeData, result, execute);
					} else {
						if (execute) {
							result = function + ": function not found";
						}
						success = false;
					}
				} else if (command.Any()) {
					if (execute) {
						result = command + ": command not found";
					}
					success = false;
				}
			}
			if (result == "" && success == false) {
				result = command + ": command not found";
			}
			return success;
		}

		///<summary>
		/// Parses the argument list and calls the function object associated with the
		/// provided variable.
		///</summary>
		private static bool ExecuteFunction(string command, CVar<ConsoleFunc> cvar, bool execute) {
			bool success = true;

			//extract arguments string
			int pos = command.IndexOf(' ');
			if (pos >= 0) {
				string args = command.Substring(pos + 1);

				//parse arguments into a list of strings
				string[] argslist;
				if (args.Length > 0) {
					argslist = args.Split(' ');
				} else {
					argslist = new string[0];
				}

				if (execute) {
					ConsoleFunc func = cvar.VarData;
					success = func(argslist);
				}
			}
			return success;
		}

		private static bool IsConsoleFunc(TrieNode<CVar> node) {
			return (node != null) && (node.NodeData is ConsoleFunc);
		}

		private static bool IsConsoleFunc(string cmd) {
			TrieNode<CVar> node = TrieInstance().Find(cmd);
			return IsConsoleFunc(node);
		}

		/// Utility function.
		private static string FindLevel(string sString, int minRecurLevel) {
			int level = 0;
			int index = sString.Length;
			for (int i = 0; i < sString.Length; i++) {
				if (sString[i] == '.') {
					level++;
				}
				if (level == minRecurLevel) {
					index = i + 1;
				}
			}
			return sString.Substring(0, index);
		}

		private static int FindRecursionLevel(string command) {
			return command.Where(c => c == '.').Count();
		}

		/// Return whether first element is greater than the second.
		private static bool StringIndexPairGreater
				(Tuple<string, int> e1, Tuple<string, int> e2 ) {
			return e1.Item1 < e2.Item1;
		}

		private static bool TabComplete(uint maxNumCharactersPerLine, string command, List<string> result) {
			Trie trie = TrieInstance();

			command = command.Trim();
			TrieNode<CVar> node = trie.FindSubStr(command);
			if (node == null) {
				string commandStripEq = command.Substring(0, command.LastIndexOf('=')).Trim();
				node = trie.FindSubStr(commandStripEq);
				if (node != null) {
					command = commandStripEq;
				}
			}

			if (node == null) {
				return false;
			} else if (node.NodeType == TrieNodeType.Leaf || !node.Children.Any()) {
				node = trie.Find(command);
				if (!IsConsoleFunc(node)) {
					command += " = " + node.NodeData.ToString();
					result.Add(command);
				}
			} else {
				// Retrieve suggestions (retrieve all leaves by traversing from current node)
				List<TrieNode<CVar>> suggest = trie.CollectAllNodes(node);
				//output suggestions
				if (suggest.Count == 1) {
					// Is this what the user wants? Clear the left bit...
					command = suggest[0].NodeData.VarName;
				} else if (suggest.Count > 1) {
					var suggestNameIndexFull = new Dictionary<string, int>();
					// Build list of names with index from suggest
					// Find lowest recursion level
					int minRecurLevel = 100000;
					for (int i = 0; i < suggest.Count; i++) {
						string sName = suggest[i].NodeData.VarName;
						suggestNameIndexFull.Add(sName, i);
						if (FindRecursionLevel(sName) < minRecurLevel) {
							minRecurLevel = FindRecursionLevel(sName);
						}
					}

					// We need to know if there are many different roots for a given recursion
					var roots = new SortedSet<string>();
					string curLevelString;
          foreach (string curString in suggestNameIndexFull.Keys) {
						if (FindRecursionLevel(curString) == minRecurLevel) {
							curLevelString = FindLevel(curString, minRecurLevel - 1);
							roots.Add(curLevelString);
						}
					}
					if (roots.Count > 1) {
						minRecurLevel--;
					}

					// Remove suggestions at a higher level of recursion
					var suggestNameIndexSet = new Dictionary<string, int>();
					curLevelString = "";
					foreach (var pair in suggestNameIndexFull) {
						string curString = pair.Key;
						int curLevelNum = FindRecursionLevel(curString);
						if (curLevelString.Length == 0) {
							if (curLevelNum == minRecurLevel) {
								curLevelString = "";
								suggestNameIndexSet.Add(curString, pair.Value);
							} else {
								// Add new substring at given level
								curLevelString = FindLevel(curString, minRecurLevel);
								suggestNameIndexSet.Add(curLevelString, pair.Value);
							}
						} else {
							if (!curString.Contains(curLevelString)) {
								// Add new substring at given level
								curLevelString = FindLevel(curString, minRecurLevel);
								suggestNameIndexSet.Add(curLevelString, pair.Value);
							}
						}
					}

					// Get all commands and function separately
					// Print out all suggestions to the console
					int longest = suggestNameIndexSet.Select(p => p.Key.Length).Max() + 3;

					// add command lines
					string commands = ""; //collect each type separately
					var cmdlines = new List<string>();
					foreach (var pair in suggestNameIndexSet) {
						string tmp = pair.Key;
						tmp += new string(' ', longest);
						if ((commands + tmp).Length > maxNumCharactersPerLine) {
							cmdlines.Add(commands);
							commands = "";
						}
						if (!IsConsoleFunc(suggest[pair.Value])) {
							commands += tmp;
						}
					}
					if (commands.Length > 0) {
						cmdlines.Add(commands);
					}

					// add function lines
					string functions = "";
					var funclines = new List<string>();
					foreach (var pair in suggestNameIndexSet) {
						string tmp = pair.Key;
						tmp += new string(' ', longest);
						if ((functions + tmp).Length > maxNumCharactersPerLine) {
							funclines.Add(functions);
							functions = "";
						}
						if (!IsConsoleFunc(suggest[pair.Value])) {
							functions += tmp;
						}
					}
					if (functions.Length > 0) {
						funclines.Add(functions);
					}

					// enter the results
					if (cmdlines.Count + funclines.Count > 0) {
						//EnterLogLine( " ", LINEPROP_LOG );
						result.Add(" ");
					}
					for (int ii = 0; ii < cmdlines.Count; ii++) {
						//EnterLogLine( cmdlines[ii].c_str(), LINEPROP_LOG );
						result.Add(cmdlines[ii]);
					}
					for (int ii = 0; ii < funclines.Count; ii++) {
						//EnterLogLine( funclines[ii].c_str(), LINEPROP_FUNCTION );
						result.Add(funclines[ii]);
					}

					// Do partial completion - look for paths with one child down the trie
					int c = command.Length;
					while (node.Children.Count == 1) {
						node = node.Children.First();
						c++;
					}
					command = suggestNameIndexSet.Keys.Max().Substring(0, c);
				}
			}
			return true;
		}
	}
}