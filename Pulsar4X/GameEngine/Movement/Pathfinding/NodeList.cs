using System;
using System.Collections;
using System.Collections.Generic;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// The NodeList class represents a collection of nodes. Internally, it uses a Hashtable instance to provide
    /// fast lookup via a Node class's Key value.  The Graph class maintains its list of nodes via this class.
    /// </summary>
    public class NodeList : IEnumerable
    {
        private readonly Hashtable _data = new Hashtable();

        public NodeList() { }

        public NodeList(NodeList nodes)
        {
            _data = (Hashtable)nodes._data.Clone();
        }

        #region Public Properties

        /// <summary>
        /// Returns a particular Node instance by key.
        /// </summary>
        public virtual Node this[string key]
        {
            get
            {
                object? node = _data[key];
                if (node != null)
                {
                    return (Node)node;
                }
                else
                {
                    // Handle the case when 'key' is not found in '_data' or when the value is null.
                    // You can return a default value, throw an exception, or handle it as needed.
                    throw new KeyNotFoundException($"Key '{key}' not found in the collection or has a null value.");
                }
            }
        }

        /// <summary>
        /// Returns the number of nodes in the NodeList.
        /// </summary>
        public virtual int Count => _data.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new Node to the NodeList.
        /// </summary>
        public virtual void Add(Node n)
        {
            _data.Add(n.Key, n);
        }

        /// <summary>
		/// Removes a Node from the NodeList.
		/// </summary>
        public virtual void Remove(Node n)
        {
            _data.Remove(n.Key);
        }

        /// <summary>
		/// Determines if a node with a specified Key value exists in the NodeList.
		/// </summary>
		/// <param name="key">The Key value to search for.</param>
		/// <returns>
		/// True if a Node with the specified Key exists in the NodeList; False otherwise
		/// </returns>
        public virtual bool ContainsKey(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
		/// Clears out all of the nodes from the NodeList.
		/// </summary>
        public virtual void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Returns an enumerator that can be used to iterate through the Nodes.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new NodeListEnumerator(_data.GetEnumerator());
        }

        #endregion

        #region NodeList Enumerator

        /// <summary>
        /// The NodeListEnumerator method is a custom enumerator for the NodeList object.  It essentially serves
        /// as an enumerator over the NodeList's Hashtable class, but rather than returning DictionaryEntry values,
        /// it returns just the Node object.
        /// <p />
        /// This allows for a developer using the Graph class to use a foreach to enumerate the Nodes in the graph.
        /// </summary>
        public class NodeListEnumerator : IEnumerator, IDisposable
        {
            private IDictionaryEnumerator _list;

            public NodeListEnumerator(IDictionaryEnumerator coll)
            {
                _list = coll;
            }

            public void Reset()
            {
                _list.Reset();
            }

            public bool MoveNext()
            {
                return _list.MoveNext();
            }

            public Node Current
            {
                get
                {
                    var dictionaryEntry = (DictionaryEntry)_list.Current;
                    if (dictionaryEntry.Value != null)
                    {
                        return (Node)dictionaryEntry.Value;
                    }

                    // Handle the case when _list.Current is null, not of type DictionaryEntry,
                    // or its Value is null or not of type Node.
                    // You can return a default value, throw an exception, or handle it as needed.
                    throw new InvalidOperationException("Invalid or null value in _list.Current.");
                }
            }

            // The current property on the IEnumerator interface:
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
        #endregion
    }
}