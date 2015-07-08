using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1);

            // Memory test
            var structureSizes = new[] { 1000, 10000, 100000, 1000000 };

            BenchmarkQueue.MemoryTest(structureSizes);
            BenchmarkStack.MemoryTest(structureSizes);
            BenchmarkHashSet.MemoryTest(structureSizes);
            BenchmarkList.MemoryTest(structureSizes);
            BenchmarkDictionary.MemoryTest(structureSizes);

            // Speed test
            var benchmark = new Benchmark(5, 50);

            BenchmarkHashSet.Perform(benchmark);
            BenchmarkStack.Perform(benchmark);
            BenchmarkQueue.Perform(benchmark);
            BenchmarkList.Perform(benchmark);
            BenchmarkDictionary.Perform(benchmark);

            Console.WriteLine("end testing");
            Console.ReadLine();
        }
    }
}
