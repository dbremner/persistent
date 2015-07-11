using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersistentCollections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests;
namespace PersistentCollections.Tests
{
    [TestClass()]
    public class PersistentDictionaryTests
    {
        #region Auxiliary classes
        [Serializable]
        internal class ManyCollisions : IEquatable<ManyCollisions>, IComparable<ManyCollisions>
        {
            internal int Key { get; private set; }
            private int max;
            private int hash;

            public ManyCollisions(int key, int max = 20)
            {
                this.Key = key;
                this.max = max;
            }

            public override int GetHashCode()
            {
                if (hash == 0)
                {
                    hash = (new Random(Key)).Next(max);
                    if (hash == 0) hash = 1;
                }

                return hash;
            }

            public bool Equals(ManyCollisions other)
            {
                return Key == other.Key;
            }

            public override bool Equals(object obj)
            {
                var other = obj as ManyCollisions;

                return other != null && Equals(other);
            }

            public int CompareTo(ManyCollisions other)
            {
                return Key.CompareTo(other.Key);
            }
        }

        [Serializable]
        internal class LongPrefixHash
        {
            internal int Key { get; private set; }

            public LongPrefixHash(int key)
            {
                this.Key = key;
            }

            public override int GetHashCode()
            {
                var r = new Random(Key);
                var hash = (UInt32)r.Next(32);

                return (int)(hash << 20);
            }

            public bool Equals(LongPrefixHash other)
            {
                return Key == other.Key;
            }

            public override bool Equals(object obj)
            {
                var other = obj as LongPrefixHash;

                return other != null && Equals(other);
            }
        }

        #endregion

        [TestMethod]
        public void ComplexRandomizedTest()
        {
            var maxVersions = 100000;
            var v0 = PersistentDictionary<ManyCollisions, int>.Empty;

            Parallel.For(0, 5, l =>
            //for (int l = 0; l < 50; l++)
            {
                var r = new Random(l * 20);
                var minVersions = Math.Max(r.Next((int)(maxVersions * 0.001)), 10);

                var numOfVersions = r.Next(minVersions, maxVersions);
                var numOfTransients = r.Next(minVersions, Math.Max(numOfVersions >> 5, minVersions + 5));

                var transientAtVersions = new HashSet<int>(r.RandomArray(numOfVersions - minVersions, numOfTransients));
                numOfTransients = transientAtVersions.Count;

                numOfVersions += numOfTransients;
                var maxHashes = r.Next(numOfVersions * 2);

                Func<int, ManyCollisions> Key = x => new ManyCollisions(x, maxHashes);

                var pArrays = new List<Dictionary<ManyCollisions, int>>(numOfVersions) 
                    { v0.ToDictionary(x => x.Key, x => x.Value) };
                var tArrays = new List<Dictionary<ManyCollisions, int>>(numOfTransients);
                var versions = new List<PersistentDictionary<ManyCollisions, int>>(numOfVersions) { v0 };
                var transients = new List<TransientDictionary<ManyCollisions, int>>(numOfTransients);

                var activeTransients = new HashSet<Tuple<int, int>>();

                for (int i = 0; i < numOfVersions; i++)
                {
                    foreach (var idx in activeTransients.Select(x => x.Item1))
                    {
                        var arr = tArrays[idx];
                        var tr = transients[idx];
                        var actID = r.Next(100);
                        
                        if (actID > 20 || arr.Count == 0)
                        {
                            var item = r.Next();
                            var key = Key(r.Next());

                            tr.Add(key, item);
                            arr.Add(key, item);
                        }
                        else
                        {
                            var key = arr.Keys.FirstOrDefault();

                            tr.Remove(key);
                            arr.Remove(key);
                        }

                        foreach (var item in arr.Take(3))
                        {
                            Assert.AreEqual(arr[item.Key], tr[item.Key]);
                        }

                        Assert.AreEqual(arr.Count, tr.Count);
                    }

                    if (transientAtVersions.Contains(i - minVersions))
                    {
                        // Creating persistent from transient

                        var idx = activeTransients.First(x => x.Item2 == i - minVersions).Item1;
                        var p = transients[idx].AsPersistent();
                        MyAssert.CloneEquals(p);

                        versions.Add(p);
                        pArrays.Add(p.ToDictionary(x => x.Key, x => x.Value));

                        activeTransients.RemoveWhere(x => x.Item2 == i - minVersions);
                    }
                    else
                    {
                        // Performing random action

                        var v = versions[r.Next(0, versions.Count)];
                        var pArr = v.ToDictionary(x => x.Key, x => x.Value);
                        var actID = r.Next(100);

                        if (actID > 20 || v.Count == 0)
                        {
                            var item = r.Next();
                            var key = Key(r.Next());

                            v = v.Add(key, item);
                            pArr.Add(key, item);
                        }
                        else
                        {
                            var key = pArr.Keys.FirstOrDefault();

                            v = v.Remove(key);
                            pArr.Remove(key);
                        }

                        versions.Add(v);
                        pArrays.Add(pArr);
                    }

                    if (transientAtVersions.Contains(i))
                    {
                        // Creating transient

                        var v = versions[r.Next(0, versions.Count)];
                        var tArr = v.ToDictionary(x => x.Key, x => x.Value);
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
                    MyAssert.DictEquals(arr, tr, false);

                    foreach (var item in arr.Take(Math.Max(arr.Count >> 8, 10)))
                    {
                        var idx = item.Key;
                        Assert.AreEqual(tr[idx], arr[idx]);
                    }

                    MyAssert.Throws<NotSupportedException>(() => tr.Remove(Key(0)));
                    MyAssert.Throws<NotSupportedException>(() => tr.Add(Key(0), 10));
                }

                Assert.AreEqual(pArrays.Count, versions.Count);
                for (int i = 0; i < pArrays.Count; i++)
                {
                    var arr = pArrays[i];
                    var v = versions[i];
                    MyAssert.CloneEquals(v);

                    Assert.AreEqual(v.Count, arr.Count);
                    MyAssert.DictEquals(arr, v);

                    foreach (var item in arr.Take(Math.Max(arr.Count >> 8, 10)))
                    {
                        var idx = item.Key;
                        Assert.AreEqual(v[idx], arr[idx]);
                    }
                }
            });
        }

        [TestMethod()]
        public void PersistentDictionaryTest()
        {
            var pd = PersistentDictionary<string, int>.Empty;
            var pd2 = PersistentDictionary<string, int>.Empty;

            Assert.AreEqual(pd.Count, 0);
            Assert.AreEqual(pd2.Count, 0);
            Assert.AreEqual(pd, pd2);
            Assert.AreEqual(pd.GetHashCode(), pd2.GetHashCode());
            Assert.AreEqual(pd.Any(), false);
        }

        [TestMethod()]
        public void ManyCollisionsTest()
        {
            foreach (var num in new[] { 1, 2, 3, 7, 8, 32, 65, 45, 130, 2000 })
            {
                Func<int, ManyCollisions> Key = x => new ManyCollisions(x);
                var pd = PersistentDictionary<ManyCollisions, int>.Empty;

                for (int i = 0; i < num; i++)
                {
                    pd = pd.Add(Key(i), i);
                }
                Assert.AreEqual(pd.Count, num);
                Assert.AreEqual(pd.ToList().Count, num);

                foreach (var item in pd)
                {
                    Assert.AreEqual(item.Key.Key, item.Value);
                }

                int idx = 0;
                foreach(var item in pd.OrderBy(x => x.Key.Key)) 
                {
                    Assert.AreEqual(idx++, item.Value);
                }

                foreach (var item in pd)
                {
                    Assert.IsTrue(pd.Contains(item.Value));
                }

                for (int i = 0; i < pd.Count; i++)
                {
                    Assert.IsTrue(pd.Contains(i));
                    Assert.IsTrue(pd.ContainsKey(Key(i)));
                }

                for (int i = num; i < 50; i++)
                {
                    Assert.IsFalse(pd.Contains(i));
                    Assert.IsFalse(pd.ContainsKey(Key(i)));
                }

                var index = new Random(30).Next(num);

                var changed = pd.Add(Key(index), -1);
                Assert.IsFalse(pd.Equals(changed));

                changed = changed.Add(Key(index), index);
                Assert.IsTrue(pd.Equals(changed));

                MyAssert.CloneEquals(pd);
            }
        }

        [TestMethod()]
        public void ItemsWithLongPrefixTest()
        {
            foreach (var num in new[] { 1, 2, 3, 7, 8, 32, 65, 45, 130, 2000 })
            {
                Func<int, LongPrefixHash> Key = x => new LongPrefixHash(x);
                var pd = PersistentDictionary<LongPrefixHash, int>.Empty;

                for (int i = 0; i < num; i++)
                {
                    pd = pd.Add(Key(i), i);
                }
                Assert.AreEqual(pd.Count, num);
                Assert.AreEqual(pd.ToList().Count, num);
                MyAssert.CloneEquals(pd);

                foreach (var item in pd)
                {
                    Assert.AreEqual(item.Key.Key, item.Value);
                }

                int idx = 0;
                foreach (var item in pd.OrderBy(x => x.Key.Key))
                {
                    Assert.AreEqual(idx++, item.Value);
                }

                foreach (var item in pd)
                {
                    Assert.IsTrue(pd.Contains(item.Value));
                }

                for (int i = 0; i < pd.Count; i++)
                {
                    Assert.IsTrue(pd.Contains(i));
                    Assert.IsTrue(pd.ContainsKey(Key(i)));
                }

                for (int i = num; i < 50; i++)
                {
                    Assert.IsFalse(pd.Contains(i));
                    Assert.IsFalse(pd.ContainsKey(Key(i)));
                }

                var index = new Random(30).Next(num);

                var changed = pd.Add(Key(index), -1);
                Assert.IsFalse(pd.Equals(changed));

                changed = changed.Add(Key(index), index);
                Assert.IsTrue(pd.Equals(changed));
            }
        }

        [TestMethod()]
        public void LongComplexTest()
        {
            Parallel.For(0, 50, i =>
            {
                var r = new Random(i * 6077);
                int bigger = 20 + r.Next(10000);
                int smaller = 20 + r.Next(10000);
                int num = smaller * 5 + r.Next(30000);
                int numOfHashCodes = r.Next(20 + i * 10 + num);

                HugeComplexTest(bigger, smaller, num, numOfHashCodes);
            });
        }

        private void HugeComplexTest(int bigger, int smaller, int num, int nh)
        {
            Func<int, ManyCollisions> Key = x => new ManyCollisions(x, nh);
            var pd = PersistentDictionary<ManyCollisions, int>.Empty;
            var r = new Random(981);

            for (int i = 0; i < smaller; i++)
            {
                int n = r.Next(num);

                while (pd.ContainsKey(Key(n)))
                {
                    Assert.AreEqual(pd[Key(n)], -1);
                    n = r.Next(num);
                }

                pd = pd.Add(Key(n), -1);
            }

            for (int i = 0; i < bigger; i++)
            {
                int n = r.Next(num);

                for (; pd.ContainsKey(Key(num + n)); n++) ;

                pd = pd.Add(Key(num + n), -1);

                Assert.AreEqual(pd.Count, smaller + i + 1);
            }

            var tr = pd.AsTransient();

            for (int i = 0; i < num; i++)
            {
                tr.Add(Key(i), i);
            }

            Assert.AreEqual(pd.Count, smaller + bigger);
            Assert.AreEqual(tr.Count, num + bigger);
            MyAssert.CloneEquals(pd);

            foreach (var item in tr)
            {
                if (item.Key.Key < num)
                    Assert.AreEqual(item.Key.Key, item.Value);
                else
                    Assert.AreNotEqual(item.Key.Key, item.Value);
            }

            foreach (var item in pd)
            {
                Assert.AreNotEqual(item.Key.Key, item.Value);
            }

            var pt = tr.AsPersistent();

            MyAssert.Throws<NotSupportedException>(() => tr.Add(Key(0), 0));
            pt = pd.Add(Key(-4), 4);

            Assert.IsFalse(tr.ContainsKey(Key(-5)));
            Assert.IsTrue(pt.ContainsKey(Key(-4)));
        }

        [TestMethod]
        public void BiggerDictionary()
        {
            var v0 = PersistentDictionary<ManyCollisions, int>.Empty;

            Parallel.For(0, 50, l =>
            //for (int l = 0; l < 50; l++)
            {
                var r = new Random(l * 549);
                var min = r.Next(200, 1000);
                var n = r.Next(min, 100000);
                var maxHashes = r.Next(n * 5);
                Func<int, ManyCollisions> Key = x => new ManyCollisions(x, maxHashes);

                var d1 = Enumerable.Range(0, n).ToDictionary(x => Key(x), x => x);
                var v1 = v0.Add(Enumerable.Range(0, n), k => Key(k), v => v);

                var v2 = v1;
                var d4 = d1.ToDictionary(x => x.Key, x => x.Value);
                var rmIdxs = r.RandomArray(n, r.Next(n - min)).Distinct();
                foreach (var idx in rmIdxs)
                {
                    var key = Key(idx);

                    v2 = v2.Remove(key);
                    d4.Remove(key);
                }

                var idxs1 = r.RandomArray(n, r.Next(n));
                var idxs2 = r.RandomArray(min, r.Next(v2.Count));

                var vals1 = r.RandomArray(n, idxs1.Length);
                var vals2 = r.RandomArray(min, idxs2.Length);

                var d2 = d1.ToDictionary(x => x.Key, x => x.Value);
                var d3 = d4.ToDictionary(x => x.Key, x => x.Value);

                var v3 = v1;
                var v4 = v2;
                for (int i = 0; i < idxs1.Length; i++)
                {
                    var key = Key(idxs1[i]);

                    v3 = v3.Add(key, vals1[i]);
                    d2[key] = vals1[i];
                    Assert.AreEqual(v3.Count, d2.Count);
                }

                for (int i = 0; i < idxs2.Length; i++)
                {
                    var key = Key(idxs2[i]);

                    v4 = v4.Add(key, vals2[i]);
                    d3[key] = vals2[i];
                }

                MyAssert.DictEquals(v1, d1);
                MyAssert.DictEquals(v2, d4);

                MyAssert.DictEquals(v3, d2);
                MyAssert.DictEquals(v4, d3);

                MyAssert.CloneEquals(v1);
                MyAssert.CloneEquals(v2);
                MyAssert.CloneEquals(v3);
                MyAssert.CloneEquals(v4);
            });
        }

        [TestMethod]
        public void Transient()
        {
            var tr = PersistentDictionary<int, int>.Empty.AsTransient();

            tr.Add(0, 0);
            tr.Add(new[] { 1, 2, 3, 4 }, k => k, v => v);
            tr.Add(new[] { 5, 6, 7, 8, 9 }, k => k, v => v);

            Assert.AreEqual(tr.Count, 10);
            MyAssert.DictEquals(tr, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.ToDictionary(x => x, x => x), false);

            var dict = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.ToDictionary(x => x, x => x);

            tr.Remove(9);
            Assert.AreEqual(tr.Count, 9);
            MyAssert.DictEquals(tr, dict, false);

            tr[5] = -1;
            tr[8] = -1;
            dict[5] = -1;
            dict[8] = -1;

            Assert.AreEqual(tr.Count, 9);
            MyAssert.DictEquals(tr, dict, false);

            Assert.AreEqual(tr[5], -1);
            MyAssert.Throws<KeyNotFoundException>(() => tr.Remove(20));

            tr[9] = 9;
            dict[9] = 9;
            Assert.AreEqual(tr.Count, 10);
            MyAssert.DictEquals(tr, dict, false);

            for (int i = 0; i < 10; i++) tr.Remove(i);
            Assert.AreEqual(tr.Count, 0);

            var v0 = tr.AsPersistent();
            Assert.AreEqual(v0, PersistentDictionary<int, int>.Empty);

            MyAssert.Throws<NotSupportedException>(() => tr.Add(0, 5));
            MyAssert.Throws<NotSupportedException>(() => tr.Add(10, 10));
            MyAssert.Throws<NotSupportedException>(() => tr.Remove(5));
        }

        [TestMethod()]
        public void Persistence()
        {
            var v0 = PersistentDictionary<int, int>.Empty;
            var v1 = v0.Add(0, 0);
            var v2 = new[] {0, 1, 2, 3, 4, 5, 6, 7}.ToPersistentDictionary(k => k, v => v);
            var v3 = v1.Add(8, 8).Add(9, 9);
            var v4 = v2.Remove(2).Remove(6);
            var v5 = v4.Add(10, 10);
            var v6 = v4.Remove(3);
            var v7 = v6.Remove(0);
            var v8 = v2.Add(5, -1);
            var v9 = v2.Add(8, 8);
            var v10 = v1.Remove(0);

            MyAssert.Throws<KeyNotFoundException>(() => v0.Remove(1));
            MyAssert.Throws<KeyNotFoundException>(() => v2.Remove(10));

            Assert.AreEqual(v0.Count, 0);
            Assert.AreEqual(v1.Count, 1);
            Assert.AreEqual(v2.Count, 8);
            Assert.AreEqual(v3.Count, 3);
            Assert.AreEqual(v4.Count, 6);
            Assert.AreEqual(v5.Count, 7);
            Assert.AreEqual(v6.Count, 5);
            Assert.AreEqual(v7.Count, 4);
            Assert.AreEqual(v8.Count, 8);
            Assert.AreEqual(v9.Count, 9);
            Assert.AreEqual(v10.Count, 0);

            MyAssert.CloneEquals(v1);
            MyAssert.CloneEquals(v2);
            MyAssert.CloneEquals(v3);
            MyAssert.CloneEquals(v4);
            MyAssert.CloneEquals(v5);
            MyAssert.CloneEquals(v6);
            MyAssert.CloneEquals(v7);
            MyAssert.CloneEquals(v8);
            MyAssert.CloneEquals(v9);
            MyAssert.CloneEquals(v10);
            
            Assert.AreEqual(v0, PersistentDictionary<int, int>.Empty);
            MyAssert.DictEquals(v1, new[] { 0 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v2.ToArray(), new[] { 0, 1, 2, 3, 4, 5, 6, 7 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v3.ToArray(), new[] { 0, 8, 9 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v4.ToArray(), new[] { 0, 1, 3, 4, 5, 7 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v5.ToArray(), new[] { 0, 1, 3, 4, 5, 7, 10 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v6.ToArray(), new[] { 0, 1, 4, 5, 7 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v7.ToArray(), new[] { 1, 4, 5, 7 }.ToDictionary(k => k, v => v));
            MyAssert.DictEquals(v8.ToArray(), new[] { 0, 1, 2, 3, 4, -1, 6, 7 }.ToDictionary(k => (k == -1) ? 5 : k, v => v));
            MyAssert.DictEquals(v9.ToArray(), new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.ToDictionary(k => k, v => v));
            Assert.AreEqual(v10, PersistentDictionary<int, int>.Empty);

            Assert.AreEqual(v8[5], -1);
            Assert.AreEqual(v8[2], 2);
            Assert.AreEqual(v9[8], 8);

            MyAssert.Throws<KeyNotFoundException>(() => v2.Remove(10));
        }

        [TestMethod()]
        public void RemoveTest()
        {
            var v0 = PersistentDictionary<int, int>.Empty;
            var v1 = v0.Add(96, 96);
            var v2 = v1.Add(608, 608);
            var rm = v2.Remove(96);
            var v3 = v2.Remove(96).Remove(608);
            Assert.IsTrue(rm.ContainsKey(608));
            Assert.IsFalse(rm.ContainsKey(96));
            MyAssert.Throws<KeyNotFoundException>(() => rm.Remove(96));
            Assert.AreEqual(v0, v3);
            Assert.AreEqual(v1.Count, 1);
            Assert.AreEqual(v2.Count, 2);

            var v4 = v2.Add(10240, 10240);
            rm = v2.Remove(96);

            MyAssert.CloneEquals(v1);
            MyAssert.CloneEquals(v2);
            MyAssert.CloneEquals(v3);
            MyAssert.CloneEquals(v4);
        }

        [TestMethod]
        public void EqualityTest()
        {
            var v0 = PersistentDictionary<int, int>.Empty;

            var v1 = v0.Add(5, 5).Add(4, 4).Add(3, 3);
            var v2 = v0.Add(5, 5).Add(4, 4);
            var v3 = v2.Add(3, 3);
            var v4 = v1.Remove(4);
            var v5 = v3.Remove(4);
            var v6 = v4.Remove(5).Remove(3);
            var v7 = v5.Remove(3).Remove(5);

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
        public void BiggerEqualityTest()
        {
            Parallel.For(0, 10, l =>
            {
                var r = new Random(5544 * l);
                var randomArray = r.RandomArray(int.MaxValue, r.Next(100000));
                var maxHashes = r.Next(randomArray.Length*2);

                Func<int, ManyCollisions> Key = x => new ManyCollisions(x, maxHashes);

                var v0 = randomArray.ToPersistentDictionary(x => Key(x), x => x);
                var v1 = randomArray.ToPersistentDictionary(x => Key(x), x => x);

                Assert.AreEqual(v0, v1);
                Assert.AreEqual(v0.GetHashCode(), v1.GetHashCode());

                var idx = r.Next(v0.Count);
                var v7 = v0.Add(Key(idx), -1);
                var v8 = v0.Add(Key(idx), -1);
                Assert.AreEqual(v7, v8);
                Assert.AreEqual(v7.GetHashCode(), v8.GetHashCode());

                var idxs = r.RandomArray(int.MaxValue, v0.Count);
                var vals = r.RandomArray(int.MaxValue, idxs.Length);

                var v2 = v0;

                for (int i = 0; i < idxs.Length; i++)
                {
                    v0 = v0.Add(Key(idxs[i]), vals[i]);
                    v1 = v1.Add(Key(idxs[i]), vals[i]);
                    v2 = v2.Add(Key(idxs[i]), vals[i]);
                }

                Assert.AreEqual(v0, v1);
                Assert.AreEqual(v0, v2);
                Assert.IsTrue(v0 == v1);
                Assert.IsTrue(v0 == v2);
                Assert.AreEqual(v0.GetHashCode(), v1.GetHashCode());
                Assert.AreEqual(v0.GetHashCode(), v2.GetHashCode());

                var v5 = v0.Add(Key(-1), -1);
                var v6 = v2.Remove(v2.First().Key);
                Assert.AreNotEqual(v5, v1);
                Assert.AreNotEqual(v1, v6);
                Assert.IsTrue(v5 != v1);
                Assert.IsTrue(v1 != v6);
                Assert.AreNotEqual(v5.GetHashCode(), v1.GetHashCode());
                Assert.AreNotEqual(v1.GetHashCode(), v6.GetHashCode());

                MyAssert.CloneEquals(v1);
                MyAssert.CloneEquals(v2);
                MyAssert.CloneEquals(v5);
                MyAssert.CloneEquals(v6);
            });
        }
    }
}
