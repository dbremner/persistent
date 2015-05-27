using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public static class MyAssert
    {
        public static void Throws<T>(Action func) where T : Exception
        {
            var exceptionThrown = false;

            try
            {
                func.Invoke();
            }
            catch (T)
            {
                exceptionThrown = true;
            }

            if (!exceptionThrown)
            {
                throw new AssertFailedException(
                    String.Format("An exception of type {0} was expected, but not thrown", typeof(T))
                    );
            }
        }

        public static void ArrayEquals<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (!Enumerable.SequenceEqual(a, b))
            {
                throw new AssertFailedException("Arrays are not equal");
            }
        }

        public static void DictEquals<K, V>(IEnumerable<KeyValuePair<K, V>> a, IEnumerable<KeyValuePair<K, V>> b)
            where K : IComparable<K>
        {
            var orderedA = a.OrderBy(x => x.Key);
            var orderedB = b.OrderBy(x => x.Key);

            if (!Enumerable.SequenceEqual(orderedA, orderedB))
            {
                throw new AssertFailedException("Dictionaries are not equal");
            }
        }

        public static void SetEquals<T>(IEnumerable<T> a, IEnumerable<T> b)
            where T : IComparable<T>
        {
            var orderedA = a.OrderBy(x => x);
            var orderedB = b.OrderBy(x => x);

            if (!Enumerable.SequenceEqual(orderedA, orderedB))
            {
                throw new AssertFailedException("Sets are not equal");
            }
        }

        public static void ArrayNotEquals<T>(T[] a, T[] b)
        {
            if (Enumerable.SequenceEqual(a, b))
            {
                throw new AssertFailedException("Arrays are equal");
            }
        }
    }
}
