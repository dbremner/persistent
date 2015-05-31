using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class BenchmarkQueue
    {
        private const int N = 1000000;

        public static void Perform(Benchmark benchmark)
        {
            Utils.WriteHeader("Queue", N, benchmark);

            EnqueueTest(benchmark);
            DequeueTest(benchmark);
        }

        public static void MemoryTest(int[] sizes)
        {
            Utils.WriteMethod("Memory test");
            foreach (var size in sizes)
            {
                Utils.WriteMethod(String.Format("[{0}]", size));

                Utils.WriteMemoryUsed("PersistentQueue", () => PersistentQueue<int>.Empty.Enqueue(Enumerable.Range(0, size)));
                Utils.WriteMemoryUsed("Queue", () => new Queue<int>(Enumerable.Range(0, size)));
                Utils.WriteMemoryUsed("ImmutableQueue", () =>
                {
                    var q = ImmutableQueue<int>.Empty;
                    for (int i = 0; i < size; i++)
                        q = q.Enqueue(i);

                    return q;
                });
                Console.WriteLine("----------");
            }
        }

        private static void DequeueTest(Benchmark benchmark)
        {
            var pQueue = new Action<PersistentQueue<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d = d.Dequeue(); 
                
            });

            var queue = new Action<Queue<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d.Dequeue(); 
            });

            var iQueue = new Action<ImmutableQueue<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d = d.Dequeue(); 
            });

            var fullPQueue = PersistentQueue<int>.Empty.Enqueue(Enumerable.Range(0, N));

            Func<Queue<int>> fullQueue = () => new Queue<int>(Enumerable.Range(0, N));

            Func<ImmutableQueue<int>> fullIQueue = () =>
            {
                var q = ImmutableQueue<int>.Empty;
                for (int i = 0; i < N; i++)
                    q = q.Enqueue(i); 

                return q;
            };

            var pq = benchmark.Perform(pQueue, () => fullPQueue);
            var qu = benchmark.Perform(queue, fullQueue);
            var iq = benchmark.Perform(iQueue, fullIQueue);

            Utils.WriteMethod("Dequeue");
            Utils.Write("PersistentQueue", pq);
            Utils.Write("Queue", qu);
            Utils.Write("ImmutableQueue", iq);
            Console.WriteLine("----------");
        }

        private static void EnqueueTest(Benchmark benchmark)
        {
            var pQueue = new Action(() =>
            {
                var d = PersistentQueue<int>.Empty;

                for (int i = 0; i < N; i++)
                    d = d.Enqueue(i);

            });

            var queue = new Action(() =>
            {
                var d = new Queue<int>();

                for (int i = 0; i < N; i++)
                    d.Enqueue(i);
            });

            var iQueue = new Action(() =>
            {
                var d = ImmutableQueue<int>.Empty;

                for (int i = 0; i < N; i++)
                    d = d.Enqueue(i);
            });

            var pq = benchmark.Perform(pQueue);
            var qu = benchmark.Perform(queue);
            var iq = benchmark.Perform(iQueue);

            Utils.WriteMethod("Enqueue");
            Utils.Write("PersistentQueue", pq);
            Utils.Write("Queue", qu);
            Utils.Write("ImmutableQueue", iq);
            Console.WriteLine("----------");
        }
    }
}
