using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class Utils
    {
        public static void Write(string op, TimeSpan[] t)
        {
            Console.WriteLine("{0:000.000000} ({1:00.000}), [{2}]",
                t.Average(x => x.TotalMilliseconds),
                t.StandardDeviation(x => x.TotalMilliseconds),
                op);
        }

        public static void WriteHeader(string structure, int size, Benchmark bm)
        {
            Console.WriteLine("{0} [size = {1}, warming = {2}, iterations = {3}]", 
                structure, size, bm.WarmingIterations, bm.Iterations);
        }

        public static void WriteMethod(string name)
        {
            Console.WriteLine("{0}:", name);
        }

        public static void WriteMemoryUsed<T>(string structure, Func<T> getCollection)
        {
            var memBefore = GC.GetTotalMemory(true);
            var collection = getCollection();
            var memAfter = GC.GetTotalMemory(true);
            Console.WriteLine("{0}: {1}", structure, memAfter - memBefore);

            // In order to prevent JIT to do optimizations
            if (collection.GetHashCode() != 0)
                memAfter = 0;
        }
    }
}
