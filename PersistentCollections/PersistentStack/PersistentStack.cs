﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    [Serializable]
    public class PersistentStack<T> : IEnumerable<T>, IEquatable<PersistentStack<T>>
    {
        private readonly T item;
        private readonly PersistentStack<T> next;

        private static readonly PersistentStack<T> empty = new PersistentStack<T>(null, default(T));
        public static PersistentStack<T> Empty { get { return empty; } }

        private PersistentStack(PersistentStack<T> next, T item)
        {
            this.next = next;
            this.item = item;
        }

        public T Peek 
        { 
            get 
            {
                return item; 
            } 
        }

        public PersistentStack<T> Push(T item)
        {
            return new PersistentStack<T>(this, item);
        }

        public PersistentStack<T> Pop()
        {
            if (next == null) throw new InvalidOperationException("Collection cannot be empty");

            return next;
        }

        public PersistentStack<T> Pop(out T item)
        {
            item = this.item;

            return Pop();
        }


        public IEnumerator<T> GetEnumerator()
        {
            var n = this;
            while (!object.ReferenceEquals(n, empty))
            {
                yield return n.item;
                n = n.next;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            return item.GetHashCode();
        }

        public bool Equals(PersistentStack<T> other)
        {
            var a = this;
            var b = other;

            while (true)
            {
                if (ReferenceEquals(a, b)) return true;
                if (!a.item.Equals(b.item)) return false;

                if (a.next == null ^ b.next == null) return false; //one is empty, not the other
                if (a.next == null && b.next == null) return a.item.Equals(b.item);
                a = a.next;
                b = b.next;
            }
        }

        public override bool Equals(object obj)
        {
            var pstack = obj as PersistentStack<T>;
            if (pstack == null) return false;

            return Equals(pstack);
        }

        public static bool operator ==(PersistentStack<T> a, PersistentStack<T> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentStack<T> a, PersistentStack<T> b)
        {
            return !(a == b);
        }
    }
}

