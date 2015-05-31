using PersistentCollections.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PersistentCollections.PersistentDictionary
{
    internal enum NodeState
    {
        Nil, Value, Collision, Reference
    }

    internal enum MapNodeRelation
    {
        Equal, Collide, Different
    }

    public abstract class APersistentDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        internal IMapNode<K, V> root;
        protected int count;
        internal VersionID versionID;

        public bool IsEmpty { get { return count == 0; } }

        public int Count
        {
            get { return count; }
        }

        internal IMapNode<K, V> CreateValueNode(int idx, K key, V value, VersionID versionID = null)
        {
            return new MapNode<K, V>(idx, key, value, versionID);
        }

        internal APersistentDictionary(IMapNode<K, V> root, int count, VersionID versionID = null)
        {
            this.root = root;
            this.count = count;
            this.versionID = versionID;
        }

        [Flags]
        internal enum SettingState
        {
            newItem = 1, changedItem = 2
        }

        internal IMapNode<K, V> CreateCommonPath(UInt32 h1, UInt32 h2, int i, int shift, IMapNode<K, V> node, K key, V value)
        {
            var i1 = (int)(h1 >> shift) & 0x01f;
            var i2 = (int)(h2 >> shift) & 0x01f;

            if (i1 != i2) return node.CreateNewNodeFrom(i, key, value, i1, i2, versionID);

            // Creating longer path
            var s = new Stack<int>();

            do
            {
                s.Push(i1);

                shift += 5;
                i1 = (int)(h1 >> shift) & 0x01f;
                i2 = (int)(h2 >> shift) & 0x01f;
            }
            while (i1 == i2);

            var newNode = node.CreateNewNodeFrom(i, key, value, i1, i2, versionID);

            // Creating path
            foreach (var idx in s)
            {
                newNode = node.CreateReferenceNode(idx, newNode, versionID);
            }

            return newNode;
        }

        internal IMapNode<K, V> Adding(int shift, IMapNode<K, V> node, UInt32 hash, K key, V value, ref SettingState set)
        {
            var idx = (int)((hash >> shift) & 0x01f);
            var state = node.GetNodeStateAt(idx);

            if (state == NodeState.Reference)
            {
                // On position is reference node
                var referencedNode = node.GetReferenceAt(idx);
                var newNode = Adding(shift + 5, referencedNode, hash, key, value, ref set);

                return (newNode == referencedNode || set.HasFlag(SettingState.changedItem))
                    ? node.ChangeReference(idx, newNode, versionID)
                    : node;
            }

            if (state == NodeState.Nil) 
                return node.AddValueItemAt(idx, key, value, versionID);
            
            // On position is value node or collision collection
            var relation = node.RelationWithNodeAt(key, idx, state);

            if (relation == MapNodeRelation.Equal)
            {
                // Value with the same key already exists
                var n = node.GetValueAt(idx, state, key);

                set &= ~SettingState.newItem;

                if (n.Equals(value))
                {
                    set &= ~SettingState.changedItem;
                    return node;
                }

                return node.ChangeValue(idx, state, key, value, versionID);
            }
            if (relation == MapNodeRelation.Collide)
            {
                // Hash collision
                return (state == NodeState.Value)
                    ? node.CreateCollisionAt(idx, key, value, versionID)
                    : node.AddToColisionAt(idx, key, value, versionID);
            }

            // Hashes are different, we create longer path
            return node.CreateReference(
                index: idx,
                mapNode: CreateCommonPath((UInt32)node.GetHashCodeAt(idx, state), hash, idx, shift + 5, node, key, value),
                state: state,
                versionID: versionID
                );
            
        }

        internal IMapNode<K, V> Removing(int shift, IMapNode<K, V> node, UInt32 hash, K key)
        {
            var idx = (int)((hash >> shift) & 0x01f);
            var state = node.GetNodeStateAt(idx);

            if (state == NodeState.Nil) 
                throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");
            if (state == NodeState.Value || state == NodeState.Collision)
            {
                var relation = node.RelationWithNodeAt(key, idx, state);

                if (relation == MapNodeRelation.Equal) 
                {
                    return node.RemoveValue(idx, state, key, versionID);
                }
                else throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");
            }

            // On position is reference node
            var newNode = Removing(shift + 5, node.GetReferenceAt(idx), hash, key);

            if (newNode.ValueCount == 1 && newNode.ReferenceCount == 0)
            {
                // In recursion, we carry node to remove

                return (node.ReferenceCount > 1 || node.ValueCount != 0)
                    ? node.Merge(newNode, idx, versionID)
                    : newNode;
            }
            else
            {
                return node.ChangeReference(idx, newNode, versionID);
            }
        }

        public V GetValue(K key)
        {
            var hash = key.GetHashCode();
            var node = root;

            for (int shift = 0; shift < 32; shift += 5)
            {
                var idx = (hash >> shift) & 0x01f;
                var state = node.GetNodeStateAt(idx);

                if (state == NodeState.Reference) node = node.GetReferenceAt(idx);
                else if (state == NodeState.Value || state == NodeState.Collision)
                {
                    return node.GetValueAt(idx, state, key);
                }
                else 
                    throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");

            }

            throw new Exception();
        }

        public bool Contains(V item)
        {
            foreach (var elem in this)
            {
                if (elem.Value.Equals(item)) return true;
            }

            return false;
        }

        public bool ContainsKey(K key)
        {
            if (root == null) return false;
            
            var hash = key.GetHashCode();
            var node = root;

            for (int shift = 0; shift < 32; shift += 5)
            {
                var idx = (hash >> shift) & 0x01f;
                var state = node.GetNodeStateAt(idx);

                if (state == NodeState.Value || state == NodeState.Collision)
                {
                    return node.IsKeyAt(idx, state, key);
                }

                if (state == NodeState.Nil) return false;

                node = node.GetReferenceAt(idx);
            }

            throw new Exception();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            if (root == null)
                return Enumerable
                    .Empty<KeyValuePair<K, V>>()
                    .GetEnumerator();

            return root.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
