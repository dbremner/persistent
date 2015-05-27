using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollection.PersistentVList
{

    public sealed class TransientVList<T> : APersistentVList<T>, IEnumerable<T>
    {
        private VersionID versionID;

        internal TransientVList(Block<T> block, int offset)
            : base(block, offset)
        {
            this.versionID = new VersionID();
        }

        public T this[int i]
        {
            get { return GetAt(i); }
            set { SetAt(i, value); }
        }

        public void SetAt(int index, T value)
        {
            if (versionID == null)
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (index < 0 || index > Count)
                throw new IndexOutOfRangeException();
            else if (index == Count) Add(value);
            else
            {
                block = block.SetAt(index, offset, value, versionID);
            }
        }

        public PersistentVList<T> AsPersistent()
        {
            versionID = null;

            return (block != null)
                ? new PersistentVList<T>(block, offset)
                : PersistentVList<T>.Empty;
        }

        public void RemoveLast()
        {
            if (versionID == null)
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (block == null)
                throw new InvalidOperationException("Collection cannot be empty");

            if (offset > 0)
            {
                block = block.TransientRemove(offset, versionID);
                offset--;
            }
            else if (Count > 1)
            {
                offset = block.Offset;
                block = block.Next;
            }
            else
            {
                block = null;
                offset = 0;
            }
        }

        public void RemoveLast(int count)
        {
            if (versionID == null)
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (block == null)
                throw new InvalidOperationException("Collection cannot be empty");

            if (count < 0 || count > Count)
                throw new InvalidOperationException("Cannot remove more items than collection contains");
            else if (count == Count)
            {
                block = null;
                offset = 0;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    RemoveLast();
                }
            }
        }

        public void Add(T item)
        {
            if (versionID == null)
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (block == null)
            {
                block = new Block<T>(Block<T>.InitialBlockSize, item, versionID);
                offset = 0;
            }
            else if (block.IsFull)
            {
                block = new Block<T>(block, offset, item, versionID);
                offset = 0;
            }
            else
            {
                block = block.TransientAdd(offset, item, versionID);
                offset++;
            }
        }

        public void Add(T item, params T[] items)
        {
            Add(item.Yield().Concat(items));
        }
        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }
}
