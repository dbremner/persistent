using PersistentCollections.Interfaces;
using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PersistentCollections.Vectors;


namespace System.Linq
{
    public static class CollectionExtensions
    {
        public static RandomAccessList<T> ToImmutable<T>(this LinkedList<T> list)
        {
            return RandomAccessList<T>.Create(list);
        }

        public static PersistentList<T> ToPersistentList<T>(this IEnumerable<T> e)
        {
            var list = PersistentList<T>.Empty;

            return list.Add(e);
        }

        public static PersistentVList<T> ToPersistentVList<T>(this IEnumerable<T> e)
        {
            var list = PersistentVList<T>.Empty;

            return list.Add(e);
        }


        public static PersistentDictionary<K, V> ToPersistentDictionary<T, K, V>(this IEnumerable<T> e, Func<T, K> key, Func<T, V> val)
        {
            var d = PersistentDictionary<K, V>.Empty.AsTransient();

            foreach (var item in e)
            {
                d.Add(key(item), val(item));
            }

            return d.AsPersistent();
        }

        public static PersistentHashSet<T> ToPersistentHashSet<T>(this IEnumerable<T> e)
        {
            var list = PersistentHashSet<T>.Empty;

            return list.Add(e);
        }

    }
}
