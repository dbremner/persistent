using System;
using System.Collections;
using System.Collections.Generic;

namespace PersistentCollections.PersistentList
{
    [Serializable]
    internal class ReferencesNode<T> : IListNode<T>, IEnumerable<IListNode<T>>, IEquatable<ReferencesNode<T>>
    {
        /// <summary>
        ///     Version ID of data structure in transient state, null otherwise
        /// </summary>
        private readonly VersionID versionID;
        private IListNode<T>[] references;
        private int hash;

        public int Count
        {
            get { return references.Length; }
        }

        public IListNode<T> this[int i]
        {
            get
            {
                return references[i];
            }
            private set
            {
                references[i] = value;
            }
        }

        internal ReferencesNode<T> Pop(VersionID versionID)
        {
            var copy = new ReferencesNode<T>(versionID, Count - 1);
            Array.Copy(references, copy.references, copy.Count);

            return copy;
        }

        private ReferencesNode(VersionID versionID)
        {
            this.versionID = versionID;
        }

        private ReferencesNode(VersionID versionID, int size)
            : this(versionID)
        {
            references = new IListNode<T>[size];
        }

        internal static ReferencesNode<T> CreateWithItems(VersionID versionID, params IListNode<T>[] nodes)
        {
            return new ReferencesNode<T>(versionID)
            {
                references = nodes
            };
        }

        internal ReferencesNode<T> Change(int index, IListNode<T> value, VersionID versionID)
        {
            if (value == null && index == Count - 1) return Pop(versionID);

            if (versionID != null && this.versionID == versionID && index < Count)
            {
                // Version ID of transient equals, we can mutate the data
                references[index] = value;
                return this;
            }

            var size = (index < Count) ? Count : index + 1;

            var copy = new ReferencesNode<T>(versionID, size);
            Array.Copy(references, copy.references, Count);
            copy[index] = value;

            return copy;
        }

        public IEnumerator<IListNode<T>> GetEnumerator()
        {
            foreach (var node in references) yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    var h = 47 * references[i].GetHashCode();

                    hash ^= h << i | h >> (32 - i);
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }

        public override bool Equals(object other)
        {
            var otherRef = other as ReferencesNode<T>;
            if (otherRef == null) return false;

            return Equals(otherRef);
        }

        public bool Equals(ReferencesNode<T> other)
        {
            if (other.Count != Count) return false;

            for (int i = 0; i < Count; i++)
            {
                if (!ReferenceEquals(other[i], this[i]) && !other[i].Equals(this[i])) return false;
            }

            return true;
        }
    }
}
