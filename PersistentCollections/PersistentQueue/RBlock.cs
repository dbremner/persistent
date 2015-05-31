using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentQueue
{
    internal class RBlock<T>
    {
        private T[] data;
        private RBlock<T> next;
        private int length = 1;

        public RBlock(T[] data)
        {
            this.data = data;
        }

        public RBlock<T> Next { get { return next; } }

        public int Count { get { return length; } }

        public T Peek(int offset)
        {
            return data[offset];
        }

        public void SetNextNode(RBlock<T> next, int lenght)
        {
            this.next = next;
            this.length = lenght;
        }

        public IEnumerator<T> Enumerate(int offset)
        {
            var q = new Queue<RBlock<T>>();

            var n = this;
            while (n.next != null)
            {
                q.Enqueue(n);
                n = n.next;
            }

            for (int i = offset; i < length; i++)
            {
                yield return data[i];
            }

            foreach (var block in q)
            {
                for (int i = 0; i < block.next.length; i++)
                {
                    yield return block.next.data[i];
                }
            }
        }
    }
}
