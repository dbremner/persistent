using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollection
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

            return new ReversePVList<T>(block.Next, 0);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return block.Enumerate(offset);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class RBlock<T> 
    {
        private T[] data;
        private RBlock<T> next;
        private int length = 1;

        public RBlock (T[] data)
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

    public class PersistentQueue<T> : IEnumerable<T>
    {
        private PersistentVList<T> stack;
        private ReversePVList<T> reverseStack;

        private static readonly PersistentQueue<T> empty = new PersistentQueue<T>(PersistentVList<T>.Empty,  null);
        public static PersistentQueue<T> Empty { get { return empty; } }

        private PersistentQueue(PersistentVList<T> stack, ReversePVList<T> reverseStack)
        {
            this.stack = stack;
            this.reverseStack = reverseStack;
        }

        public PersistentQueue<T> Enqueue(T item)
        {
            return new PersistentQueue<T>(stack.Add(item), reverseStack);
        }

        public PersistentQueue<T> Dequeue()
        {
            if (reverseStack == null)
            {
                if (stack.Count == 0)
                    throw new InvalidOperationException("Collection cannot be empty");
                else
                    reverseStack = new ReversePVList<T>(stack);

                return new PersistentQueue<T>(PersistentVList<T>.Empty, reverseStack.Next());
            }

            return new PersistentQueue<T>(stack, reverseStack.Next());
        }
        
        public T Peek
        {
            get
            {
                return reverseStack.First;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((reverseStack == null)
                    ? stack
                    : reverseStack.Concat(stack))
                .GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
