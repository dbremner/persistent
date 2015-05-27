using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollection.PersistentList
{
    internal class ReferencesNode<T> : IVectorNode<T>, IEnumerable<IVectorNode<T>>
    {
        /// <summary>
        ///     Version ID of data structure in transient state, null otherwise
        /// </summary>
        private readonly VersionID versionID;
        private IVectorNode<T>[] references;

        public int Count
        {
            get { return references.Length; }
        }

        public IVectorNode<T> this[int i]
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
            references = new IVectorNode<T>[size];
        }

        internal static ReferencesNode<T> CreateWithItems(VersionID versionID, params IVectorNode<T>[] nodes)
        {
            return new ReferencesNode<T>(versionID)
            {
                references = nodes
            };
        }

        // TODO
        public bool ContentEquals(IVectorNode<T> other)
        {
            var otherRef = other as ReferencesNode<T>;
            if (otherRef == null || otherRef.Count != Count) return false;

            for (int i = 0; i < Count; i++)
            {
                if (otherRef[i] != this[i] && otherRef[i].ContentEquals(this[i])) return false;
            }

            return true;
        }

        internal ReferencesNode<T> Change(int index, IVectorNode<T> value, VersionID versionID)
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

        public IEnumerator<IVectorNode<T>> GetEnumerator()
        {
            foreach (var node in references) yield return node;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
