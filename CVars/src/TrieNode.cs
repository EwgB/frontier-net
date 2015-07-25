/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: TrieNode.cpp 162 2010-02-15 19:11:05Z gsibley $
*/

namespace CVars {
	using System.Collections.Generic;
	using System.Linq;

	internal class TrieNode<T> where T : class {
		internal T NodeData { get; set; }
		internal List<TrieNode<T>> Children { get; }
		internal TrieNodeType NodeType { get; }

		private string LeafText { get; set; }
		private char NodeChar { get; set; }

		internal TrieNode() {
			NodeData = null;
			NodeType = TrieNodeType.Leaf;
			Children = new List<TrieNode<T>>();
		}

		internal TrieNode(TrieNodeType nodeType) : this() {
			NodeType = nodeType;
		}

		internal TrieNode(string leafText) : this() {
			LeafText = leafText;
		}

		internal TrieNode(char nodeChar) : this() {
			NodeType = TrieNodeType.Node;
		}

		///<summary>
		/// Go through this node and see if this char is a branch, if so, simply return
		/// the corresponding child, otherwise create a node and return its child.
		///</summary>
		internal TrieNode<T> TraverseInsert(char addchar) {
			var child = Children.FirstOrDefault(c => (c.NodeType == TrieNodeType.Node) && (c.NodeChar == addchar));

			if (child != default(TrieNode<T>)) {
				return child;
			} else {
				var newNode = new TrieNode<T>(addchar);
				Children.Add(newNode);
				return newNode;
			}
		}

		// See if there is a child with this character, if so, return it,
		// otherwise return null.
		internal TrieNode<T> TraverseFind(char addchar) {
			return Children.FirstOrDefault(c => (c.NodeType == TrieNodeType.Node) && (c.NodeChar == addchar));
		}

		// Recursively traverses
		internal void PrintToVector(IList<string> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(LeafText);
			} else {
				Children.ForEach(c => c.PrintToVector(vec));
			}
		}

		// Recursively traverses
		internal void PrintNodeToVector(IList<TrieNode<T>> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(this);
			} else {
				Children.ForEach(c => c.PrintNodeToVector(vec));
			}
		}
	}
}