using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
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
        private int hash;

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

        public PersistentQueue<T> Enqueue(IEnumerable<T> items)
        {
            var newStack = stack;
            foreach (var item in items)
            {
                newStack = newStack.Add(item);
            }

            return new PersistentQueue<T>(newStack, reverseStack);
        }

        public PersistentQueue<T> Dequeue()
        {
            if (reverseStack == null)
            {
                if (stack.Count == 0)
                    throw new InvalidOperationException("Collection cannot be empty");

                return new PersistentQueue<T>(PersistentVList<T>.Empty, new ReversePVList<T>(stack).Next());
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

        public bool Equals(PersistentQueue<T> other)
        {
            return Enumerable.SequenceEqual(this, other);
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                int i = 1;
                foreach (var item in this)
                {
                    hash ^= item.GetHashCode() * 47 * i++;
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            var plist = obj as PersistentQueue<T>;
            if (obj == null) return false;

            return Equals(plist);
        }

        public static bool operator ==(PersistentQueue<T> a, PersistentQueue<T> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentQueue<T> a, PersistentQueue<T> b)
        {
            return !(a == b);
        }
    }
}
