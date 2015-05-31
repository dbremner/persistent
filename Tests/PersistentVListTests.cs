using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentCollections;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using PersistentCollections.PersistentVList;

namespace Tests
{
    [TestClass]
    public class PersistentVListTests
    {
        [TestMethod]
        public void ComplexRandomizedTest()
        {
            var maxVersions = 1000000;
            var v0 = PersistentVList<int>.Empty;

            Parallel.For(0, 10, l =>
            //for (int l = 0; l < 1; l++)
            {
                var r = new Random(l * 20);

                var minVersions = Math.Max(r.Next((int)(maxVersions * 0.001)), 10);

                var numOfVersions = r.Next(minVersions, maxVersions);
                var numOfTransients = r.Next(minVersions, Math.Max(numOfVersions >> 5, minVersions + 5));

                var transientAtVersions = new HashSet<int>(r.RandomArray(numOfVersions - minVersions, numOfTransients));
                numOfTransients = transientAtVersions.Count;

                numOfVersions += numOfTransients;

                var pArrays = new List<List<int>>(numOfVersions) { v0.ToList() };
                var tArrays = new List<List<int>>(numOfTransients);
                var versions = new List<PersistentVList<int>>(numOfVersions) { v0 };
                var transients = new List<TransientVList<int>>(numOfTransients);

                var activeTransients = new HashSet<Tuple<int, int>>();

                for (int i = 0; i < numOfVersions; i++)
                {
                    foreach (var idx in activeTransients.Select(x => x.Item1))
                    {
                        var arr = tArrays[idx];
                        var tr = transients[idx];
                        var actID = r.Next(100);

                        if (actID > 40)
                        {
                            var item = r.Next();
                            tr.Add(item);
                            arr.Add(item);
                        }
                        else if (actID < 10)
                        {
                            if (tr.Count == 0)
                            {
                                MyAssert.Throws<InvalidOperationException>(() => tr.RemoveLast());
                            }
                            else
                            {
                                tr.RemoveLast();
                                arr.RemoveAt(arr.Count - 1);
                            }
                        }
                        else
                        {
                            var index = r.Next(arr.Count + (arr.Count >> 2));
                            var item = r.Next();

                            if (tr.Count < index)
                            {
                                MyAssert.Throws<IndexOutOfRangeException>(() => tr[index] = item);
                            }
                            else
                            {
                                tr[index] = item;
                                if (arr.Count == index)
                                    arr.Add(item);
                                else
                                    arr[index] = item;
                            }
                        }

                        for (int c = 0; c < Math.Min(arr.Count, 3); c++)
                        {
                            var index = r.Next(arr.Count);
                            Assert.AreEqual(arr[index], tr[index]);
                        }

                        Assert.AreEqual(arr.Count, tr.Count);
                    }

                    if (transientAtVersions.Contains(i - minVersions))
                    {
                        // Creating persistent from transient

                        var idx = activeTransients.First(x => x.Item2 == i - minVersions).Item1;
                        var p = transients[idx].AsPersistent();

                        versions.Add(p);
                        pArrays.Add(p.ToList());

                        MyAssert.ArrayEquals(transients[idx], tArrays[idx]);

                        activeTransients.RemoveWhere(x => x.Item2 == i - minVersions);
                    }
                    else
                    {
                        // Performing random action

                        var v = versions[r.Next(0, versions.Count)];
                        var pArr = v.ToList();
                        var actID = r.Next(100);

                        if (actID > 33)
                        {
                            var item = r.Next();
                            pArr.Add(item);
                            v = v.Add(item);
                        }
                        else
                        {
                            if (pArr.Count == 0)
                            {
                                MyAssert.Throws<InvalidOperationException>(() => v.RemoveLast());
                            }
                            else
                            {
                                v = v.RemoveLast();
                                pArr.RemoveAt(pArr.Count - 1);
                            }
                        }
                        versions.Add(v);
                        pArrays.Add(pArr);
                    }

                    if (transientAtVersions.Contains(i))
                    {
                        // Creating transient

                        var v = versions[r.Next(0, versions.Count)];
                        var tArr = v.ToList();
                        var tr = v.AsTransient();

                        tArrays.Add(tArr);
                        transients.Add(tr);

                        activeTransients.Add(Tuple.Create(tArrays.Count - 1, i));
                    }
                }

                Assert.AreEqual(tArrays.Count, transients.Count);
                for (int i = 0; i < tArrays.Count; i++)
                {
                    var arr = tArrays[i];
                    var tr = transients[i];

                    Assert.AreEqual(tr.Count, arr.Count);
                    MyAssert.ArrayEquals(arr, tr);

                    for (int s = 0; s < Math.Min(arr.Count, 10); s++)
                    {
                        var idx = r.Next(arr.Count);
                        Assert.AreEqual(tr[idx], arr[idx]);
                    }

                    MyAssert.Throws<NotSupportedException>(() => tr.SetAt(0, 5));
                    MyAssert.Throws<NotSupportedException>(() => tr.Add(10));
                    MyAssert.Throws<NotSupportedException>(() => tr.RemoveLast());
                }

                Assert.AreEqual(pArrays.Count, versions.Count);
                for (int i = 0; i < pArrays.Count; i++)
                {
                    var arr = pArrays[i];
                    var v = versions[i];

                    Assert.AreEqual(v.Count, arr.Count);
                    MyAssert.ArrayEquals(arr, v);

                    for (int s = 0; s < Math.Min(arr.Count, 10); s++)
                    {
                        var idx = r.Next(arr.Count);
                        Assert.AreEqual(v[idx], arr[idx]);
                    }
                }
            });
        }

        [TestMethod]
        public void BasicFunctonality()
        {
            var v0 = PersistentVList<int>.Empty;

            var v1 = v0.Add(1, 2, 3, 4, 5);
            var v2 = v0.Add(2, 3, 4, 1);
            var v3 = v1.RemoveLast();
            var v4 = v3.Add(6);
            var v5 = v0.Add(0);
            var v6 = v5.RemoveLast();
            var v7 = v1.RemoveLast(3);
            var v8 = v1.RemoveLast(5);

            Assert.AreEqual(v0.Count, 0);
            Assert.AreEqual(v1.Count, 5);
            Assert.AreEqual(v2.Count, 4);
            Assert.AreEqual(v3.Count, 4);
            Assert.AreEqual(v4.Count, 5);
            Assert.AreEqual(v5.Count, 1);
            Assert.AreEqual(v6.Count, 0);
            Assert.AreEqual(v7.Count, 2);
            Assert.AreEqual(v8.Count, 0);

            Assert.AreEqual(v0, PersistentVList<int>.Empty);
            MyAssert.ArrayEquals(v1, new[] { 1, 2, 3, 4, 5 });
            MyAssert.ArrayEquals(v2, new[] { 2, 3, 4, 1 });
            MyAssert.ArrayEquals(v3, new[] { 1, 2, 3, 4 });
            MyAssert.ArrayEquals(v4, new[] { 1, 2, 3, 4, 6 });
            MyAssert.ArrayEquals(v5, new[] { 0 });
            Assert.AreEqual(v6, PersistentVList<int>.Empty);
            MyAssert.ArrayEquals(v7, new[] { 1, 2 });
            Assert.AreEqual(v8, PersistentVList<int>.Empty);

            Assert.AreEqual(v1[2], 3);
            Assert.AreEqual(v1[0], 1);
            Assert.AreEqual(v2[2], 4);
            Assert.AreEqual(v2[3], 1);
            Assert.AreEqual(v4[4], 6);
            Assert.AreEqual(v5[0], 0);

            MyAssert.Throws<IndexOutOfRangeException>(() => v4.GetAt(5));
            MyAssert.Throws<IndexOutOfRangeException>(() => v4.GetAt(-1));
            MyAssert.Throws<IndexOutOfRangeException>(() => v0.GetAt(0));
        }

        [TestMethod]
        public void Transient()
        {
            var tr = PersistentVList<int>.Empty.AsTransient();
            
            tr.Add(0);
            tr.Add(1, 2, 3, 4);
            tr.Add(5, 6, 7, 8, 9);

            Assert.AreEqual(tr.Count, 10);
            MyAssert.ArrayEquals(tr.ToArray(), new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            tr.RemoveLast();
            Assert.AreEqual(tr.Count, 9);
            MyAssert.ArrayEquals(tr, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

            tr[5] = -1;
            tr[8] = -1;

            Assert.AreEqual(tr.Count, 9);
            MyAssert.ArrayEquals(tr, new[] { 0, 1, 2, 3, 4, -1, 6, 7, -1 });

            Assert.AreEqual(tr[5], -1);
            MyAssert.Throws<IndexOutOfRangeException>(() => { var t = tr[20]; });

            tr[9] = 9;
            Assert.AreEqual(tr.Count, 10);
            MyAssert.ArrayEquals(tr, new[] { 0, 1, 2, 3, 4, -1, 6, 7, -1, 9 });
            
            tr.RemoveLast(5);

            Assert.AreEqual(tr.Count, 5);
            MyAssert.ArrayEquals(tr.ToArray(), new[] { 0, 1, 2, 3, 4 });

            tr.Add(new[] { 2, 3, 4 });
            Assert.AreEqual(tr[6], 3);

            Assert.AreEqual(tr.Count, 8);
            MyAssert.ArrayEquals(tr.ToArray(), new[] { 0, 1, 2, 3, 4, 2, 3, 4 });

            tr.RemoveLast(8);
            Assert.AreEqual(tr.Count, 0);

            var v0 = tr.AsPersistent();
            Assert.AreEqual(v0, PersistentVList<int>.Empty);

            MyAssert.Throws<NotSupportedException>(() => tr.SetAt(0, 5));
            MyAssert.Throws<NotSupportedException>(() => tr.Add(10));
            MyAssert.Throws<NotSupportedException>(() => tr.RemoveLast());
        }


        [TestMethod]
        public void MakingTransient()
        {
            var v0 = PersistentVList<int>.Empty;
            var v1 = v0.Add(1, 5, 8);
            var v2 = v1.Add(9, 8);

            var t1 = v1.AsTransient();
            var t2 = v1.AsTransient();

            var v3 = v1.RemoveLast(2);

            t1.Add(30);
            t1[0] = 9;

            Assert.AreEqual(t1.Count, 4);
            Assert.AreEqual(t2.Count, 3);
            Assert.AreEqual(v3.Count, 1);

            MyAssert.ArrayEquals(v1, new[] { 1, 5, 8 });
            MyAssert.ArrayEquals(t1, new[] { 9, 5, 8, 30 });
            MyAssert.ArrayEquals(t2, new[] { 1, 5, 8 });
            MyAssert.ArrayEquals(v3, new[] { 1 });

            t2.Add(8, 9);
            MyAssert.ArrayEquals(t2, new[] { 1, 5, 8, 8, 9 });

            var v4 = t1.AsPersistent();

            MyAssert.Throws<NotSupportedException>(() => t1.SetAt(0, 5));
            MyAssert.Throws<NotSupportedException>(() => t1.Add(10));
            MyAssert.Throws<NotSupportedException>(() => t1.RemoveLast());

            t2[2] = 3;
            t2.RemoveLast();
            t2.Add(89);

            MyAssert.ArrayEquals(t2, new[] { 1, 5, 3, 8, 89 });

            t2[0] = 0;
            t2.RemoveLast(4);
            t2.Add(1, 2);

            MyAssert.ArrayEquals(v1, new[] { 1, 5, 8 });
            MyAssert.ArrayEquals(t2, new[] { 0, 1, 2 });

            var v5 = t2.AsPersistent();

            MyAssert.Throws<NotSupportedException>(() => t2.SetAt(0, 5));
            MyAssert.Throws<NotSupportedException>(() => t2.Add(10));
            MyAssert.Throws<NotSupportedException>(() => t2.RemoveLast());

            var v6 = v5.Add(4);
            MyAssert.ArrayEquals(v6, new[] { 0, 1, 2, 4 });
            MyAssert.ArrayEquals(v5, new[] { 0, 1, 2 });
            MyAssert.ArrayEquals(v4, new[] { 9, 5, 8, 30 });
        }


        [TestMethod]
        public void EqualityTest()
        {
            var v0 = PersistentVList<int>.Empty;

            var v1 = v0.Add(5, 4, 3);
            var v2 = v0.Add(5).Add(4);
            var v3 = v2.Add(3);
            var v4 = v1.RemoveLast();
            var v5 = v3.RemoveLast();
            var v6 = v4.RemoveLast(2);
            var v7 = v5.RemoveLast(2);

            Assert.AreNotEqual(v1, v2);
            Assert.IsTrue(v1 != v2);
            Assert.AreEqual(v1, v3);
            Assert.IsTrue(v1 == v3);
            Assert.AreEqual(v4, v5);
            Assert.AreEqual(v6, v7);
            Assert.AreEqual(v1.GetHashCode(), v3.GetHashCode());
            Assert.AreEqual(v4.GetHashCode(), v5.GetHashCode());
            Assert.AreEqual(v6.GetHashCode(), v7.GetHashCode());
        }

        [TestMethod]
        public void BiggerEqualityTest()
        {
            Parallel.For(0, 100, l =>
            {
                var r = new Random(5544 * l);
                var randomArray = r.RandomArray(int.MaxValue, r.Next(1000000));

                var v0 = randomArray.ToPersistentVList();
                var v1 = randomArray.ToPersistentVList();

                Assert.AreEqual(v0, v1);
                Assert.AreEqual(v0.GetHashCode(), v1.GetHashCode());

                var v7 = v0.RemoveLast(5);
                var v8 = v0.RemoveLast(5);
                Assert.AreEqual(v7, v8);
                Assert.AreEqual(v7.GetHashCode(), v8.GetHashCode());

                var vals = r.RandomArray(int.MaxValue, v0.Count);

                var v2 = v0;

                for (int i = 0; i < vals.Length; i++)
                {
                    v0 = v0.Add(vals[i]);
                    v1 = v1.Add(vals[i]);
                    v2 = v2.Add(vals[i]);
                }

                Assert.AreEqual(v0, v1);
                Assert.AreEqual(v0, v2);
                Assert.IsTrue(v0 == v1);
                Assert.IsTrue(v0 == v2);
                Assert.AreEqual(v0.GetHashCode(), v1.GetHashCode());
                Assert.AreEqual(v0.GetHashCode(), v2.GetHashCode());

                var v5 = v0.Add(2);
                var v6 = v2.RemoveLast();
                Assert.AreNotEqual(v5, v1);
                Assert.AreNotEqual(v1, v6);
                Assert.IsTrue(v5 != v1);
                Assert.IsTrue(v6 != v1);
                Assert.AreNotEqual(v5.GetHashCode(), v1.GetHashCode());
                Assert.AreNotEqual(v1.GetHashCode(), v6.GetHashCode());

                v0 = v0.Add(Enumerable.Range(0, 1024));
                v2 = v2.RemoveLast(r.Next(v0.Count >> 1));
                Assert.AreNotEqual(v0.GetHashCode(), v1.GetHashCode());
                Assert.AreNotEqual(v1.GetHashCode(), v2.GetHashCode());
            });
        }
    }
}
