using PersistentCollections.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    /// <summary>
    ///     Simple implementation of collision collection using persistent array
    /// </summary>
    [Serializable]
    internal class CollisionArray<K, V> : ICollisionCollection<K, V>, IEnumerable<KeyValuePair<K, V>>
    {
        private readonly int hashCode;
        private KeyValuePair<K, V>[] collisions;
        private VersionID versionID;

        public int HashCode
        {
            get { return hashCode; }
        }

        private CollisionArray(KeyValuePair<K, V>[] collisions, int hashCode, VersionID versionID)
        {
            this.collisions = collisions;
            this.hashCode = hashCode;
            this.versionID = versionID;
        }

        public CollisionArray(VersionID versionID, params KeyValuePair<K, V>[] pairs)
        {
            if (pairs.Length < 2) throw new NotSupportedException("Collision collection should contain at least 2 items");

            this.hashCode = pairs.First().Key.GetHashCode();
            this.collisions = pairs.ToArray();
            this.versionID = versionID;
        }

        public ICollisionCollection<K, V> Add(K key, V item, VersionID versionID)
        {
            return new CollisionArray<K, V>(
                collisions: collisions.Concat(new KeyValuePair<K, V>(key, item).Yield()).ToArray(),
                hashCode: hashCode,
                versionID: versionID
                );
        }

        public ICollisionCollection<K, V> Remove(K key, VersionID versionID)
        {
            if (Count < 3) throw new NotSupportedException("Collision collection should contain at least 2 items");

            return new CollisionArray<K, V>(
                collisions: collisions.Where(x => !key.Equals(x.Key)).ToArray(),
                hashCode: hashCode,
                versionID: versionID
                );
        }

        public ICollisionCollection<K, V> Change(K key, V value, VersionID versionID)
        {
            if (versionID != null && versionID == this.versionID)
            {
                for (int i = 0; i < collisions.Length; i++)
                {
                    if (collisions[i].Key.Equals(key))
                    {
                        collisions[i] = new KeyValuePair<K, V>(key, value);
                        return this;
                    }
                }

                throw new Exception("Key not found");
            }

            return new CollisionArray<K, V>(
                collisions: collisions
                    .Where(x => !key.Equals(x.Key))
                    .Concat(new KeyValuePair<K, V>(key, value).Yield())
                    .ToArray(),
                hashCode: hashCode,
                versionID: versionID
                );
        }

        public KeyValuePair<K, V> GetItem(K key)
        {
            var pair = collisions.FirstOrDefault(x => key.Equals(x.Key));

            if (!key.Equals(pair.Key)) 
                throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");

            return pair;
        }

        public bool HasItemWithKey(K key)
        {
            return collisions.Any(x => key.Equals(x.Key));
        }

        public bool ContentEqual(ICollisionCollection<K, V> c2)
        {
            if (c2.Count != Count) return false;

            var set1 = new HashSet<KeyValuePair<K, V>>(this);
            var set2 = new HashSet<KeyValuePair<K, V>>(c2);
            
            return set1.SetEquals(set2);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return collisions
                .AsEnumerable()
                .GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return collisions.Length; }
        }

        public KeyValuePair<K, V> GetRemainingValue(K removedKey)
        {
            return collisions.First(x => !removedKey.Equals(x.Key));
        }
    }
}
