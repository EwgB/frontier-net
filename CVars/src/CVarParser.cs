namespace CVars {
	using System.Linq;

	internal class CVarParser {
		internal bool ProcessCommand(string command, ref string result, bool execute) {
			Trie trie = TrieInstance();
			bool success = true;

			// remove leading and trailing spaces
			command = command.Trim();

			// Simply print value if the command is just a variable
			TrieNode<CVar> node;
			if ((node = trie.Find(command)) != null) {
				//execute function if this is a function cvar
				if (IsConsoleFunc(node)) {
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
				} else if ((pos = command.IndexOf(" ")) >= 0) {
				//check if this is a function
          string function = command.Substring(0, pos - 1);
					//check if this is a valid function name
					if ((node = trie.Find(function)) && IsConsoleFunc(node)) {
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
	}
}