using PersistentCollections.Interfaces;
using PersistentCollections.PersistentDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PersistentCollections
{
    public sealed class TransientDictionary<K, V> : APersistentDictionary<K, V>, IEnumerable<KeyValuePair<K, V>>
    {
        internal TransientDictionary(IMapNode<K, V> root, int count)
            : base(root, count, new VersionID())
        {

        }

        public PersistentDictionary<K, V> AsPersistent()
        {
            versionID = null;
            if (count == 0) return PersistentDictionary<K, V>.Empty;

            return new PersistentDictionary<K, V>(root, count);
        }

        #region Main functionality
        public void Add(K key, V value)
        {
            if (versionID == null)
                throw new NotSupportedException("Transient dictionary cannot be modified after call AsPersistent() method");

            var set = SettingState.changedItem | SettingState.newItem;

            if (count == 0)
            {
                var idx = key.GetHashCode() & 0x01f;

                root = CreateValueNode(idx, key, value, versionID);
            }
            else
            {
                root = Adding(0, root, (UInt32)key.GetHashCode(), key, value, ref set);
            }

            if (set.HasFlag(SettingState.newItem)) count++;
        }

        public void Add<T>(IEnumerable<T> e, Func<T, K> key, Func<T, V> val)
        {
            foreach (var item in e)
            {
                Add(key(item), val(item));
            }
        }

        public void Remove(K key)
        {
            if (versionID == null)
                throw new NotSupportedException("Transient dictionary cannot be modified after call AsPersistent() method");

            if (count == 0) 
                throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");

            var newRoot = Removing(0, root, (UInt32)key.GetHashCode(), key);
            count--;

            if (newRoot.ValueCount == 1) newRoot = newRoot.MakeRoot(versionID);
            else if (newRoot.ValueCount == 0 && newRoot.ReferenceCount == 0) newRoot = null;

            root = newRoot;
        }
        #endregion

        public V this[K i]
        {
            get { return GetValue(i); }
            set { Add(i, value); }
        }
    }
}
