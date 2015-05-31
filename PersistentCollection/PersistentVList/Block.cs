using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentVList
{
    internal sealed class Block<T>
    {
        private const int MaxSize = 1 << 13;
        internal const int InitialBlockSize = 1;

        private int lastModified;
        private T[] data;
        private Block<T> next;
        private int offset;
        private int restCount;
        private VersionID versionID;

        public bool IsFull
        {
            get
            {
                return lastModified + 1 == data.Length;
            }
        }

        public int Offset { get { return offset; } }
        public Block<T> Next { get { return next; } }

        public int Count(int offset)
        {
            return offset + restCount + 1;
        }

        private Block() { }

        public Block(int size, T item, VersionID VersionID = null)
        {
            if (size > MaxSize) size = MaxSize;

            data = new T[size];
            data[0] = item;

            this.versionID = VersionID;
        }

        public Block(Block<T> next, int offset, T item, VersionID VersionID = null)
            : this(next, offset, next.data.Length << 1, item, VersionID)
        {

        }

        public Block(Block<T> next, int offset, int size, T item, VersionID VersionID = null)
            : this(size, item, VersionID)
        {
            this.next = next;
            this.offset = offset;
            this.restCount = (next != null) ? next.Count(offset) : 0;
        }

        /// <summary>
        ///     Shallow copy
        /// </summary>
        public Block<T> Copy(Block<T> next)
        {
            return new Block<T>()
            {
                data = data,
                lastModified = lastModified,
                next = next,
                restCount = restCount,
                offset = offset
            };
        }

        /// <summary>
        ///     Deep copy with specified version ID
        /// </summary>
        public Block<T> Copy(VersionID VersionID, int lastModified)
        {
            return new Block<T>()
            {
                data = data.ToArray(),
                lastModified = lastModified,
                next = next,
                restCount = restCount,
                offset = offset,
                versionID = VersionID
            };
        }


        public Block(Block<T> source, VersionID VersionID, int offset, T item)
        {
            this.next = source.next;
            this.offset = source.offset;
            this.versionID = VersionID;
            this.restCount = source.restCount;
            this.lastModified = offset;

            data = new T[source.data.Length];
            Array.Copy(source.data, data, offset + 1);
            data[++lastModified] = item;
        }

        internal Block<T> TransientAdd(int offset, T item, VersionID VersionID)
        {
            if (VersionID != this.versionID)
            {
                return (TryAdd(offset, item))
                    ? this
                    : new Block<T>(this, VersionID, offset, item);
            }

            data[++lastModified] = item;
            return this;
        }

        internal bool TryAdd(int offset, T item)
        {
            if (offset != lastModified) return false;

            data[++lastModified] = item;
            return true;
        }

        public T GetAt(int index, int offset)
        {
            var rIdx = Count(offset) - index - 1;

            var node = this;
            while (rIdx > offset)
            {
                rIdx -= offset + 1;
                offset = node.offset;
                node = node.next;
            }

            return node.data[offset - rIdx];
        }

        public IEnumerable<T> Enumerate(int offset)
        {
            var s = new Stack<Block<T>>();

            var n = this;
            while (n.next != null)
            {
                s.Push(n);
                n = n.next;
            }

            foreach (var block in s)
            {
                for (int i = 0; i <= block.offset; i++)
                {
                    yield return block.next.data[i];
                }
            }

            for (int i = 0; i <= offset; i++)
            {
                yield return data[i];
            }
        }

        internal Block<T> SetAt(int index, int offset, T item, VersionID versionID)
        {
            var rIdx = Count(offset) - index - 1;

            var s = new Stack<Block<T>>();

            var node = this;
            while (rIdx > offset)
            {
                rIdx -= offset + 1;
                offset = node.offset;
                s.Push(node);
                node = node.next;
            }

            if (versionID != node.versionID)
            {
                node = node.Copy(versionID, offset);
            }

            node.data[offset - rIdx] = item;

            // Path copying
            foreach (var n in s)
            {
                if (versionID == n.versionID)
                {
                    // Versions are equal, we can mutate data and skip copying
                    n.next = node;
                    return this;
                }

                node = n.Copy(node);
            }

            // Make deep copy of last node, in order to avoid lastMod issue
            node.data = node.data.ToArray();
            return node;
        }

        internal Block<T> TransientRemove(int offset, VersionID versionID)
        {
            if (versionID == this.versionID)
            {
                lastModified--;
                return this;
            }

            return Copy(versionID, offset - 1);
        }

        internal RBlock<T> Reverse(int offset)
        {
            var s = new Stack<Block<T>>();

            var n = this;
            while (n != null)
            {
                s.Push(n);
                n = n.next;
            }

            var node = s.Pop().CreateRBlock();
            var root = node;

            // Path copying
            foreach (var b in s)
            {
                var newNode = b.CreateRBlock();
                node.SetNextNode(newNode, b.offset + 1);

                node = newNode;
            }

            node.SetNextNode(null, offset + 1);

            return root;
        }

        internal RBlock<T> CreateRBlock()
        {
            return new RBlock<T>(data);
        }

        internal bool IsEqual(int off1, Block<T> b2, int off2)
        {
            var b1 = this;

            while (b1 != null && b2 != null)
            {
                if (!b1.data[off1].Equals(b2.data[off2]))
                {
                    return false;
                }

                if (off1 == 0)
                {
                    off1 = b1.offset;
                    b1 = b1.next;
                }
                else off1--;

                if (off2 == 0)
                {
                    off2 = b2.offset;
                    b2 = b2.next;
                }
                else off2--;

                if (ReferenceEquals(b1, b2))
                {
                    return off1 == off2;
                }
            }

            return b1 == b2;
        }
    }
}
