using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{

    public static class MyAssert
    {

        public static void CloneEquals<T>(T serializable)
        {
            var clone = Clone(serializable);
            Assert.AreEqual(serializable, clone);
            Assert.AreEqual(serializable.GetHashCode(), clone.GetHashCode());

        }

        private static T Clone<T>(T serializable)
        {
            var formatter = new BinaryFormatter();
            var ms = new MemoryStream();
            formatter.Serialize(ms,serializable);
            ms.Position = 0;
            return (T) formatter.Deserialize(ms);
        }

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

        public static void ArrayEquals<T>(IEnumerable<T> a, IEnumerable<T> b, bool cloneAndRecurse = true)
        {
            if (!Enumerable.SequenceEqual(a, b))
            {
                throw new AssertFailedException("Arrays are not equal" + (cloneAndRecurse ? "" : " (after cloning)"));
            }
            if (cloneAndRecurse) ArrayEquals(Clone(a), Clone(b), false);
        }

        public static void DictEquals<K, V>(IEnumerable<KeyValuePair<K, V>> a, IEnumerable<KeyValuePair<K, V>> b, bool cloneAndRecurse = true)
            where K : IComparable<K>
        {
            var orderedA = a.OrderBy(x => x.Key);
            var orderedB = b.OrderBy(x => x.Key);

            if (!Enumerable.SequenceEqual(orderedA, orderedB))
            {
                throw new AssertFailedException("Dictionaries are not equal" + (cloneAndRecurse ? "" : " (after cloning)"));
            }
            if (cloneAndRecurse) DictEquals(Clone(a), Clone(b), false);
        }

        public static void SetEquals<T>(IEnumerable<T> a, IEnumerable<T> b, bool cloneAndRecurse = true)
            where T : IComparable<T>
        {
            var orderedA = a.OrderBy(x => x);
            var orderedB = b.OrderBy(x => x);

            if (!Enumerable.SequenceEqual(orderedA, orderedB))
            {
                throw new AssertFailedException("Sets are not equal" + (cloneAndRecurse?"":" (after cloning)"));
            }
            if (cloneAndRecurse) SetEquals(Clone(a), Clone(b), false);
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
