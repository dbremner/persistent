using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentCollections;
using System.Linq;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class PersistentHashSetTest
    {
        [TestMethod]
        public void Transient()
        {
            var tr = PersistentHashSet<int>.Empty.AsTransient();

            tr.Add(0);
            tr.Add(1, 2, 3, 4);
            tr.Add(5, 6, 7, 8, 9, 5, 6, 7);

            Assert.AreEqual(tr.Count, 10);
            MyAssert.SetEquals(tr, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            tr.Remove(5);
            Assert.AreEqual(tr.Count, 9);
            MyAssert.SetEquals(tr, new[] { 0, 1, 2, 3, 4, 6, 7, 8, 9 });

            MyAssert.Throws<KeyNotFoundException>(() => tr.Remove(10));
            MyAssert.Throws<KeyNotFoundException>(() => tr.Remove(-1));

            tr.Add(new[] { 2, 3, 4, 5 });

            Assert.AreEqual(tr.Count, 10);
            MyAssert.SetEquals(tr, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            tr.Remove(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Assert.AreEqual(tr.Count, 0);

            var v0 = tr.AsPersistent();
            Assert.AreEqual(v0, PersistentHashSet<int>.Empty);

            MyAssert.Throws<NotSupportedException>(() => tr.Add(0));
            MyAssert.Throws<NotSupportedException>(() => tr.Remove(0));
        }

        [TestMethod]
        public void BasicFunctionality()
        {
            var v0 = PersistentHashSet<int>.Empty;

            var v1 = v0.Add(0);
            var v2 = v1.Add(1, 2, 3, 4, 5, 6, 7);
            var v3 = v1.Add(new[] { 0, 8, 9 });
            var v4 = v2.Remove(2);
            var v5 = v4.Add(0);
            var v6 = v4.Remove(5);
            var v7 = v6.Add(1, 2, 3, 4, 5);
            var v8 = v1.Remove(0);
            var v9 = v8.Add(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            Assert.AreEqual(v0.Count, 0);
            Assert.AreEqual(v1.Count, 1);
            Assert.AreEqual(v2.Count, 8);
            Assert.AreEqual(v3.Count, 3);
            Assert.AreEqual(v4.Count, 7);
            Assert.AreEqual(v5.Count, 7);
            Assert.AreEqual(v6.Count, 6);
            Assert.AreEqual(v7.Count, 8);
            Assert.AreEqual(v8.Count, 0);
            Assert.AreEqual(v9.Count, 1);

            Assert.AreEqual(v0, PersistentHashSet<int>.Empty);
            MyAssert.SetEquals(v1, new[] { 0 });
            MyAssert.SetEquals(v2, new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            MyAssert.SetEquals(v3, new[] { 0, 8, 9 });
            MyAssert.SetEquals(v4, new[] { 0, 1, 3, 4, 5, 6, 7 });
            MyAssert.SetEquals(v5, new[] { 0, 1, 3, 4, 5, 6, 7 });
            MyAssert.SetEquals(v6, new[] { 0, 1, 3, 4, 6, 7 });
            MyAssert.SetEquals(v7, new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            Assert.AreEqual(v8, PersistentHashSet<int>.Empty);
            MyAssert.SetEquals(v9, new[] { 0 });

            Assert.IsFalse(v8.Contains(5));
            Assert.IsFalse(v3.Contains(5));
            Assert.IsFalse(v5.Contains(2));

            Assert.IsTrue(v1.Contains(0));
            Assert.IsTrue(v6.Contains(6));
            Assert.IsTrue(v7.Contains(5));

            Assert.IsTrue(v4 == v5);
            Assert.IsTrue(v2 == v7);
            Assert.IsTrue(v0 == v8);
            Assert.IsTrue(v1 == v9);

            Assert.IsTrue(v0 != v7);
            Assert.IsTrue(v3 != v0);
            Assert.IsTrue(v6 != v5);
            Assert.IsTrue(v6 != v1);
        }
    }
}
