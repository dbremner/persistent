using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class BenchmarkList
    {
        private const int N = 100000;

        public static void Perform(Benchmark benchmark)
        {
            Utils.WriteHeader("List", N, benchmark);

            AddTest(benchmark);
            SetAtTest(benchmark);
            RemoveTest(benchmark);
            GetAtTest(benchmark);

            AddTransientTest(benchmark);
            SetAtTransientTest(benchmark);
            RemoveTransientTest(benchmark);
        }

        public static void MemoryTest(int[] sizes)
        {
            Utils.WriteMethod("Memory test");
            foreach (var size in sizes)
            {
                Utils.WriteMethod(String.Format("[{0}]", size));

                Utils.WriteMemoryUsed("PersistentList", () => Enumerable.Range(0, size).ToPersistentList());
                Utils.WriteMemoryUsed("PersistentVList", () => Enumerable.Range(0, size).ToPersistentVList());
                Utils.WriteMemoryUsed("List", () => Enumerable.Range(0, size).ToList());
                Utils.WriteMemoryUsed("ImmutableList", () => Enumerable.Range(0, size).ToImmutableList());
                Console.WriteLine("----------");
            }
        }

        private static void SetAtTransientTest(Benchmark benchmark)
        {
            var actPList = new Action<PersistentList<int>>(plist =>
            {
                var tr = plist.AsTransient();
                for (int i = 0; i < N; i++)
                    tr[i] = 0;
            });

            var actVList = new Action<PersistentVList<int>>(vlist =>
            {
                var tr = vlist.AsTransient();
                for (int i = N - 1; i >= 0; i--)
                    tr[i] = 0;
            });

            var actIList = new Action<ImmutableList<int>>(ilist =>
            {
                var tr = ilist.ToBuilder();
                for (int i = 0; i < N; i++)
                    tr[i] = 0;
            });

            var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullVList = PersistentVList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));

            Utils.WriteMethod("Transient SetAt");
            Utils.Write("TransientList", benchmark.Perform(actPList, () => fullPList));
            Utils.Write("TransientVList", benchmark.Perform(actVList, () => fullVList));
            Utils.Write("ImmutableList.Build", benchmark.Perform(actIList, () => fullIList));
            Console.WriteLine("----------");
        }

        private static void RemoveTransientTest(Benchmark benchmark)
        {
            var actPList = new Action<PersistentList<int>>(plist =>
            {
                var tr = plist.AsTransient();
                for (int i = 0; i < N; i++)
                    tr.RemoveLast();
            });

            var actVList = new Action<PersistentVList<int>>(vlist =>
            {
                var tr = vlist.AsTransient();
                for (int i = 0; i < N; i++)
                    tr.RemoveLast();
            });

            var actIList = new Action<ImmutableList<int>>(ilist =>
            {
                var tr = ilist.ToBuilder();
                for (int i = 0; i < N; i++)
                    tr.Remove(tr.Count - 1);
            });

            var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullVList = PersistentVList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));

            Utils.WriteMethod("Transient RemoveLast");
            Utils.Write("TransientList", benchmark.Perform(actPList, () => fullPList));
            Utils.Write("TransientVList", benchmark.Perform(actVList, () => fullVList));
            Utils.Write("ImmutableList.Build", benchmark.Perform(actIList, () => fullIList));
            Console.WriteLine("----------");
        }

        private static void AddTransientTest(Benchmark benchmark)
        {
            var actTPListAdd = new Action(() =>
            {
                var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            });

            var actTVListAdd = new Action(() =>
            {
                var fullVList = PersistentVList<int>.Empty.Add(Enumerable.Range(0, N));
            });

            var actTIListAdd = new Action(() =>
            {
                var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));
            });

            Utils.WriteMethod("Transient Add");
            Utils.Write("TransientList", benchmark.Perform(actTPListAdd));
            Utils.Write("TransientVList", benchmark.Perform(actTVListAdd));
            Utils.Write("ImmutableList.Build", benchmark.Perform(actTIListAdd));
            Console.WriteLine("----------");
        }

        private static void GetAtTest(Benchmark benchmark)
        {
            var actPListGet = new Action<PersistentList<int>>(plist =>
            {
                for (int i = 0; i < N; i++)
                {
                    var t = plist[i];
                }
            });


            var actVListGet = new Action<PersistentVList<int>>(vlist =>
            {
                for (int i = 0; i < N; i++)
                {
                    var t = vlist[i];
                }
            });

            var actListGet = new Action<List<int>>(list =>
            {
                for (int i = 0; i < N; i++)
                {
                    var t = list[i];
                }
            });

            var actIListGet = new Action<ImmutableList<int>>(ilist =>
            {
                for (int i = 0; i < N; i++)
                {
                    var t = ilist[i];
                }
            });

            var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullVList = PersistentVList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));

            var pListSet = benchmark.Perform(actPListGet, () => fullPList);
            var vListSet = benchmark.Perform(actVListGet, () => fullVList);
            var iListSet = benchmark.Perform(actIListGet, () => fullIList);

            var ListSet = benchmark.Perform(actListGet, () =>
            {
                var list = new List<int>(N);
                list.AddRange(Enumerable.Range(0, N));
                return list;
            });

            Utils.WriteMethod("GetAt");
            Utils.Write("PersistentList", pListSet);
            Utils.Write("PersistentVList", vListSet);
            Utils.Write("List", ListSet);
            Utils.Write("ImmutableList", iListSet);
            Console.WriteLine("----------");
        }

        private static void SetAtTest(Benchmark benchmark)
        {
            var actPListSet = new Action<PersistentList<int>>(plist => {
                for (int i = 0; i < N; i++)
                    plist = plist.SetAt(i, 0);
            });

            var actListSet = new Action<List<int>>(list =>
            {
                for (int i = 0; i < N; i++)
                    list[i] = 0;
            });

            var actIListSet = new Action<ImmutableList<int>>(ilist =>
            {
                for (int i = 0; i < N; i++)
                    ilist = ilist.SetItem(i, 0);
            });

            var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));

            var pListRm = benchmark.Perform(actPListSet, () => fullPList);
            var iListRm = benchmark.Perform(actIListSet, () => fullIList);

            var ListRm = benchmark.Perform(actListSet, () =>
            {
                var list = new List<int>(N);
                list.AddRange(Enumerable.Range(0, N));
                return list;
            });

            Utils.WriteMethod("SetAt");
            Utils.Write("PersistentList", pListRm);
            Utils.Write("List", ListRm);
            Utils.Write("ImmutableList", iListRm);
            Console.WriteLine("----------");
        }

        private static void RemoveTest(Benchmark benchmark)
        {
            var actPListRm = new Action<PersistentList<int>>(plist =>
            {
                for (int i = 0; i < N; i++)
                    plist = plist.RemoveLast();
            });

            var actVListRm = new Action<PersistentVList<int>>(vlist =>
            {
                for (int i = 0; i < N; i++)
                    vlist = vlist.RemoveLast();
            });

            var actListRm = new Action<List<int>>(list =>
            {
                for (int i = 0; i < N; i++)
                    list.RemoveAt(list.Count - 1);
            });

            var actIListRm = new Action<ImmutableList<int>>(ilist =>
            {
                for (int i = 0; i < N; i++)
                    ilist = ilist.RemoveAt(ilist.Count - 1);
            });

            var fullPList = PersistentList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullVList = PersistentVList<int>.Empty.Add(Enumerable.Range(0, N));
            var fullIList = ImmutableList<int>.Empty.AddRange(Enumerable.Range(0, N));

            var pListRm = benchmark.Perform(actPListRm, () => fullPList);
            var vListRm = benchmark.Perform(actVListRm, () => fullVList);
            var iListRm = benchmark.Perform(actIListRm, () => fullIList);

            var ListRm = benchmark.Perform(actListRm, () => {
                var list = new List<int>(N);
                list.AddRange(Enumerable.Range(0, N));
                return list;
            });

            Utils.WriteMethod("Remove");
            Utils.Write("PersistentList", pListRm);
            Utils.Write("PersistentVList", vListRm);
            Utils.Write("List", ListRm);
            Utils.Write("ImmutableList", iListRm);
            Console.WriteLine("----------");
        }

        private static void AddTest(Benchmark benchmark)
        {
            var actPListAdd = new Action(() =>
            {
                var plist = PersistentList<int>.Empty;

                for (int i = 0; i < N; i++)
                    plist = plist.Add(i);
            });

            var actVListAdd = new Action(() =>
            {
                var vlist = PersistentVList<int>.Empty;

                for (int i = 0; i < N; i++)
                    vlist = vlist.Add(i);
            });

            var actListAdd = new Action(() =>
            {
                var list = new List<int>();

                for (int i = 0; i < N; i++)
                    list.Add(i);
            });

            var actIListAdd = new Action(() =>
            {
                var ilist = ImmutableList<int>.Empty;

                for (int i = 0; i < N; i++)
                    ilist = ilist.Add(i);
            });

            var pListAdd = benchmark.Perform(actPListAdd);
            var vListAdd = benchmark.Perform(actVListAdd);
            var ListAdd = benchmark.Perform(actListAdd);
            var iListAdd = benchmark.Perform(actIListAdd);

            Utils.WriteMethod("Add");
            Utils.Write("PersistentList", pListAdd);
            Utils.Write("PersistentVList", vListAdd);
            Utils.Write("List", ListAdd);
            Utils.Write("ImmutableList", iListAdd);
            Console.WriteLine("----------");
        }
    }
}
