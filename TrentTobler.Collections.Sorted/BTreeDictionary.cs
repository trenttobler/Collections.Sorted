//Copyright 2011 Trent Tobler. All rights reserved.

//Redistribution and use in source and binary forms, with or without modification, are
//permitted provided that the following conditions are met:

//   1. Redistributions of source code must retain the above copyright notice, this list of
//      conditions and the following disclaimer.

//   2. Redistributions in binary form must reproduce the above copyright notice, this list
//      of conditions and the following disclaimer in the documentation and/or other materials
//      provided with the distribution.

//THIS SOFTWARE IS PROVIDED BY TRENT TOBLER ''AS IS'' AND ANY EXPRESS OR IMPLIED
//WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL TRENT TOBLER OR
//CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace TrentTobler.Collections
{
    /// <summary>
    /// An O(log N) implementation of the ISortedDictionary interface.
    /// </summary>
    /// <typeparam name="TKey">The type for the sorted keys.</typeparam>
    /// <typeparam name="TValue">The type for the associated values.</typeparam>
    public class BTreeDictionary<TKey,TValue> : ISortedDictionary<TKey,TValue>
    {
        #region Fields

        Node _root;
        readonly Node _first;
        readonly KeyCollection _keys;
        readonly ValueCollection _values;
        readonly IComparer<TKey> _keyComparer;
        bool _allowDuplicates = false;

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant( _root != null );
            Contract.Invariant( _first != null );
            Contract.Invariant( _keyComparer != null );
            Contract.Invariant( _keys != null );
            Contract.Invariant( _values != null );
        }

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new BTreeDictionary instance optimized for the specified node capacity.
        /// </summary>
        /// <param name="nodeCapacity">The capacity in keys for each node in the tree structure.</param>
        public BTreeDictionary( int nodeCapacity = 128 )
            : this( Comparer<TKey>.Default, nodeCapacity )
        {
             Contract.Requires( nodeCapacity > 2, SR.btreeCapacityError );
        }

        /// <summary>
        /// Initializes a new BTreeDictionary instance.
        /// </summary>
        /// <param name="keyComparer">The comparer for ordering keys in the structure.</param>
        /// <param name="nodeCapacity">The capacity in keys for each node in the tree structure.</param>
        public BTreeDictionary( IComparer<TKey> keyComparer, int nodeCapacity )
        {
            Contract.Requires( keyComparer != null, SR.nullArgumentError );
            Contract.Requires( nodeCapacity > 2, SR.btreeCapacityError );

            this._keyComparer = keyComparer;
            this._first = new Node( nodeCapacity );
            this._root = this._first;

            this._keys = new KeyCollection( this );
            this._values = new ValueCollection( this );
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value associated with the specified key.  An arbitrary value will be chosen if key is a duplicate.
        /// </summary>
        /// <param name="key">The key for which to retrieve the associated value.</param>
        /// <returns>The value associated with the key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if( this.TryGetValue( key, out result ) )
                    return result;
                throw new InvalidOperationException( SR.keyNotFoundError );
            }
            set
            {
                Contract.Requires( !IsReadOnly, SR.immutableError );

                Node leaf;
                int pos;
                if( Node.Find( _root, key, KeyComparer, 0, out leaf, out pos ) )
                    leaf.SetValue( pos, value );
                else
                {
                    Node.Insert( key, ref leaf, ref pos, ref _root );
                    leaf.SetValue( pos, value );
                }
            }
        }

        /// <summary>
        /// Gets the number of key value pairs in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                Contract.Ensures( Contract.Result<int>() >= 0 );

                return this._root.TotalCount;
            }
        }

        /// <summary>
        /// Gets the key comparer.
        /// </summary>
        public IComparer<TKey> KeyComparer
        {
            get
            {
                Contract.Ensures( Contract.Result<IComparer<TKey>>() != null );

                return this._keyComparer;
            }
        }

        /// <summary>
        /// Gets the collection of keys in the dictionary.
        /// </summary>
        public ISortedCollection<TKey> Keys
        {
            get
            {
                Contract.Ensures( Contract.Result<ISortedCollection<TKey>>() != null );

                return this._keys;
            }
        }

        /// <summary>
        /// Gets the collection of values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                Contract.Ensures( Contract.Result<ICollection<TValue>>() != null );

                return this._values;
            }
        }

        /// <summary>
        /// Gets or sets indication whether this dictionary is readonly or mutable.
        /// </summary>
        public bool IsReadOnly
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets indication whether this dictionary allows duplicate keys.
        /// </summary>
        public bool AllowDuplicates
        {
            get
            {
                return this._allowDuplicates;
            }
            set
            {
                Contract.Requires( !IsReadOnly, SR.immutableError );
                Contract.Requires( value == true || AllowDuplicates == false || Count == 0, SR.collectionMustBeEmptyToClearAllowDuplicates );

                this._allowDuplicates = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines the bias when inserting duplicate keys.
        /// </summary>
        /// <value>If positive, add duplicates to the end, if negative, add duplicates to the beginning, and if 0, insert them arbitrarily.</value>
        public int InsertBias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that determines the bias when removing duplicate keys.
        /// </summary>
        /// <value>If positive, remove duplicates from the end, if negative, remove duplicates from the beginning, and if 0, remove them arbitrarily.</value>
        public int RemoveBias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that determines the bias when finding a value for a duplicate key.
        /// </summary>
        /// <value>If positive, value will be the first duplicate, if negative, value will be the last duplicate, and if 0, value will be chosen arbitrarily.</value>
        public int LookupBias
        {
            get;
            set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets indication of whether the dictionary contains an entry for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if the dictionary contains the key; otherwise, false.</returns>
        public bool ContainsKey( TKey key )
        {
            Node leaf;
            int pos;
            var found = Node.Find( _root, key, KeyComparer, 0, out leaf, out pos );
            return found;
        }

        /// <summary>
        /// Tries to get the value for the specified key.  An arbitrary value will be chosen if key is a duplicate.
        /// </summary>
        /// <param name="key">The key for which to try to get the value.</param>
        /// <param name="value">The value found for the specified key, or a default value if not found.</param>
        /// <returns>True if the value was found; otherwise, false.</returns>
        public bool TryGetValue( TKey key, out TValue value )
        {
            Node leaf;
            int pos;
            var found = Node.Find( _root, key, KeyComparer, AllowDuplicates ? LookupBias : 0, out leaf, out pos );
            value = found ? leaf.GetValue( pos ) : default(TValue);
            return found;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.  If duplicates are allowed, new value location relative to other duplicates will be arbirtary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        public void Add( TKey key, TValue value )
        {
            Contract.Requires( !IsReadOnly, SR.immutableError );

            Node leaf;
            int pos;
            var found = Node.Find( _root, key, KeyComparer, AllowDuplicates ? InsertBias : 0, out leaf, out pos );
            if( found )
            {
                if( !AllowDuplicates )
                    throw new InvalidOperationException( SR.duplicateNotAllowedError );
                if( InsertBias > 0 )
                    ++pos;
            }

            Node.Insert( key, ref leaf, ref pos, ref _root );
            leaf.SetValue( pos, value );
        }

        /// <summary>
        /// Gets the key value pair at the specified index.
        /// </summary>
        /// <param name="index">The index at which to get the key value pair.</param>
        /// <returns>The key value pair at the specified index.</returns>
        public KeyValuePair<TKey, TValue> At( int index )
        {
            Contract.Requires( index >= 0 && index < this.Count, SR.indexOutOfRangeError );

            var leaf = Node.LeafAt( _root, ref index );
            return new KeyValuePair<TKey, TValue>( leaf.GetKey( index ), leaf.GetValue( index ) );
        }

        /// <summary>
        /// Clears the dictionary of all items.
        /// </summary>
        public void Clear()
        {
            Contract.Requires( !IsReadOnly, SR.immutableError );

            Node.Clear( _first );
            _root = _first;
        }

        /// <summary>
        /// Remove the key and associated value from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was removed; otherwise, false if key was not found.</returns>
        public bool Remove( TKey key )
        {
            Contract.Requires( !IsReadOnly, SR.immutableError );

            Node leaf;
            int pos;
            if( !Node.Find( _root, key, KeyComparer, AllowDuplicates ? RemoveBias : 0, out leaf, out pos ) )
                return false;

            Node.Remove( leaf, pos, ref _root );
            return true;
        }

        /// <summary>
        /// Removes the key and associated value from the dictionary at the specified index.
        /// </summary>
        /// <param name="index">The index at which to remove the key value pair.</param>
        public void RemoveAt( int index )
        {
            Contract.Requires( !IsReadOnly, SR.immutableError );
            Contract.Requires( index >= 0 && index < this.Count );

            var leaf = Node.LeafAt( _root, ref index );
            Node.Remove( leaf, index, ref _root );
        }


        /// <summary>
        /// Get all items starting at the index, and moving forward.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> ForwardFromIndex( int index )
        {
            Contract.Requires( index >= 0 && index <= this.Count, SR.indexOutOfRangeError );
            Contract.Ensures( Contract.Result<IEnumerable<KeyValuePair<TKey, TValue>>>() != null );

            var node = Node.LeafAt( _root, ref index );
            return Node.ForwardFromIndex( node, index );
        }

        /// <summary>
        /// Get all items starting at the index, and moving backward.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> BackwardFromIndex( int index )
        {
            Contract.Requires( index >= 0 && index <= this.Count, SR.indexOutOfRangeError );
            Contract.Ensures( Contract.Result<IEnumerable<KeyValuePair<TKey, TValue>>>() != null );

            var node = Node.LeafAt( _root, ref index );
            return Node.BackwardFromIndex( node, index );
        }

        /// <summary>
        /// Sets the value at the specified index, leaving the key unchanged.
        /// </summary>
        /// <param name="index">The index at which to set the value.</param>
        /// <param name="value">The value to associate at the specified index.</param>
        public void SetValueAt( int index, TValue value )
        {
            Contract.Requires( !IsReadOnly, SR.immutableError );
            Contract.Requires( index >= 0 && index < this.Count );

            var leaf = Node.LeafAt( _root, ref index );
            leaf.SetValue( index, value );
        }

        /// <summary>
        /// Gets an enumerator of key value pairs for the entire collection in sorted ascending order.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Contract.Ensures( Contract.Result<IEnumerator<KeyValuePair<TKey, TValue>>>() != null );

            return Node.ForwardFromIndex( _first, 0 ).GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator of key value pairs in ascending order by key, for all items with a key 
        /// equal or greater than the specified key lower bound.
        /// </summary>
        /// <param name="keyLowerBound">The value at which to start returning keys.</param>
        /// <returns>All key value pairs having a key equal or greater than the lower bound, in key ascending order.</returns>
        public IEnumerable<KeyValuePair<TKey, TValue>> WhereGreaterOrEqual( TKey keyLowerBound )
        {
            Contract.Ensures( Contract.Result<IEnumerable<KeyValuePair<TKey, TValue>>>() != null );

            Node leaf;
            int leafPos;
            Node.Find( _root, keyLowerBound, KeyComparer, AllowDuplicates ? -1 : 0, out leaf, out leafPos );
            return Node.ForwardFromIndex( leaf, leafPos );
        }

        /// <summary>
        /// Gets an enumerator of key value pairs in descending order by key, for all items with a key
        /// less than or equal than the specified key upper bound.
        /// </summary>
        /// <param name="keyUpperBound">The value at which to start returning keys.</param>
        /// <returns>All key value pairs having a key equal or less than the upper bound, in key descending order.</returns>
        public IEnumerable<KeyValuePair<TKey, TValue>> WhereLessOrEqualBackwards( TKey keyUpperBound )
        {
            Contract.Ensures( Contract.Result<IEnumerable<KeyValuePair<TKey, TValue>>>() != null );

            Node leaf;
            int leafPos;
            var found = Node.Find( _root, keyUpperBound, KeyComparer, AllowDuplicates ? 1 : 0, out leaf, out leafPos );
            if( !found )
                --leafPos;
            return Node.BackwardFromIndex( leaf, leafPos );
        }

        /// <summary>
        /// Copy the entire dictionary to the specified array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The array into which to copy.</param>
        /// <param name="arrayIndex">The index at which to start copying.</param>
        public void CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex )
        {
            foreach( var item in this )
                array[arrayIndex++] = item;
        }

		/// <summary>
		/// Efficiently get a range of values starting with a specified lower bound, and continuing through to the specified upper bound.
		/// </summary>
		/// <param name="keyLowerBound">The lower bound value</param>
		/// <param name="keyUpperBound">The upper bound value</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<TKey, TValue>> WhereInRange( TKey keyLowerBound, TKey keyUpperBound )
		{
			Contract.Requires( this._keyComparer.Compare( keyUpperBound, keyLowerBound ) >= 0, String.Format( "Lower Bound ({0}) must be greater or equal to Upper Bound ({1})", keyLowerBound, keyUpperBound ) );
			Contract.Ensures( Contract.Result<IEnumerable<KeyValuePair<TKey, TValue>>>() != null );

			Node leafStart, leafEnd;
			int leafStartPos, leafEndPos;

			Node.Find( _root, keyLowerBound, KeyComparer, AllowDuplicates ? -1 : 0, out leafStart, out leafStartPos );
			if( !Node.Find( _root, keyUpperBound, KeyComparer, AllowDuplicates ? 1 : 0, out leafEnd, out leafEndPos ) )
				--leafEndPos;

			return Node.Range( leafStart, leafStartPos, leafEnd, leafEndPos );
		}

        #endregion

        #region Implementation - Nested Types

        [DebuggerDisplay( "Count={_nodeCount}/{_totalCount}, First={keys[0]}" )]
        sealed class Node
        {
            #region Fields

            readonly TKey[] _keys;
            readonly TValue[] _values;
            readonly Node[] _nodes;

            int _nodeCount;
            int _totalCount;

            Node _parent;
            Node _next;
            Node _prev;

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                // Simple BTree invariants
                Contract.Invariant( _keys != null );
                Contract.Invariant( _nodeCount >= 0 && _nodeCount <= _keys.Length );
                Contract.Invariant( _nodes == null || _keys.Length == _nodes.Length );

                // Key/Value BTree invariants
                Contract.Invariant( _values == null || _keys.Length == _values.Length );
                Contract.Invariant( _values == null || _nodes == null );
                Contract.Invariant( _values != null || _nodes != null );
                
                // Indexable BTree invariants
                Contract.Invariant( _totalCount >= 0 );
            }

            #endregion

            #region Construction

            /// <summary>
            /// Initialize the first node in the BTree structure.
            /// </summary>
            public Node( int nodeCapacity )
                : this( nodeCapacity, true )
            {
            }

            #endregion

            #region Properties

            public int TotalCount
            {
                get
                {
                    return this._totalCount;
                }
            }

            public bool IsRoot
            {
                get
                {
                    return this._parent == null;
                }
            }

            public bool IsLeaf
            {
                get
                {
                    return _nodes == null;
                }
            }

            public int NodeCount
            {
                get
                {
                    return this._nodeCount;
                }
            }

            #endregion

            #region Methods

            /// <summary>
            /// Gets the key at the specified position.
            /// </summary>
            public TKey GetKey( int pos )
            {
                Contract.Requires( pos >= 0 && pos < this._nodeCount );
                return this._keys[pos];
            }

            /// <summary>
            /// Gets the value at the specified position.
            /// </summary>
            public TValue GetValue( int pos )
            {
                Contract.Requires( pos >= 0 && pos < this.NodeCount );
                return this._values[pos];
            }

            /// <summary>
            /// Sets the value at the specified position.
            /// </summary>
            public void SetValue( int pos, TValue value )
            {
                Contract.Requires( pos >= 0 && pos < this.NodeCount );
                this._values[pos] = value;
            }

            /// <summary>
            /// Get the leaf node at the specified index in the tree defined by the specified root.
            /// </summary>
            public static Node LeafAt( Node root, ref int pos )
            {
                Contract.Requires( root != null );
                Contract.Requires( root.IsRoot );
                Contract.Requires( 0 <= pos && pos < root.TotalCount );
                Contract.Ensures( Contract.Result<Node>() != null );
                Contract.Ensures( Contract.Result<Node>().IsLeaf );
                Contract.Ensures( 0 <= pos && pos < Contract.Result<Node>().NodeCount );

                int nodeIndex = 0;
                while( true )
                {
                    if( root._nodes == null )
                    {
                        Contract.Assume( pos < root._nodeCount );
                        return root;
                    }

                    var node = root._nodes[nodeIndex];
                    if( pos < node._totalCount )
                    {
                        root = node;
                        nodeIndex = 0;
                    }
                    else
                    {
                        pos -= node._totalCount;
                        ++nodeIndex;
                    }
                }
            }

            /// <summary>
            /// Find the node and index in the tree defined by the specified root.
            /// </summary>
            public static bool Find( Node root, TKey key, IComparer<TKey> keyComparer, int duplicatesBias, out Node leaf, out int pos )
            {
                Contract.Requires( root != null );
                Contract.Requires( root.IsRoot );
                Contract.Ensures( Contract.ValueAtReturn<Node>( out leaf ) != null );
                Contract.Ensures( 0 <= Contract.ValueAtReturn<int>( out pos ) && Contract.ValueAtReturn<int>( out pos ) <= leaf.NodeCount );

                pos = Array.BinarySearch( root._keys, 0, root._nodeCount, key, keyComparer );
                while( root._nodes != null )
                {
                    if( pos >= 0 )
                    {
                        if( duplicatesBias != 0 )
                            MoveToDuplicatesBoundary( key, keyComparer, duplicatesBias, ref root, ref pos );

                        // Found an exact match.  Move down one level.
                        root = root._nodes[pos];
                    }
                    else
                    {
                        // No exact match.  Find greatest lower bound.
                        pos = ~pos;
                        if( pos > 0 )
                            --pos;
                        root = root._nodes[pos];
                    }
                    Contract.Assume( root != null );
                    pos = Array.BinarySearch( root._keys, 0, root._nodeCount, key, keyComparer );
                }

                leaf = root;
                if( pos < 0 )
                {
                    pos = ~pos;
                    return false;
                }

                if( duplicatesBias != 0 )
                    MoveToDuplicatesBoundary( key, keyComparer, duplicatesBias, ref leaf, ref pos );

                return true;
            }

            /// <summary>
            /// Insert a new key into the leaf node at the specified position.
            /// </summary>
            public static void Insert( TKey key, ref Node leaf, ref int pos, ref Node root )
            {
                // Make sure there is space for the new key.
                if( EnsureSpace( leaf, ref root ) && pos > leaf._nodeCount )
                {
                    pos -= leaf._nodeCount;
                    leaf = leaf._next;
                }

                // Insert the key.
                int moveCount = leaf._nodeCount - pos;
                Array.Copy( leaf._keys, pos, leaf._keys, pos + 1, moveCount );
                leaf._keys[pos] = key;
                ++leaf._nodeCount;
                EnsureParentKey( leaf, pos );

                // Insert space for the value.  Caller is responsible for filling in value.
                Array.Copy( leaf._values, pos, leaf._values, pos + 1, moveCount );

                // Update total counts.
                for( var node = leaf; node != null; node = node._parent )
                    ++node._totalCount;
            }

            /// <summary>
            /// Remove the item from the node at the specified position.
            /// </summary>
            public static bool Remove( Node leaf, int pos, ref Node root )
            {
                Contract.Requires( leaf != null );
                Contract.Requires( 0 <= pos && pos < leaf.NodeCount );
                Contract.Requires( leaf.IsLeaf );

                // Update total counts.
                for( var node = leaf; node != null; node = node._parent )
                    --node._totalCount;

                // Remove the key and value from the node.
                --leaf._nodeCount;
                Array.Copy( leaf._keys, pos + 1, leaf._keys, pos, leaf._nodeCount - pos );
                Array.Copy( leaf._values, pos + 1, leaf._values, pos, leaf._nodeCount - pos );
                leaf._keys[leaf._nodeCount] = default( TKey );
                leaf._values[leaf._nodeCount] = default( TValue );

                // Make sure parent keys index correctly into this node.
                if( leaf._nodeCount > 0 )
                    EnsureParentKey( leaf, pos );

                // Merge this node with others if it is below the node capacity threshold.
                Merge( leaf, ref root );
                return true;
            }

            /// <summary>
            /// Get an ascending enumerable for the collection, starting an the index in the specified leaf node.
            /// </summary>
            public static IEnumerable<KeyValuePair<TKey, TValue>> ForwardFromIndex( Node leaf, int pos )
            {
                Contract.Requires( leaf != null );
                Contract.Requires( leaf.IsLeaf );
                Contract.Requires( 0 <= pos && pos <= leaf.NodeCount );

                while( leaf != null )
                {
                    while( pos < leaf._nodeCount )
                    {
                        yield return new KeyValuePair<TKey,TValue>( leaf.GetKey(pos), leaf.GetValue( pos ) );
                        ++pos;
                    }
                    pos -= leaf._nodeCount;
                    leaf = leaf._next;
                }
            }

            /// <summary>
            /// Get a descending enumerable, starting at the index in the specified leaf node. 
            /// </summary>
            public static IEnumerable<KeyValuePair<TKey, TValue>> BackwardFromIndex( Node leaf, int pos )
            {
                Contract.Requires( leaf != null );
                Contract.Requires( leaf.IsLeaf );
                Contract.Requires( -1 <= pos && pos <= leaf.NodeCount );

                if( pos == -1 )
                {
                    // Handle special case to start moving in the previous node.
                    leaf = leaf._prev;
                    if( leaf != null )
                        pos = leaf._nodeCount - 1;
                    else
                        pos = 0;
                }
                else if( pos == leaf.NodeCount )
                {
                    // Handle special case to start moving in the next node.
                    if( leaf._next == null )
                        --pos;
                    else
                    {
                        leaf = leaf._next;
                        pos = 0;
                    }
                }

                // Loop thru collection, yielding each value in sequence.
                while( leaf != null )
                {
                    while( pos >= 0 )
                    {
                        yield return new KeyValuePair<TKey, TValue>( leaf.GetKey( pos ), leaf.GetValue( pos ) );
                        --pos;
                    }
                    leaf = leaf._prev;
                    if( leaf != null )
                        pos += leaf._nodeCount;
                }
            }

            /// <summary>
            /// Clear all keys and values from the specified node.
            /// </summary>
            public static void Clear( Node firstNode )
            {
                Contract.Requires( firstNode != null );

                int clearCount = firstNode._nodeCount;

                Array.Clear( firstNode._keys, 0, clearCount );
                Array.Clear( firstNode._values, 0, clearCount );
                firstNode._nodeCount = 0;
                firstNode._totalCount = 0;

                firstNode._parent = null;
                firstNode._next = null;
            }

            /// <summary>
            /// Get the index relative to the root node, for the position in the specified leaf.
            /// </summary>
            public static int GetRootIndex( Node leaf, int pos )
            {
                var node = leaf;
                var rootIndex = pos;
                while( node._parent != null )
                {
                    int nodePos = Array.IndexOf( node._parent._nodes, node, 0, node._parent._nodeCount );
                    for( int i = 0; i < nodePos; ++i )
                        rootIndex += node._parent._nodes[i]._totalCount;
                    node = node._parent;
                }
                return rootIndex;
            }

			public static IEnumerable<KeyValuePair<TKey, TValue>> Range( Node leafStart, int startPos, Node leafEnd, int endPos )
			{
				Contract.Requires( leafStart != null );
				Contract.Requires( leafStart.IsLeaf );
				Contract.Requires( 0 <= startPos && startPos <= leafStart.NodeCount );

				Contract.Requires( leafEnd != null );
				Contract.Requires( leafEnd.IsLeaf );
				Contract.Requires( -1 <= endPos && endPos <= leafEnd.NodeCount );


				if( endPos == -1 )
				{
					// Handle special case to start moving in the previous node.
					leafEnd = leafEnd._prev;

					if( leafEnd != null )
						endPos = leafEnd._nodeCount - 1;

				}
				else if( endPos == leafEnd.NodeCount )
				{
					// Handle special case to start moving in the next node.
					if( leafEnd._next == null )
						--endPos;
					else
					{
						leafEnd = leafEnd._next;
						endPos = 0;
					}
				}


				if( leafEnd == null )
					yield break;

				Node leaf = leafStart;
				int pos = startPos;

				while( leaf != leafEnd )
				{
					for( ; pos < leaf._nodeCount; ++pos )
						yield return new KeyValuePair<TKey, TValue>( leaf.GetKey( pos ), leaf.GetValue( pos ) );

					pos = 0;
					leaf = leaf._next;
				}

				for( ; pos <= endPos; ++pos )
					yield return new KeyValuePair<TKey, TValue>( leafEnd.GetKey( pos ), leafEnd.GetValue( pos ) );
			}

            #endregion

            #region Implementation

            Node( int nodeCapacity, bool leaf )
            {
                this._keys = new TKey[nodeCapacity];

                if( leaf )
                {
                    this._values = new TValue[nodeCapacity];
                    this._nodes = null;
                }
                else
                {
                    this._values = null;
                    this._nodes = new Node[nodeCapacity];
                }

                this._nodeCount = 0;
                this._totalCount = 0;
                this._parent = null;
                this._next = null;
                this._prev = null;
            }

            /// <summary>
            /// (Assumes: key is a duplicate in node at pos) Move to the side on the range of duplicates,
            /// as indicated by the sign of duplicatesBias.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="keyComparer"></param>
            /// <param name="duplicatesBias"></param>
            /// <param name="node"></param>
            /// <param name="pos"></param>
            static void MoveToDuplicatesBoundary( TKey key, IComparer<TKey> keyComparer, int duplicatesBias, ref Node node, ref int pos )
            {
                // Technically, we could adjust the binary search to perform most of this step, but duplicates
                // are usually unexpected.. algorithm is still O(log N), because scan include at most a scan thru two nodes
                // worth of keys, for each level.
                // Also, the binary search option would still need the ugliness of the special case for moving into the 
                // previous node; it would only be a little faster, on average, assuming large numbers of duplicates were common.

                if( duplicatesBias < 0 )
                {
                    // Move backward over duplicates.
                    while( pos > 0 && 0 == keyComparer.Compare( node._keys[pos - 1], key ) )
                        --pos;

                    // Special case: duplicates can span backwards into the previous node because the parent
                    // key pivot might be in the center for the duplicates.
                    if( pos == 0 && node._prev != null )
                    {
                        var prev = node._prev;
                        var prevPos = prev.NodeCount;
                        while( prevPos > 0 && 0 == keyComparer.Compare( prev._keys[prevPos - 1], key ) )
                        {
                            --prevPos;
                        }
                        if( prevPos < prev.NodeCount )
                        {
                            node = prev;
                            pos = prevPos;
                        }
                    }
                }
                else
                {
                    // Move forward over duplicates.
                    while( pos < node.NodeCount - 1 && 0 == keyComparer.Compare( node._keys[pos + 1], key ) )
                        ++pos;
                }
            }

            static bool EnsureSpace( Node node, ref Node root )
            {
                if( node._nodeCount < node._keys.Length )
                    return false;

                EnsureParent( node, ref root );
                EnsureSpace( node._parent, ref root );

                var sibling = new Node( node._keys.Length, node._nodes == null );
                sibling._next = node._next;
                sibling._prev = node;
                sibling._parent = node._parent;

                if( node._next != null )
                    node._next._prev = sibling;
                node._next = sibling;

                int pos = Array.IndexOf( node._parent._nodes, node, 0, node._parent._nodeCount );
                int siblingPos = pos + 1;

                Array.Copy( node._parent._keys, siblingPos, node._parent._keys, siblingPos + 1, node._parent._nodeCount - siblingPos );
                Array.Copy( node._parent._nodes, siblingPos, node._parent._nodes, siblingPos + 1, node._parent._nodeCount - siblingPos );
                ++node._parent._nodeCount;
                node._parent._nodes[siblingPos] = sibling;

                int half = node._nodeCount / 2;
                int halfCount = node._nodeCount - half;
                Move( node, half, sibling, 0, halfCount );
                return true;
            }

            static void Move( Node source, int sourceIndex, Node target, int targetIndex, int moveCount )
            {
                Move( source._keys, sourceIndex, source._nodeCount, target._keys, targetIndex, target._nodeCount, moveCount );
                if( source._values != null )
                    Move( source._values, sourceIndex, source._nodeCount, target._values, targetIndex, target._nodeCount, moveCount );

                int totalMoveCount;
                if( source._nodes == null )
                {
                    totalMoveCount = moveCount;
                }
                else
                {
                    Move( source._nodes, sourceIndex, source._nodeCount, target._nodes, targetIndex, target._nodeCount, moveCount );
                    totalMoveCount = 0;
                    for( int i = 0; i < moveCount; ++i )
                    {
                        var child = target._nodes[targetIndex + i];
                        child._parent = target;
                        totalMoveCount += child._totalCount;
                    }
                }

                source._nodeCount -= moveCount;
                target._nodeCount += moveCount;

                var sn = source;
                var tn = target;
                while( sn != null && sn != tn )
                {
                    sn._totalCount -= totalMoveCount;
                    tn._totalCount += totalMoveCount;
                    sn = sn._parent;
                    tn = tn._parent;
                }

                EnsureParentKey( source, sourceIndex );
                EnsureParentKey( target, targetIndex );
            }

            static void Move<TItem>( TItem[] source, int sourceIndex, int sourceTotal, TItem[] target, int targetIndex, int targetTotal, int count )
            {
                Array.Copy( target, targetIndex, target, targetIndex + count, targetTotal - targetIndex );
                Array.Copy( source, sourceIndex, target, targetIndex, count );
                Array.Copy( source, sourceIndex + count, source, sourceIndex, sourceTotal - sourceIndex - count );
                Array.Clear( source, sourceTotal - count, count );
            }

            static void EnsureParent( Node node, ref Node root )
            {
                if( node._parent != null )
                    return;

                var parent = new Node( node._keys.Length, false );
                parent._totalCount = node._totalCount;
                parent._nodeCount = 1;
                parent._keys[0] = node._keys[0];
                parent._nodes[0] = node;

                node._parent = parent;
                root = parent;
            }

            static void EnsureParentKey( Node node, int pos )
            {
                while( pos == 0 && node._parent != null )
                {
                    pos = Array.IndexOf( node._parent._nodes, node, 0, node._parent._nodeCount );
                    node._parent._keys[pos] = node._keys[0];
                    node = node._parent;
                }
            }

            static void Merge( Node node, ref Node root )
            {
                if( node._nodeCount == 0 )
                {
                    // Handle special case: Empty node.
                    if( node._parent == null )
                        return;

                    // Remove the node from the parent nodes.
                    int pos = Array.IndexOf( node._parent._nodes, node, 0, node._parent._nodeCount );
                    --node._parent._nodeCount;
                    Array.Copy( node._parent._keys, pos + 1, node._parent._keys, pos, node._parent._nodeCount - pos );
                    Array.Copy( node._parent._nodes, pos + 1, node._parent._nodes, pos, node._parent._nodeCount - pos );
                    node._parent._keys[node._parent._nodeCount] = default( TKey );
                    node._parent._nodes[node._parent._nodeCount] = null;

                    // Make sure parent (of the parent) keys link down correctly.
                    if( node._parent._nodeCount > 0 )
                        EnsureParentKey( node._parent, pos );

                    // Delete the node from the next/prev linked list.
					if( node._prev != null )
	                    node._prev._next = node._next;
                    if( node._next != null )
                        node._next._prev = node._prev;

                    // Merge the parent node.
                    Merge( node._parent, ref root );
                    return;
                }

                if( node._next == null )
                {
                    if( node._parent == null && node._nodeCount == 1 && node._nodes != null )
                    {
                        root = node._nodes[0];
                        root._parent = null;
                    }

                    return;
                }

                if( node._nodeCount >= node._keys.Length / 2 )
                    return;

                int count = node._next._nodeCount;
                if( node._nodeCount + count > node._keys.Length )
                    count -= ( node._nodeCount + count ) / 2;

                Move( node._next, 0, node, node._nodeCount, count );
                Merge( node._next, ref root );
            }

            #endregion
        }

        abstract class KeyValueCollectionBase<T> : ICollection<T>
        {
            #region Fields

            protected readonly BTreeDictionary<TKey,TValue> Tree;

            #endregion

            #region Construction

            public KeyValueCollectionBase( BTreeDictionary<TKey,TValue> tree )
            {
                this.Tree = tree;
            }

            #endregion

            #region Properties

            public int Count
            {
                get
                {
                    return Tree.Count;
                }
            }

            #endregion

            #region Methods

            public abstract bool Contains( T item );

            public void CopyTo( T[] array, int arrayIndex )
            {
                foreach( var item in this )
                    array[arrayIndex++] = item;
            }

            public abstract IEnumerator<T> GetEnumerator();

            #endregion

            #region ICollection<> members

            void ICollection<T>.Add( T item )
            {
                throw new NotSupportedException();
            }

            void ICollection<T>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public bool Remove( T item )
            {
                throw new NotSupportedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion
        }

        sealed class ValueCollection : KeyValueCollectionBase<TValue>
        {
            #region Construction

            public ValueCollection( BTreeDictionary<TKey, TValue> tree ) : base( tree )
            {
            }

            #endregion

            #region Methods
            
            public override bool Contains( TValue item )
            {
                return this.Tree.Any( keyValue => object.Equals( item, keyValue.Value ) );
            }

            public override IEnumerator<TValue> GetEnumerator()
            {
                return this.Tree.Select( keyValue => keyValue.Value ).GetEnumerator();
            }

            #endregion
        }

        sealed class KeyCollection : KeyValueCollectionBase<TKey>, ISortedCollection<TKey>
        {
            #region Construction

            public KeyCollection( BTreeDictionary<TKey, TValue> tree )
                : base( tree )
            {
            }

            #endregion

            #region Properties

            public bool AllowDuplicates
            {
                get
                {
                    return Tree.AllowDuplicates;
                }
            }

            public IComparer<TKey> Comparer
            {
                get
                {
                    return Tree.KeyComparer;
                }
            }

            #endregion

            #region Methods

            public int FirstIndexWhereGreaterThan( TKey value )
            {
                Node leaf;
                int pos;
                var found = Node.Find( Tree._root, value, Tree.KeyComparer, AllowDuplicates ? -1 : 0,out leaf, out pos );
                int result = Node.GetRootIndex( leaf, pos );
                if( found )
                    ++result;
                return result;
            }

            public int LastIndexWhereLessThan( TKey value )
            {
                Node leaf;
                int pos;
                var found = Node.Find( Tree._root, value, Tree.KeyComparer, AllowDuplicates ? 1 : 0, out leaf, out pos );
                int result = Node.GetRootIndex( leaf, pos );
                if( found )
                    --result;
                return result;
            }

            public TKey At( int index )
            {
                return this.Tree.At( index ).Key;
            }

            public override bool Contains( TKey item )
            {
                return Tree.ContainsKey( item );
            }

            public override IEnumerator<TKey> GetEnumerator()
            {
                return Tree.Select( keyValue => keyValue.Key ).GetEnumerator();
            }

            public IEnumerable<TKey> WhereGreaterOrEqual( TKey lowerBound )
            {
                return Tree.WhereGreaterOrEqual( lowerBound ).Select( keyValue => keyValue.Key );
            }

            public IEnumerable<TKey> WhereLessOrEqualBackwards( TKey upperBound )
            {
                return Tree.WhereLessOrEqualBackwards( upperBound ).Select( keyValue => keyValue.Key );
            }

            public IEnumerable<TKey> ForwardFromIndex( int index )
            {
                return this.Tree.ForwardFromIndex( index ).Select( item => item.Key );
            }

            public IEnumerable<TKey> BackwardFromIndex( int index )
            {
                return this.Tree.BackwardFromIndex( index ).Select( item => item.Key );
            }

            #endregion

            #region IEnumerable members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            #region ISortedCollection<> members

            void ISortedCollection<TKey>.RemoveAt( int index )
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        #endregion

        #region IDictionary<> members

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return this.Keys;
            }
        }

        #endregion

        #region ICollection<> members

        void ICollection<KeyValuePair<TKey, TValue>>.Add( KeyValuePair<TKey, TValue> item )
        {
            this.Add( item.Key, item.Value );
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains( KeyValuePair<TKey, TValue> item )
        {
            TValue value;
            return this.TryGetValue( item.Key, out value ) && Object.Equals( item.Value, value );
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove( KeyValuePair<TKey, TValue> item )
        {
            TValue value;
            if( this.TryGetValue( item.Key, out value ) && object.Equals( item.Value, value ) )
            {
                this.Remove( item.Key );
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
