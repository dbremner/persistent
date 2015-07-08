using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentVList
{
    [Serializable]
    public abstract class APersistentVList<T> : IEnumerable<T>
    {
        internal Block<T> block;
        protected int offset;

        internal APersistentVList(Block<T> block, int offset)
        {
            this.block = block;
            this.offset = offset;
        }

        public T GetAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();

            return block.GetAt(index, offset);
        }

        public bool IsEmpty { get { return block == null; } }

        public int Count { get { return (block != null) ? block.Count(offset) : 0; } }

        public IEnumerator<T> GetEnumerator()
        {
            return ((block != null)
                    ? block.Enumerate(offset)
                    : Enumerable.Empty<T>())
                .GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
