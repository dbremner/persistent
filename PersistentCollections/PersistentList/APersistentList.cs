using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentList
{
    [Serializable]
    public abstract class APersistentList<T> : IEnumerable<T>
    {
        internal IListNode<T> root;
        protected int count;
        protected int treeDepth;

        internal TailNode<T> tailData;
        internal VersionID versionID;


        internal APersistentList(IListNode<T> root, int count, int treeDepth, TailNode<T> tailData)
        {
            this.root = root;
            this.count = count;
            this.treeDepth = treeDepth;
            this.tailData = tailData;
        }

        internal APersistentList(APersistentList<T> plist)
        {
            this.root = plist.root;
            this.count = plist.count;
            this.treeDepth = plist.treeDepth;
            this.tailData = plist.tailData;
        }


        #region State Properties
        protected bool IsFull
        {
            get { return (count >> 5) > (1 << 5 * treeDepth); }
        }

        protected bool IsTailFull
        {
            get { return count > 0 && count % 32 == 0; }
        }

        protected int TailOffset
        {
            get
            {
                return (((count - 1) >> 5) << 5);
            }
        }

        public bool IsEmpty
        {
            get { return count == 0; }
        }

        /// <summary>
        ///     Count of items in structure
        /// </summary>
        public int Count
        {
            get { return count; }
        }
        #endregion

        #region Auxiliary private methods
        internal IListNode<T> RemoveTail(int level, IListNode<T> node)
        {
            int subidx = ((count - 2) >> level) & 0x01f;
            if (level > 5)
            {
                var refNode = node as ReferencesNode<T>;

                var newChild = RemoveTail(level - 5, refNode[subidx]);
                if (newChild == null && subidx == 0)
                    return null;
                else
                {
                    return refNode.Change(subidx, newChild, versionID);
                }
            }
            else if (subidx == 0)
                return null;
            else
            {
                var refNode = node as ReferencesNode<T>;
                if (refNode == null)
                {
                    return null;
                }

                return refNode.Pop(versionID);
            }
        }

        internal IListNode<T> AddTail(ReferencesNode<T> parent, DataNode<T> tail, int level)
        {
            int subidx = ((count - 1) >> level) & 0x01f;

            if (level == 5)
            {
                return parent.Change(
                    index: subidx,
                    value: tail,
                    versionID: versionID
                    );
            }
            else
            {
                if (subidx < parent.Count)
                {
                    return parent.Change(
                        index: subidx,
                        value: AddTail((ReferencesNode<T>)parent[subidx], tail, level - 5),
                        versionID: versionID
                        );
                }
                else
                {
                    return parent.Change(
                        index: subidx,
                        value: NewPath(level - 5, tail),
                        versionID: versionID
                        );
                }
            }
        }

        internal IListNode<T> ChangeValue(int level, ReferencesNode<T> node, int index, T value)
        {
            int subidx = (index >> level) & 0x01f;

            if (level == 5)
            {
                return node.Change(
                    index: subidx,
                    value: ((DataNode<T>)node[subidx]).Change(index & 0x01f, value, versionID),
                    versionID: versionID);
            }

            var sub = (ReferencesNode<T>)node[subidx];
            var changedValue = ChangeValue(level - 5, sub, index, value);

            // Optimization for transient
            if (changedValue == sub) return node;

            return node.Change(
                index: subidx,
                value: changedValue,
                versionID: versionID);
        }

        internal DataNode<T> GetNodeAt(int index)
        {
            if (index < 0 || index >= count) throw new IndexOutOfRangeException();

            //if (index >= TailOffset) return tailData;

            var node = root;
            for (int lvl = treeDepth * 5; lvl > 0; lvl -= 5)
            {
                node = ((ReferencesNode<T>)node)[(index >> lvl) & 0x01f];
            }

            return node as DataNode<T>;
        }

        internal IListNode<T> NewPath(int level, IListNode<T> node)
        {
            if (level == 0) return node;

            return ReferencesNode<T>.CreateWithItems(versionID, NewPath(level - 5, node));
        }
        #endregion

        public T this[int i]
        {
            get
            {
                return GetAt(i);
            }
        }

        public T GetAt(int index)
        {
            if (index < 0 || index >= count) throw new IndexOutOfRangeException();

            if (index >= TailOffset) return tailData[index & 0x01f];

            var node = root;
            for (int lvl = treeDepth * 5; lvl > 0; lvl -= 5)
            {
                node = ((ReferencesNode<T>)node)[(index >> lvl) & 0x01f];
            }

            return ((DataNode<T>)node)[index & 0x01f];
        }

        public bool Contains(T item)
        {
            foreach (var elem in this)
            {
                if (item.Equals(elem)) return true;
            }

            return false;
        }

        public int IndexOf(T item)
        {
            int index = 0;
            foreach (var elem in this)
            {
                if (item.Equals(elem)) return index;
                index++;
            }

            return index;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (tailData == null) 
                return Enumerable
                    .Empty<T>()
                    .GetEnumerator();

            return Enumerate(root)
                .Concat(tailData.Enumerate(count & 0x01f))
                .GetEnumerator();
        }

        /// <summary>
        ///     Recursively enumerate through childs of node
        /// </summary>
        private IEnumerable<T> Enumerate(IListNode<T> node)
        {
            if (node is DataNode<T>)
            {
                return (DataNode<T>)node;
            }
            else if (node is ReferencesNode<T>)
            {
                return ((ReferencesNode<T>)node)
                    .SelectMany(x => Enumerate(x));
            }
            else return Enumerable.Empty<T>();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(2 + Count * 2);
            sb.Append("[ ");

            foreach (var item in this)
                sb.Append(item + " ");

            sb.Append("]");
            return sb.ToString();
        }
    }

    internal interface IListNode<T> { }
}
