using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class BenchmarkHashSet
    {
        private const int N = 100000;

        public static void Perform(Benchmark benchmark)
        {
            Utils.WriteHeader("HashSet", N, benchmark);

            AddTest(benchmark);
            ContainsTest(benchmark);
            RemoveTest(benchmark);
        }

        public static void MemoryTest(int[] sizes)
        {
            Utils.WriteMethod("Memory test");
            foreach (var size in sizes)
            {
                Utils.WriteMethod(String.Format("[{0}]", size));
                var rand = RandomArray(size);

                Utils.WriteMemoryUsed("PersistentHashSet", () => rand.ToPersistentHashSet());
                Utils.WriteMemoryUsed("HashSet", () => new HashSet<int>(rand));
                Utils.WriteMemoryUsed("ImmutableHashSet", () => rand.ToImmutableHashSet());
                Console.WriteLine("----------");
            }
        }

        private static void ContainsTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action<PersistentHashSet<int>>(d =>
            {
                foreach (var i in arr)
                { var y = d.Contains(i); }
            });

            var dict = new Action<HashSet<int>>(d =>
            {
                foreach (var i in arr)
                { var y = d.Contains(i); }
            });

            var iDict = new Action<ImmutableHashSet<int>>(d =>
            {
                foreach (var i in arr)
                { var y = d.Contains(i); }
            });

            var fullPDict = arr.ToPersistentHashSet();
            var fullIDict = arr.ToImmutableHashSet();
            var fullDict = new HashSet<int>(arr);

            var pd = benchmark.Perform(pDict, () => fullPDict);
            var di = benchmark.Perform(dict, () => fullDict);
            var id = benchmark.Perform(iDict, () => fullIDict);

            Utils.WriteMethod("Contains");
            Utils.Write("PersistentHashSet", pd);
            Utils.Write("HashSet", di);
            Utils.Write("ImmutableHashSet", id);
            Console.WriteLine("----------");
        }

        private static void RemoveTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action<PersistentHashSet<int>>(d =>
            {
                foreach (var i in arr)
                    d = d.Remove(i);
            });

            var dict = new Action<HashSet<int>>(d =>
            {
                foreach (var i in arr)
                    d.Remove(i);
            });

            var iDict = new Action<ImmutableHashSet<int>>(d =>
            {
                foreach (var i in arr)
                    d = d.Remove(i);
            });

            var fullPDict = arr.ToPersistentHashSet();
            var fullIDict = arr.ToImmutableHashSet();
            var fullDict = new HashSet<int>(arr);

            var pd = benchmark.Perform(pDict, () => fullPDict);
            var di = benchmark.Perform(dict, () => fullDict);
            var id = benchmark.Perform(iDict, () => fullIDict);

            Utils.WriteMethod("Remove");
            Utils.Write("PersistentHashSet", pd);
            Utils.Write("HashSet", di);
            Utils.Write("ImmutableHashSet", id);
            Console.WriteLine("----------");
        }

        private static void AddTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var actPDictAdd = new Action(() =>
            {
                var d = PersistentHashSet<int>.Empty;

                foreach (var i in arr)
                    d = d.Add(i);
            });

            var actDictAdd = new Action(() =>
            {
                var d = new HashSet<int>();

                foreach (var i in arr)
                    d.Add(i);
            });

            var actIDictAdd = new Action(() =>
            {
                var d = ImmutableHashSet<int>.Empty;

                foreach (var i in arr)
                    d = d.Add(i);
            });

            var pAdd = benchmark.Perform(actPDictAdd);
            var Add = benchmark.Perform(actDictAdd);
            var iAdd = benchmark.Perform(actIDictAdd);

            Utils.WriteMethod("Add");
            Utils.Write("PersistentHashSet", pAdd);
            Utils.Write("HashSet", Add);
            Utils.Write("ImmutableHashSet", iAdd);
            Console.WriteLine("----------");
        }

        private static int[] RandomArray(int num = N)
        {
            var list = new List<int>();
            var hs = new HashSet<int>();
            var r = new Random(123456);

            for (int i = 0; i < num; i++)
            {
                var n = r.Next();
                while (hs.Contains(n)) n = r.Next();
                list.Add(n);
                hs.Add(n);
            }

            return list.ToArray();
        }
    }
}
