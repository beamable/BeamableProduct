using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Beamable.Common
{
	[Serializable]
	public class Trie<T> : ISerializationCallbackReceiver
	{
		[Serializable]
		class SerializationEntry
		{
			public string path;
			public List<T> values;
		}
		
		[DebuggerDisplay("{path} (children=[{children.Count}]) (values=[{values.Count}])")]
		public class Node
		{
			public string path;
			public string part;
			public Node parent;
			public Dictionary<string, Node> children = new Dictionary<string, Node>();
			public List<T> values = new List<T>();

			public IEnumerable<Node> TraverseChildren()
			{
				var queue = new Queue<Node>();
				foreach (var child in children.Values)
				{
					queue.Enqueue(child);
				}

				while (queue.Count > 0)
				{
					var curr = queue.Dequeue();

					yield return curr;

					foreach (var subChild in curr.children.Values)
					{
						queue.Enqueue(subChild);
					}
				}
			}
		}

		private Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
		private Dictionary<string, Node> _pathToNode = new Dictionary<string, Trie<T>.Node>();
		private Dictionary<string, List<T>> _pathCache = new Dictionary<string, List<T>>();

		[SerializeField]
		private List<SerializationEntry> data = new List<SerializationEntry>();
		
		public void Insert(string key, T value)
		{
			var node = Search(key);
			node.values.Add(value);
		}
		
		public void InsertRange(string key, IEnumerable<T> values)
		{
			var node = Search(key);
			node.values.AddRange(values);
		}

		public void SetRange(string key, IEnumerable<T> values)
		{
			var node = Search(key);
			node.values.Clear();
			node.values.AddRange(values);
		}

		public void Remove(string key, T value)
		{
			var node = Search(key);
			node.values.Remove(value);
		}
		
		public void RemoveRange(string key, IEnumerable<T> values)
		{
			var node = Search(key);
			foreach (var value in values)
			{
				node.values.Remove(value);
			}
		}

		public List<T> GetAll(string key)
		{
			
			if (!_pathCache.TryGetValue(key, out var existing))
			{
				_pathCache[key] = existing = new List<T>();

				Node last = null;
				foreach (var node in Traverse(key))
				{
					last = node;
				}

				existing.AddRange(last.values);
				foreach (var node in last.TraverseChildren())
				{
					existing.AddRange(node.values);
				}
				
			}

			// return a clone of the list, so downstream doesn't mutate it in odd ways.
			return existing.ToList();
		}

		private void InvalidatePathCache(string key)
		{
			_pathCache.Remove(key);
		}

		public IEnumerable<Node> TraverseChildren(string key)
		{
			if (_pathToNode.TryGetValue(key, out var node))
			{
				yield return node;
				foreach (var subNode in node.TraverseChildren())
				{
					yield return subNode;
				}
			}
		}

		public IEnumerable<Node> Traverse(string key)
		{
			var parts = key.Split('.');
			var first = parts[0];
			
			if (!_nodes.TryGetValue(first, out var node))
			{
				_pathToNode[first] = _nodes[first] = node = new Node
				{
					part = first,
					path = first
				};
			}

			yield return node;
			var subPath = first;
			for (var i = 1; i < parts.Length; i++)
			{
				var curr = parts[i];
				subPath += "." + curr;
				if (!node.children.TryGetValue(curr, out var nextNode))
				{
					_pathToNode[subPath] = node.children[curr] = nextNode = new Node {parent = node, part = curr, path = subPath};
				}

				node = nextNode;
				yield return node;
			}

		}

		private Node Search(string key, bool invalidate=true)
		{
			Node last = null;
			foreach (var node in Traverse(key))
			{
				InvalidatePathCache(node.path);
				last = node;
			}

			return last;
		}

		public void OnBeforeSerialize()
		{
			data.Clear();
			foreach (var kvp in _pathToNode)
			{
				data.Add(new SerializationEntry
				{
					path = kvp.Key,
					values = kvp.Value.values
				});
			}
		}

		public void OnAfterDeserialize()
		{
			foreach (var entry in data)
			{
				InsertRange(entry.path, entry.values);
			}
		}
	}

}
