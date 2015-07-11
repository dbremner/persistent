using PersistentCollections.PersistentQueue;
using PersistentCollections.PersistentVList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    [Serializable]
    public sealed class PersistentVList<T> : APersistentVList<T>, IEnumerable<T>, IEquatable<PersistentVList<T>>
    {
        [NonSerialized]
        private int hash;
        private static readonly PersistentVList<T> empty = new PersistentVList<T>(null, 0);
        public static PersistentVList<T> Empty { get { return empty; } }

        internal PersistentVList(Block<T> block, int offset)
            : base(block, offset)
        {

        }

        public T this[int i]
        {
            get { return GetAt(i); }
        }

        public PersistentVList<T> RemoveLast()
        {
            if (block == null) throw new InvalidOperationException("Collection cannot be empty");

            if (offset > 0)
            {
                return new PersistentVList<T>(block, offset - 1);
            }

            return (Count > 1)
                ? new PersistentVList<T>(block.Next, block.Offset)
                : Empty;
        }

        public PersistentVList<T> Add(T item)
        {
            if (block == null)
            {
                return new PersistentVList<T>(
                    block: new Block<T>(Block<T>.InitialBlockSize, item),
                    offset: 0);
            }
            else
            {
                lock (block)
                {
                    if (block.IsFull)
                    {
                        return new PersistentVList<T>(
                            block: new Block<T>(block, offset, item),
                            offset: 0);
                    }
                    else if (block.TryAdd(offset, item))
                    {
                        return new PersistentVList<T>(
                            block: block,
                            offset: offset + 1);
                    }
                }

                return new PersistentVList<T>(
                    block: new Block<T>(block, offset, Block<T>.InitialBlockSize, item),
                    offset: 0);
            }
        }

        public PersistentVList<T> Add(T item, params T[] items)
        {
            return Add(item.Yield().Concat(items));
        }
        public PersistentVList<T> Add(IEnumerable<T> items)
        {
            var tList = AsTransient();

            tList.Add(items);

            return tList.AsPersistent();
        }

        public PersistentVList<T> RemoveLast(int count)
        {
            var tr = AsTransient();
            tr.RemoveLast(count);
            return tr.AsPersistent();
        }

        internal RBlock<T> ToReverse()
        {
            return block.Reverse(offset);
        }

        public TransientVList<T> AsTransient()
        {
            return new TransientVList<T>(block, offset);
        }

        public bool Equals(PersistentVList<T> other)
        {
            if (Count != other.Count) return false;
            if (Count == 0) return true;
            if (block == null) return false;

            return block.IsEqual(offset, other.block, other.offset);
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                if (block != null)
                {
                    int i = 1;
                    foreach (var item in this)
                    {
                        hash ^= item.GetHashCode() * 47 * i++;
                    }
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            var plist = obj as PersistentVList<T>;
            if (obj == null) return false;

            return Equals(plist);
        }

        public static bool operator ==(PersistentVList<T> a, PersistentVList<T> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentVList<T> a, PersistentVList<T> b)
        {
            return !(a == b);
        }

    }


}
