using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentQueue
{
    internal class ReversePVList<T> : IEnumerable<T>
    {
        private RBlock<T> block;
        private int offset;

        public ReversePVList(RBlock<T> block, int offset)
        {
            this.block = block;
            this.offset = offset;
        }

        public ReversePVList(PersistentVList<T> vlist)
        {
            this.block = vlist.ToReverse();
            this.offset = 0;
        }

        public T First { get { return block.Peek(offset); } }

        internal ReversePVList<T> Next()
        {
            if (offset + 1 < block.Count)
            {
                return new ReversePVList<T>(block, offset + 1);
            }

            return (block.Next != null)
                ? new ReversePVList<T>(block.Next, 0)
                : null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (block != null)
                ? block.Enumerate(offset)
                : Enumerable.Empty<T>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
