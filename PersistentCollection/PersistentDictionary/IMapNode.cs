using PersistentCollection.PersistentDictionary;
using PersistentCollection.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PersistentCollection.Interfaces
{
    internal interface IMapNode<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        NodeState GetNodeStateAt(int i);

        V GetValueAt(int i, NodeState state, K key);

        IMapNode<K, V> GetReferenceAt(int i);

        IMapNode<K, V> AddValueItemAt(int i, K key, V value, VersionID versionID);

        IMapNode<K, V> CreateCollisionAt(int i, K key, V value, VersionID versionID);

        IMapNode<K, V> AddToColisionAt(int i, K key, V value, VersionID versionID);

        MapNodeRelation RelationWithNodeAt(K key, int idx, NodeState state);

        IMapNode<K, V> ChangeReference(int idx, IMapNode<K, V> mapNode, VersionID versionID);

        IMapNode<K, V> CreateNewNodeFrom(int oldIdx, K key, V value, int idx1, int idx2, VersionID versionID);

        IMapNode<K, V> CreateReference(int index, IMapNode<K, V> mapNode, NodeState state, VersionID versionID);

        int GetHashCodeAt(int idx, NodeState state);

        IMapNode<K, V> CreateReferenceNode(int idx, IMapNode<K, V> node, VersionID versionID);

        IMapNode<K, V> ChangeValue(int idx, NodeState state, K key, V value, VersionID versionID);

        bool IsKeyAt(int idx, NodeState state, K key);

        int ValueCount { get; }

        int ReferenceCount { get; }

        IMapNode<K, V> RemoveValue(int idx, NodeState state, K key, VersionID versionID);

        IMapNode<K, V> Merge(IMapNode<K, V> newNode, int index, VersionID versionID);

        IMapNode<K, V> MakeRoot(VersionID versionID);
    }

    internal interface ICollisionCollection<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        int HashCode { get; }

        ICollisionCollection<K, V> Add(K Key, V item, VersionID versionID);

        ICollisionCollection<K, V> Remove(K key, VersionID versionID);

        ICollisionCollection<K, V> Change(K key, V value, VersionID versionID);

        KeyValuePair<K, V> GetItem(K key);

        bool HasItemWithKey(K key);

        int Count { get; }

        bool ContentEqual(ICollisionCollection<K, V> c2);

        KeyValuePair<K, V> GetRemainingValue(K removedKey);
    }
}
