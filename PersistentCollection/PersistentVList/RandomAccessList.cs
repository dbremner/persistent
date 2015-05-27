using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PersistentCollection.Interfaces;
using PersistentCollection;

namespace PersistentCollection.Vectors
{
    public class RandomAccessList<T> : IEnumerable<T>
    {
        #region Fields
        /// <summary>
        ///     The number of elements contained in RandomAccessList<T>
        /// </summary>
        protected int count;

        /// <summary>
        ///     Root of random access list
        /// </summary>
        protected Node root;

        protected Node lastTree;

        /// <summary>
        ///     An empty instance of RandomAccessList
        /// </summary>
        private static readonly RandomAccessList<T> empty = new RandomAccessList<T>();

        #endregion

        #region Properties
        /// <summary>
        ///     The number of elements contained in RandomAccessList<T>
        /// </summary>
        public int Count
        {
            get 
            {
                return count;
            }
        }

        public T this[int i]
        {
            get { return GetAt(i); }
        }

        public static RandomAccessList<T> Empty
        {
            get
            {
                return empty;
            }
        }

        #endregion

        protected RandomAccessList()
        {

        } 

        protected RandomAccessList(Node root, Node lastTree, int count)
        {
            this.root = root;
            this.count = count;
            this.lastTree = lastTree;
        }

        public static RandomAccessList<T> Create(IEnumerable<T> seq)
        {
            var list = empty;
            foreach (var item in seq)
            {
                list = list.Add(item);
            }
            return list;
        } 

        // TODO: optimize
        protected class Node
        {
            internal T Value { get; private set; }
            internal Node RightNode { get; private set; }
            internal Node LeftChild { get; private set; }
            internal Node RightChild { get; private set; }

            internal int NumOfChildern { get; private set; }
            public Node(T value)
            {
                Value = value;
            }

            public Node(T value, Node rightNode) 
                : this(value)
            {
                RightNode = rightNode;
            }

            public Node(T value, Node rightNode,
                Node leftChild, Node rightChild)
                : this(value, rightNode)
            {
                LeftChild = leftChild;
                RightChild = rightChild;

                if (leftChild != null) NumOfChildern = leftChild.NumOfChildern + rightChild.NumOfChildern + 2;
            }

        }

        public int IndexOf(T item)
        {
            int i = 0;
            foreach (var node in this)
            {
                if (item.Equals(node)) return i;
                i++;
            }
            return -1;
        }
        /*
        /// <summary>
        ///     Creates persistent vector with inserted item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted</param>
        /// <param name="item">The object to insert</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Index is not a valid index</exception>
        public RandomAccessList<T> Insert(int index, T item)
        {
            if (index == count) return Add(item);

            Node newNode = null;
            Node oldNode = null;

            T value = default(T);
            Node rightNode = null;
            Node leftChild = null;
            Node rightChild = null;

            var t = GetPathToNodeAt(index).ToList();
            foreach (var node in GetPathToNodeAt(index).Reverse())
            {
                value = node.Value;
                rightNode = node.RightNode;
                leftChild = node.LeftChild;
                rightChild = node.RightChild;

                if (oldNode == null)
                {
                    value = item;
                }
                else if (oldNode.Equals(rightNode))
                {
                    rightNode = newNode;
                }
                else if (leftChild.Equals(oldNode))
                {
                    leftChild = newNode;
                }
                else
                {
                    leftChild = new Node(node.LeftChild.Value, newNode, node.LeftChild.LeftChild, node.LeftChild.RightChild);
                    rightChild = newNode;
                }

                oldNode = node;
                newNode = new Node(value, rightNode, leftChild, rightChild);
            }

            return new RandomAccessList<T>(newNode, count);
        }*/

        /// <summary>
        ///     Gets the element at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public T GetAt(int idx)
        {
            if (idx < 0 || idx >= count) throw new IndexOutOfRangeException();

            Node treeNode = null;

            if (idx >= lastTree.NumOfChildern)
            {
                // Requested node is in last tree node
                idx -= (count - lastTree.NumOfChildern - 1);
                treeNode = lastTree;
            }
            else
            {
                foreach (var node in GetListSequence())
                {
                    if (node.NumOfChildern < idx)
                    {
                        idx -= node.NumOfChildern + 1;
                    }
                    else
                    {
                        treeNode = node;
                        break;
                    }
                }
            }

            while (idx != 0)
            {
                if ((treeNode.NumOfChildern >> 1) >= idx)
                {
                    treeNode = treeNode.LeftChild;
                    idx--;
                }
                else
                {
                    treeNode = treeNode.RightChild;
                    idx -= treeNode.NumOfChildern + 2;
                }
            }

            return treeNode.Value;
        }
        /*
        private IEnumerable<Node> GetPathToNodeAt(int ReverseIndex)
        {
            if (ReverseIndex < 0 || ReverseIndex >= count) throw new IndexOutOfRangeException();

            // reverse index
            int index = count - ReverseIndex - 1;

            foreach (var node in GetListSequence())
            {
                if (node.NumOfChildern < index)
                {
                    index -= node.NumOfChildern + 1;
                    yield return node;
                }
                else
                {
                    yield return node;
                    var treeNode = node;

                    while (index != 0)
                    {
                        if ((treeNode.NumOfChildern >> 1) >= index)
                        {
                            treeNode = treeNode.LeftChild;
                            index--;
                        }
                        else
                        {
                            treeNode = treeNode.RightChild;
                            index -= treeNode.NumOfChildern + 2;
                        }
                        yield return treeNode;
                    }

                    break;
                }
            }
        }*/

        public RandomAccessList<T> Add(T item)
        {
            if (root != null && root.RightNode != null && root.NumOfChildern == root.RightNode.NumOfChildern)
            {
                var newRoot = new Node(item, root.RightNode.RightNode, root, root.RightNode);

                return new RandomAccessList<T>(newRoot, (root.RightNode.RightNode == null) ? newRoot : lastTree, count + 1);
            }

            return new RandomAccessList<T>(new Node(item, root), lastTree, count + 1);
        }

        public RandomAccessList<T> RemoveLast()
        {
            if (count == 0) throw new Exception("Cannot remove element from empty collection");

            if (root.NumOfChildern > 0)
            {
                if (root == lastTree) return new RandomAccessList<T>(root.LeftChild, root.RightChild, count - 1);

                return new RandomAccessList<T>(root.LeftChild, lastTree, count - 1);
            }
            else if (count == 1) return empty;

            return new RandomAccessList<T>(root.RightNode, lastTree, count - 1);
        }

        public bool Contains(T item)
        {
            foreach (var node in this)
            {
                if (item.Equals(node)) return true;
            }
            return false;
        }

        public T Concat(T list)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetListSequence()
                .SelectMany(x => TraverseTree(x))
                .GetEnumerator();
        }

        private IEnumerable<Node> GetListSequence()
        {
            var node = root;

            while (node != null)
            {
                yield return node;
                node = node.RightNode;
            }
        }

        private IEnumerable<T> TraverseTree(Node node)
        {
            var postOrderList = node.Value.Yield();
                
            if (node.LeftChild != null) postOrderList = postOrderList.Concat(TraverseTree(node.LeftChild));
            if (node.RightChild != null) postOrderList = postOrderList.Concat(TraverseTree(node.RightChild));

            return postOrderList;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
