using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollection.PersistentList
{
    class TailNode<T>
    {
        private T[] data;
        private VersionID versionID;
        private int lastModified;

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

        private TailNode(T[] sourceData, int length, VersionID versionID)
            : this(versionID)
        {
            Array.Copy(sourceData, data, length);
            lastModified = length - 1;
        }

        private TailNode(T[] sourceData, int length, int idx, T item, VersionID versionID)
            : this(sourceData, length, versionID)
        {
            data[idx] = item;
        }

        public TailNode(TailNode<T> sourceTail, int length, VersionID versionID)
            : this(versionID)
        {
            if (length == 0) length = 32;
            Array.Copy(sourceTail.data, data, length);
            lastModified = length - 1;
        }

        private TailNode(VersionID versionID)
        {
            data = new T[32];
            this.versionID = versionID;
        }

        public TailNode(T item, VersionID versionID)
            : this(versionID)
        {
            data[0] = item;
        }

        internal TailNode(DataNode<T> dataNode, VersionID versionID)
            : this(versionID)
        {
            Array.Copy(dataNode.data, data, 32);
            lastModified = 31;
        }

        internal TailNode<T> Push(T item, int idx, VersionID versionID)
        {
            if (versionID != null && this.versionID == versionID)
            {
                lastModified++;
                data[idx] = item;
                return this;
            }

            // Mutable tail is not thread safe so we use lock
            lock (this)
            {
                if (idx == lastModified + 1)
                {
                    data[++lastModified] = item;
                    return this;
                }
            }

            return new TailNode<T>(data, idx + 1, idx, item, versionID);
        }

        internal DataNode<T> ToDataNode(VersionID versionID)
        {
            return new DataNode<T>(data, versionID);
        }

        public IEnumerable<T> Enumerate(int length)
        {
            if (length == 0) length = 32;

            for (int i = 0; i < length; i++) yield return data[i];
        }

        internal TailNode<T> Change(int index, T item, VersionID versionID)
        {
            if (versionID != null && this.versionID == versionID)
            {
                data[index] = item;
                return this;
            }

            return new TailNode<T>(data, lastModified + 1, index, item, versionID);
        }

        internal TailNode<T> Pop(VersionID versionID, int count)
        {
            if (versionID != null && this.versionID == versionID)
            {
                lastModified--;
                return this;
            }

            return new TailNode<T>(data, count - 1, versionID);
        }

        public bool ContentEquals(TailNode<T> other, int length)
        {
            for (int i = 0; i < length; i++)
                if (!other.data[i].Equals(data[i])) return false;

            return true;
        }

        public int HashCode(int length)
        {
            int hash = 0;
            for (int i = 0; i < length; i++)
            {
                var h = 47 * data[i].GetHashCode();

                hash ^= h << i | h >> (32 - i);
            }
            return hash;
        }
    }
}
