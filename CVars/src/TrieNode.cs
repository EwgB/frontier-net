/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: TrieNode.cpp 162 2010-02-15 19:11:05Z gsibley $
*/

namespace CVars {
	using System.Collections.Generic;
	using System.Linq;

	class TrieNode<T> where T : class {
		public T NodeData { get; set; }
		public List<TrieNode<T>> Children { get; }
		public TrieNodeType NodeType { get; }

		private string LeafText { get; set; }
		private char NodeChar { get; set; }

		public TrieNode() {
			NodeData = null;
			NodeType = TrieNodeType.Leaf;
			Children = new List<TrieNode<T>>();
		}

		public TrieNode(TrieNodeType nodeType) : this() {
			NodeType = nodeType;
		}

		public TrieNode(string leafText) : this() {
			LeafText = leafText;
		}

		public TrieNode(char nodeChar) : this() {
			NodeType = TrieNodeType.Node;
		}

		///<summary>
		/// Go through this node and see if this char is a branch, if so, simply return
		/// the corresponding child, otherwise create a node and return its child.
		///</summary>
		public TrieNode<T> TraverseInsert(char addchar) {
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
		public TrieNode<T> TraverseFind(char addchar) {
			return Children.FirstOrDefault(c => (c.NodeType == TrieNodeType.Node) && (c.NodeChar == addchar));
		}

		// Recursively traverses
		public void PrintToVector(IList<string> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(LeafText);
			} else {
				Children.ForEach(c => c.PrintToVector(vec));
			}
		}

		// Recursively traverses
		public void PrintNodeToVector(IList<TrieNode<T>> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(this);
			} else {
				Children.ForEach(c => c.PrintNodeToVector(vec));
			}
		}
	}
}