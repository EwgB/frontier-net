/*
Cross platform "CVars" functionality.
This Code is covered under the LGPL.  See COPYING file for the license.
$Id: TrieNode.cpp 162 2010-02-15 19:11:05Z gsibley $
*/

namespace CVars {
	//using CVar;

	using System.Collections.Generic;

	using System.Linq;
	using System.Linq.Expressions;

	class TrieNode {
		public object NodeData { get; }
		public IList<TrieNode> Children { get; }
		public TrieNodeType NodeType { get; }

		private string LeafText { get; set; }
		private char NodeChar { get; set; }

		public TrieNode() {
			NodeData = null;
			NodeType = TrieNodeType.Leaf;
			Children = new List<TrieNode>();
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
		TrieNode TraverseInsert(char addchar) {
			foreach (var child in Children) {
				//found child
				if ((child.NodeType == TrieNodeType.Node) && (child.NodeChar == addchar)) {
					return child;
				}
			}

			TrieNode newNode = new TrieNode(addchar);
			Children.Add(newNode);
			return newNode;
		}

		// See if there is a child with this character, if so, return it,
		// otherwise return null.
		TrieNode TraverseFind(char addchar) {
			foreach (var child in Children) {
				//found child
				if ((child.NodeType == TrieNodeType.Node) && (child.NodeChar == addchar)) {
					return child;
				}
			}
			return null;
		}

		// Recursively traverses
		void PrintToVector(IList<string> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(LeafText);
			} else {
				foreach (var child in Children) {
					child.PrintToVector(vec);
				}
			}
		}

		// Recursively traverses
		void PrintNodeToVector(IList<TrieNode> vec) {
			if (NodeType == TrieNodeType.Leaf) {
				vec.Add(this);
			} else {
				foreach (var child in Children) {
					child.PrintNodeToVector(vec);
				}
			}
		}
	}
}