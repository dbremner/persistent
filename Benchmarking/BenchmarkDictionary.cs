using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class BenchmarkDictionary
    {
        private const int N = 100000;

        public static void Perform(Benchmark benchmark)
        {
            Utils.WriteHeader("Dictionary", N, benchmark);

            AddTest(benchmark);
            GetAtTest(benchmark);
            RemoveTest(benchmark);
        }

        public static void MemoryTest(int[] sizes)
        {
            Utils.WriteMethod("Memory test");
            foreach (var size in sizes)
            {
                Utils.WriteMethod(String.Format("[{0}]", size));
                var rand = RandomArray(size);

                Utils.WriteMemoryUsed("PersistentDictionary", () => rand.ToPersistentDictionary(x => x, x => x));
                Utils.WriteMemoryUsed("Dictionary", () => rand.ToDictionary(x => x, x => x));
                Utils.WriteMemoryUsed("ImmutableDictionary", () => rand.ToImmutableDictionary(x => x, x => x));
                Console.WriteLine("----------");
            }
        }

        private static void RemoveTransientTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action<PersistentDictionary<int, int>>(d =>
            {
                var tr = d.AsTransient();

                foreach (var i in arr)
                    tr.Remove(i);
            });

            var iDict = new Action<ImmutableDictionary<int, int>>(d =>
            {
                var tr = d.ToBuilder();

                foreach (var i in arr)
                    tr.Remove(i);
            });

            var fullPDict = arr.ToPersistentDictionary(x => x, x => x);
            var fullIDict = arr.ToImmutableDictionary(x => x, x => x);

            var pd = benchmark.Perform(pDict, () => fullPDict);
            var id = benchmark.Perform(iDict, () => fullIDict);

            Utils.WriteMethod("Transient Remove");
            Utils.Write("TransientDictionary", pd);
            Utils.Write("ImmutableDictionary.build", id);
            Console.WriteLine("----------");
        }

        private static void AddTransientTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action(() =>
            {
                var d = arr.ToPersistentDictionary(x => x, x => x);
            });

            var iDict = new Action(() =>
            {
                var d = arr.ToImmutableDictionary(x => x, x => x);
            });

            var pd = benchmark.Perform(pDict);
            var id = benchmark.Perform(iDict);

            Utils.WriteMethod("Transient Add");
            Utils.Write("TransientDictionary.Add", pd);
            Utils.Write("ImmutableDictionary.build.Ad", id);
            Console.WriteLine("----------");
        }



        private static void GetAtTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action<PersistentDictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                { var y = d[i]; }
            });

            var dict = new Action<Dictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                { var y = d[i]; }
            });

            var iDict = new Action<ImmutableDictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                { var y = d[i]; }
            });

            var fullPDict = arr.ToPersistentDictionary(x => x, x => x);
            var fullIDict = arr.ToImmutableDictionary(x => x, x => x);
            var fullDict = arr.ToDictionary(x => x, x => x);

            var pd = benchmark.Perform(pDict, () => fullPDict);
            var di = benchmark.Perform(dict, () => fullDict);
            var id = benchmark.Perform(iDict, () => fullIDict);

            Utils.WriteMethod("GetAt");
            Utils.Write("PersistentDictionary", pd);
            Utils.Write("Dictionary", di);
            Utils.Write("ImmutableDictionary", id);
            Console.WriteLine("----------");
        }

        private static void RemoveTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var pDict = new Action<PersistentDictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                    d = d.Remove(i);
            });

            var dict = new Action<Dictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                    d.Remove(i);
            });

            var iDict = new Action<ImmutableDictionary<int, int>>(d =>
            {
                foreach (var i in arr)
                    d = d.Remove(i);
            });

            var fullPDict = arr.ToPersistentDictionary(x => x, x => x);
            var fullIDict = arr.ToImmutableDictionary(x => x, x => x);
            var fullDict = arr.ToDictionary(x => x, x => x);

            var pd = benchmark.Perform(pDict, () => fullPDict);
            var di = benchmark.Perform(dict, () => fullDict);
            var id = benchmark.Perform(iDict, () => fullIDict);

            Utils.WriteMethod("Remove");
            Utils.Write("PersistentDictionary", pd);
            Utils.Write("Dictionary", di);
            Utils.Write("ImmutableDictionary", id);
            Console.WriteLine("----------");
        }

        private static void AddTest(Benchmark benchmark)
        {
            var arr = RandomArray();

            var actPDictAdd = new Action(() =>
            {
                var d = PersistentDictionary<int, int>.Empty;

                foreach (var i in arr)
                    d = d.Add(i, i);
            });

            var actDictAdd = new Action(() =>
            {
                var d = new Dictionary<int, int>();

                foreach (var i in arr)
                    d.Add(i, i);
            });

            var actIDictAdd = new Action(() =>
            {
                var d = ImmutableDictionary<int, int>.Empty;

                foreach (var i in arr)
                    d = d.Add(i, i);
            });

            var pAdd = benchmark.Perform(actPDictAdd);
            var Add = benchmark.Perform(actDictAdd);
            var iAdd = benchmark.Perform(actIDictAdd);

            Utils.WriteMethod("Add");
            Utils.Write("PersistentDictionary", pAdd);
            Utils.Write("Dictionary", Add);
            Utils.Write("ImmutableDictionary", iAdd);
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
