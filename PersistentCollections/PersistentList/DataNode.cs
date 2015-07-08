using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PersistentCollections.PersistentList
{
    [Serializable]
    internal class DataNode<T> : IListNode<T>, IEquatable<DataNode<T>>, IEnumerable<T>
    {
        internal T[] data;
        private VersionID versionID;
        private int hash;

        public T this[int i]
        {
            get
            {
                return data[i];
            }
            private set
            {
                data[i] = value;
            }
        }

        internal DataNode(T[] sourceData, VersionID versionID)
        {
            data = sourceData.ToArray();
            this.versionID = versionID;
        }

        private DataNode(T[] sourceData, int idx, T item, VersionID versionID)
            : this(sourceData, versionID)
        {
            data[idx] = item;
        }

        internal DataNode<T> Change(int index, T item, VersionID versionID = null)
        {
            if (versionID != null && this.versionID == versionID)
            {
                data[index] = item;
                return this;
            }

            return new DataNode<T>(data, index, item, versionID);
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            foreach (var elem in data) yield return elem;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal TailNode<T> ToTailNode(VersionID versionID)
        {
            return new TailNode<T>(this, versionID);
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                for (int i = 0; i < 32; i++)
                {
                    var h = 47 * data[i].GetHashCode();

                    hash ^= h << i | h >> (32 - i);
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }

        public override bool Equals(object other)
        {
            var otherData = other as DataNode<T>;
            if (otherData == null) return false;

            return Equals(otherData);
        }

        public bool Equals(DataNode<T> other)
        {
            return Enumerable.SequenceEqual(data, other.data);
        }
    }
}
