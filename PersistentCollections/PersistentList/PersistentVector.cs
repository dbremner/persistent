using PersistentCollections.Interfaces;
using PersistentCollections.PersistentList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PersistentCollections
{
    public class PersistentList<T> : APersistentList<T>, IEquatable<PersistentList<T>>
    {
        private int hash;
        protected static readonly PersistentList<T> empty = new PersistentList<T>(null, 0, 0, null);
        public static PersistentList<T> Empty
        {
            get { return empty; }
        }

        public PersistentList(TransientList<T> transient)
            : base(transient)
        {

        }

        #region Main functionality
        public PersistentList<T> Add(T item)
        {
            if (count == 0)
            {
                return new PersistentList<T>(
                       root: null,
                       count: 1,
                       treeDepth: 0,
                       tailData: new TailNode<T>(item, null));
            }

            if (!IsTailFull)
            {
                // Tail is not full, we simply add new item to the tail
                return new PersistentList<T>(
                    root: root,
                    count: count + 1,
                    treeDepth: treeDepth,
                    tailData: tailData.Push(item, count & 0x01f, versionID));
            }

            if (IsFull)
            {
                // Root overflow, we push the tail and create a new tail from inserted item
                var newTail = new TailNode<T>(item, versionID);
                var oldTail = tailData.ToDataNode(versionID);

                if (treeDepth == 0)
                {
                    return new PersistentList<T>(
                        root: ReferencesNode<T>.CreateWithItems(versionID, root, oldTail),
                        count: count + 1,
                        treeDepth: treeDepth + 1,
                        tailData: newTail);
                }
                return new PersistentList<T>(
                    root: ReferencesNode<T>.CreateWithItems(versionID, root, NewPath(treeDepth * 5, oldTail)),
                    count: count + 1,
                    treeDepth: treeDepth + 1,
                    tailData: newTail);
            }

            // Tail is full, we push it
            return new PersistentList<T>(
                root: (root == null)
                    ? tailData.ToDataNode(versionID)
                    : AddTail((ReferencesNode<T>)root, tailData.ToDataNode(versionID), treeDepth * 5),
                count: count + 1,
                treeDepth: treeDepth,
                tailData: new TailNode<T>(item, versionID));
        }

        public PersistentList<T> Add(T item, params T[] items)
        {
            return Add(item.Yield().Concat(items));
        }

        public PersistentList<T> Add(IEnumerable<T> items)
        {
            var tList = AsTransient();

            foreach (var item in items)
            {
                tList.Add(item);
            }

            return tList.AsPersistent();
        }

        public PersistentList<T> SetAt(int index, T value)
        {
            if (index >= 0 && index < count)
            {
                // Indexed value is in the tail
                if (index >= TailOffset)
                {
                    return new PersistentList<T>(root, count, treeDepth, tailData.Change(index & 0x01f, value, versionID));
                }

                // Root points to leaf, we simply change the value 
                if (root is DataNode<T>)
                {
                    return new PersistentList<T>(
                        ((DataNode<T>)root).Change(index & 0x01f, value), count, treeDepth, tailData);
                }

                // Root points to references of nodes, we recursively copy the path
                return new PersistentList<T>(
                    ChangeValue(treeDepth * 5, (ReferencesNode<T>)root, index, value), 
                    count, treeDepth, tailData);
            }

            if (index == count) return Add(value);

            throw new IndexOutOfRangeException();
        }

        public PersistentList<T> RemoveLast()
        {
            if (count == 0) throw new InvalidOperationException("Cannot remove element from an empty list.");
            if (count == 1) return Empty;

            if (count % 32 != 1)
            {
                return new PersistentList<T>(root, count - 1, treeDepth, tailData);
            }

            var newTail = GetNodeAt(count - 2);
            var newRoot = RemoveTail(treeDepth * 5, root);
            int newDepth = treeDepth;
            if (newRoot == null)
            {
                newRoot = null;
            }
            if (treeDepth > 1 && ((ReferencesNode<T>)newRoot).Count == 1)
            {
                newRoot = ((ReferencesNode<T>)newRoot)[0];
                newDepth--;
            }

            return new PersistentList<T>(newRoot, count - 1, newDepth, newTail.ToTailNode(versionID));
        }

        public PersistentList<T> RemoveLast(int count)
        {
            var tr = AsTransient();
            tr.RemoveLast(count);

            return tr.AsPersistent();
        }
        #endregion

        private PersistentList(IListNode<T> root, int count, int treeDepth, TailNode<T> tailData)
            :base(root, count, treeDepth, tailData)
        {

        }

        public TransientList<T> AsTransient()
        {
            return new TransientList<T>(this);
        }

        private bool IsEqual(IListNode<T> node1, IListNode<T> node2)
        {
            return object.ReferenceEquals(node1, node2) || node1.Equals(node2);
        }

        public bool Equals(PersistentList<T> other)
        {
            if (count != other.count) return false;
            if (tailData == null) return true;

            if (!tailData.ContentEquals(other.tailData, count % 32)) return false;

            return IsEqual(root, other.root);
        }

        public override bool Equals(object obj)
        {
            var plist = obj as PersistentList<T>;
            if (obj == null) return false;

            return Equals(plist);
        }

        public static bool operator ==(PersistentList<T> a, PersistentList<T> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentList<T> a, PersistentList<T> b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                if (root != null)
                {
                    hash = root.GetHashCode();
                }

                if (tailData != null)
                {
                    hash ^= tailData.HashCode(count & 0x01f);
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }
    }

    public class TransientList<T> : APersistentList<T>
    {
        public new T this[int i]
        {
            get
            {
                return GetAt(i);
            }
            set
            {
                SetAt(i, value);
            }
        }

        public TransientList(PersistentList<T> plist)
            : base(plist)
        {
            versionID = new VersionID();
            if (tailData != null)
            {
                tailData = new TailNode<T>(tailData, count & 0x01f, versionID);
            }
        }

        public PersistentList<T> AsPersistent()
        {
            versionID = null;

            return (count != 0)
                ? new PersistentList<T>(this)
                : PersistentList<T>.Empty;
        }

        #region Main functionality
        public void RemoveLast()
        {
            if (versionID == null)
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (count == 0) throw new InvalidOperationException("Cannot remove element from an empty list.");
            if (count == 1)
            {
                tailData = null;
                count = 0;
                return;
            }

            if (count % 32 == 1)
            {
                this.tailData = GetNodeAt(count - 2).ToTailNode(versionID);
                var newRoot = RemoveTail(treeDepth * 5, root);

                if (treeDepth > 1 && ((ReferencesNode<T>)newRoot).Count == 1)
                {
                    newRoot = ((ReferencesNode<T>)newRoot)[0];
                    treeDepth--;
                }

                this.root = newRoot;
            }
            else
            {
                tailData = tailData.Pop(versionID, count & 0x01f);
            }

            count--;
        }

        public void RemoveLast(int count)
        {
            for (int i = 0; i < count; i++) RemoveLast();
        }

        public void Add(T item)
        {
            if (versionID == null) 
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            if (count == 0)
            {
                tailData = new TailNode<T>(item, versionID);
            }
            else if (!IsTailFull)
            {
                // Tail is not full, we simply add new item to the tail
                tailData = tailData.Push(item, count & 0x01f, versionID);
            }
            else
            {
                if (IsFull)
                {
                    // Root overflow, we push the tail and create a new tail with the inserted item

                    if (treeDepth == 0)
                    {
                        root = ReferencesNode<T>.CreateWithItems(versionID, root, tailData.ToDataNode(versionID));
                    }
                    else
                    {
                        root = ReferencesNode<T>.CreateWithItems(versionID, root, NewPath(treeDepth * 5, tailData.ToDataNode(versionID)));
                    }

                    treeDepth++;
                }
                else
                {
                    root = (root == null)
                        ? tailData.ToDataNode(versionID)
                        : AddTail((ReferencesNode<T>)root, tailData.ToDataNode(versionID), treeDepth * 5);
                }

                tailData = new TailNode<T>(item, versionID);
            }

            count++;
        }

        public void Add(params T[] items)
        {
            foreach (var item in items) Add(item);
        }

        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items) Add(item);
        }

        public void SetAt(int index, T value)
        {
            if (versionID == null) 
                throw new NotSupportedException("Cannot modify transient after call AsPersistent() method");

            
            if (index >= 0 && index < count)
            {
                // Indexed value is in the tail
                if (index >= TailOffset)
                {
                    tailData = tailData.Change(index & 0x01f, value, versionID);
                }

                // Root points to leaf, we only change the value 
                else if (root is DataNode<T>)
                {
                    root = ((DataNode<T>)root).Change(index & 0x01f, value, versionID);
                }

                // Root points to references of nodes, we recursively change values along the path
                else
                {
                    root = ChangeValue(treeDepth * 5, (ReferencesNode<T>)root, index, value);
                }
            }
            else if (index == count) Add(value);
            else throw new IndexOutOfRangeException();
        }
        #endregion

    }
}
