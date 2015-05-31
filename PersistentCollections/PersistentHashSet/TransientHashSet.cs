using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections
{
    public class TransientHashSet<T> : IEnumerable<T>
    {
        private TransientDictionary<T, bool> dict;

        internal TransientHashSet(TransientDictionary<T, bool> dict)
        {
            this.dict = dict;
        }

        public bool Contains(T item)
        {
            return dict.ContainsKey(item);
        }

        public int Count { get { return dict.Count; } }

        public void Add(T item)
        {
            dict.Add(item, true);
        }

        public void Add(T item, params T[] items)
        {
            Add(item.Yield().Concat(items));
        }

        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public bool HasValue(T item)
        {
            return dict.ContainsKey(item);
        }

        public void Remove(T item)
        {
            dict.Remove(item);
        }

        public void Remove(T item, params T[] items)
        {
            Remove(item.Yield().Concat(items));
        }

        public void Remove(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Remove(item);
            }
        }

        public PersistentHashSet<T> AsPersistent()
        {
            return new PersistentHashSet<T>(dict.AsPersistent());
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dict.Select(x => x.Key).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
