using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentCollections;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class PersistentQueueTests
    {
        [TestMethod]
        public void Persistence()
        {
            var v0 = PersistentQueue<int>.Empty;

            var v1 = v0.Enqueue(0);
            var v2 = v1.Enqueue(new[] {1, 2, 3, 4, 5, 6, 7});
            var v3 = v1.Enqueue(new[] { 8, 9 });
            var v4 = v2.Dequeue().Dequeue();
            var v5 = v4.Enqueue(0);
            var v6 = v4.Dequeue();
            var v7 = v1.Dequeue();

            Assert.AreEqual(v0.Count(), 0);
            Assert.AreEqual(v1.Count(), 1);
            Assert.AreEqual(v2.Count(), 8);
            Assert.AreEqual(v3.Count(), 3);
            Assert.AreEqual(v4.Count(), 6);
            Assert.AreEqual(v5.Count(), 7);
            Assert.AreEqual(v6.Count(), 5);
            Assert.AreEqual(v7.Count(), 0);

            Assert.AreEqual(v0, PersistentQueue<int>.Empty);
            MyAssert.ArrayEquals(v1.ToArray(), new[] { 0 });
            MyAssert.ArrayEquals(v2.ToArray(), new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            MyAssert.ArrayEquals(v3.ToArray(), new[] { 0, 8, 9 });
            MyAssert.ArrayEquals(v4.ToArray(), new[] { 2, 3, 4, 5, 6, 7 });
            MyAssert.ArrayEquals(v5.ToArray(), new[] { 2, 3, 4, 5, 6, 7, 0 });
            MyAssert.ArrayEquals(v6.ToArray(), new[] { 3, 4, 5, 6, 7 });
            Assert.AreEqual(v7, PersistentQueue<int>.Empty);

            MyAssert.CloneEquals(v1);
            MyAssert.CloneEquals(v2);
            MyAssert.CloneEquals(v3);
            MyAssert.CloneEquals(v4);
            MyAssert.CloneEquals(v5);
            MyAssert.CloneEquals(v6);
            MyAssert.CloneEquals(v7);

        }

        [TestMethod]
        public void EqualityTest()
        {
            var v0 = PersistentQueue<int>.Empty;

            var v1 = v0.Enqueue(5).Enqueue(4).Enqueue(3);
            var v2 = v0.Enqueue(5).Enqueue(4);
            var v3 = v2.Enqueue(3);
            var v4 = v1.Dequeue();
            var v5 = v3.Dequeue();
            var v6 = v4.Dequeue().Dequeue();
            var v7 = v5.Dequeue().Dequeue();

            Assert.AreNotEqual(v1, v2);
            Assert.IsTrue(v1 != v2);
            Assert.AreEqual(v1, v3);
            Assert.IsTrue(v1 == v3);
            Assert.AreEqual(v4, v5);
            Assert.AreEqual(v6, v7);
            Assert.AreEqual(v1.GetHashCode(), v3.GetHashCode());
            Assert.AreEqual(v4.GetHashCode(), v5.GetHashCode());
            Assert.AreEqual(v6.GetHashCode(), v7.GetHashCode());

            MyAssert.CloneEquals(v1);
            MyAssert.CloneEquals(v2);
            MyAssert.CloneEquals(v3);
            MyAssert.CloneEquals(v4);
            MyAssert.CloneEquals(v5);
            MyAssert.CloneEquals(v6);
            MyAssert.CloneEquals(v7);
        }

        [TestMethod]
        public void ComplexRandomizedTest()
        {
            var maxVersions = 100000;
            var v0 = PersistentQueue<int>.Empty;

            Parallel.For(0, 100, l =>
            {
                var r = new Random(l * 20);

                var minVersions = Math.Max(r.Next((int)(maxVersions * 0.001)), 10);
                var numOfVersions = r.Next(minVersions, maxVersions);
               
                var pArrays = new List<List<int>>(numOfVersions) { v0.ToList() };
                var versions = new List<PersistentQueue<int>>(numOfVersions) { v0 };


                for (int i = 0; i < numOfVersions; i++)
                {
                    // Performing random action

                    var v = versions[r.Next(0, versions.Count)];
                    var pArr = v.ToList();
                    var actID = r.Next(100);

                    if (actID > 40)
                    {
                        var item = r.Next();
                        pArr.Add(item);
                        v = v.Enqueue(item);
                    }
                    else
                    {
                        if (pArr.Count == 0)
                        {
                            MyAssert.Throws<Exception>(() => v.Dequeue());
                        }
                        else
                        {
                            v = v.Dequeue();
                            pArr.RemoveAt(0);
                        }
                    }
                }

                Assert.AreEqual(pArrays.Count, versions.Count);
                for (int i = 0; i < pArrays.Count; i++)
                {
                    var arr = pArrays[i];
                    var v = versions[i];
                    MyAssert.CloneEquals(v);
                    
                    MyAssert.ArrayEquals(arr.ToArray(), v.ToArray());
                }
            });
        }
    }
}
