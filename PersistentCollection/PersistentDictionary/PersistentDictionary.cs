using PersistentCollections.Interfaces;
using PersistentCollections.PersistentDictionary;
using PersistentCollections.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    public class PersistentDictionary<K, V> : APersistentDictionary<K, V>, IEnumerable<KeyValuePair<K, V>>, IEquatable<PersistentDictionary<K, V>>
    {
        #region Empty structure
        private static readonly PersistentDictionary<K, V> empty = new PersistentDictionary<K, V>(null, 0);
        public static PersistentDictionary<K, V> Empty { get { return empty; } }
        #endregion

        internal PersistentDictionary(IMapNode<K, V> root, int count)
            :base(root, count)
        {

        }

        #region Main functionality
        
        public PersistentDictionary<K, V> Add(K key, V value)
        {
            if (count == 0)
            {
                var idx = key.GetHashCode() & 0x01f;

                return new PersistentDictionary<K, V>(
                    root: CreateValueNode(idx, key, value),
                    count: 1
                    );
            }

            SettingState set = SettingState.changedItem | SettingState.newItem;
            var newRoot = Adding(0, root, (UInt32)key.GetHashCode(), key, value, ref set);

            return new PersistentDictionary<K,V>(
                root: newRoot,
                count: count + ((set.HasFlag(SettingState.newItem)) ? 1 : 0)
                );
        }

        public PersistentDictionary<K, V> Add<T>(IEnumerable<T> e, Func<T, K> key, Func<T, V> val)
        {
            var d = PersistentDictionary<K, V>.Empty
                .AsTransient();
            d.Add(e, key, val);

            return d.AsPersistent();
        }

        public PersistentDictionary<K, V> Remove(K key)
        {
            if (count == 0) 
                throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key"); ;

            var newRoot = Removing(0, root, (UInt32)key.GetHashCode(), key);

            if (count == 1) return empty;
            else if (newRoot.ValueCount == 1 && newRoot.ReferenceCount == 0) 
                newRoot = newRoot.MakeRoot(versionID);

            return new PersistentDictionary<K, V>(
                root: newRoot,
                count: count - 1
                );
        }

        public V this[K i]
        {
            get { return GetValue(i); }
        }
        #endregion

        public TransientDictionary<K, V> AsTransient()
        {
            return new TransientDictionary<K, V>(root, count);
        }

        public override int GetHashCode()
        {
            return (root != null)
                ? root.GetHashCode()
                : 0;
        }

        public override bool Equals(object obj)
        {
            var dict = obj as PersistentDictionary<K, V>;

            return dict != null && Equals(dict);
        }

        public bool Equals(PersistentDictionary<K, V> other)
        {
            if (count != other.count) return false;

            if ((root != null) == (other.root != null))
            {
                if (root != null)
                    return root.Equals(other.root);

                return true;
            }

            return false;
        }

        public static bool operator ==(PersistentDictionary<K, V> a, PersistentDictionary<K, V> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentDictionary<K, V> a, PersistentDictionary<K, V> b)
        {
            return !(a == b);
        }
    }
    
    
}
