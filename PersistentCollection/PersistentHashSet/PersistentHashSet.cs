using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    public class PersistentHashSet<T> : IEnumerable<T>, IEquatable<PersistentHashSet<T>>
    {
        #region Empty structure
        private static readonly PersistentHashSet<T> empty = 
            new PersistentHashSet<T>(PersistentDictionary<T, bool>.Empty);
        public static PersistentHashSet<T> Empty { get { return empty; } }
        #endregion
        private PersistentDictionary<T, bool> dict;

        internal PersistentHashSet(PersistentDictionary<T, bool> dict)
        {
            this.dict = dict;
        }

        public bool Contains(T item)
        {
            return dict.ContainsKey(item);
        }

        public PersistentHashSet<T> Add(T item)
        {
            return new PersistentHashSet<T>(dict.Add(item, true));
        }

        public PersistentHashSet<T> Add(T item, params T[] items)
        {
            return Add(item.Yield().Concat(items));
        }

        public PersistentHashSet<T> Add(IEnumerable<T> items)
        {
            var tSet = AsTransient();

            foreach (var item in items)
            {
                tSet.Add(item);
            }

            return tSet.AsPersistent();
        }

        public bool HasValue(T item)
        {
            return dict.ContainsKey(item);
        }

        public PersistentHashSet<T> Remove(T item)
        {
            return new PersistentHashSet<T>(dict.Remove(item));
        }

        public PersistentHashSet<T> Remove(T item, params T[] items)
        {
            return Remove(item.Yield().Concat(items));
        }

        public PersistentHashSet<T> Remove(IEnumerable<T> items)
        {
            var tSet = AsTransient();

            foreach (var item in items)
            {
                tSet.Remove(item);
            }

            return tSet.AsPersistent();
        }

        public TransientHashSet<T> AsTransient()
        {
            return new TransientHashSet<T>(dict.AsTransient());
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dict.Select(x => x.Key).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            var set = obj as PersistentHashSet<T>;

            return set != null && Equals(set);
        }

        public override int GetHashCode()
        {
            return dict.GetHashCode();
        }

        public bool Equals(PersistentHashSet<T> other)
        {
            return dict.Equals(other.dict);
        }

        public static bool operator ==(PersistentHashSet<T> a, PersistentHashSet<T> b)
        {
            if (((object)a == null) == ((object)b == null))
            {
                if ((object)a != null) return a.Equals(b);
            }
            else return false;

            return true;
        }

        public static bool operator !=(PersistentHashSet<T> a, PersistentHashSet<T> b)
        {
            return !(a == b);
        }

        public int Count { get { return dict.Count; } }
    }
    
}
