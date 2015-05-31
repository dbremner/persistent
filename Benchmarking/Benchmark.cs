using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    class Benchmark
    {
        private int warmingIterations;
        private int iterations;

        public int WarmingIterations { get { return warmingIterations; } }
        public int Iterations { get { return iterations; } }

        public Benchmark(int warmingIterations, int iterations)
        {
            this.warmingIterations = warmingIterations;
            this.iterations = iterations;
        }

        private void Warming(Action action)
        {
            for (int i = 0; i < warmingIterations; i++) 
                action();
        }

        private void Warming<T>(Action<T> action, Func<T> inpFunc)
        {
            for (int i = 0; i < warmingIterations; i++)
                action(inpFunc());
        }

        private void CollectGarbage()
        {
            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Forced,
                blocking: true);
        }

        public TimeSpan[] Perform<T>(Action<T> action, Func<T> inpFunc)
        {
            Warming(action, inpFunc);
            var ellapsed = new TimeSpan[iterations];
            var time = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                CollectGarbage();
                var inp = inpFunc();

                time.Restart();
                action(inp);
                time.Stop();

                ellapsed[i] = time.Elapsed;
            }

            return ellapsed;
        }

        public TimeSpan[] Perform(Action action)
        {
            Warming(action);
            var ellapsed = new TimeSpan[iterations];
            var time = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                CollectGarbage();

                time.Restart();
                action();
                time.Stop();

                ellapsed[i] = time.Elapsed;
            }

            return ellapsed;
        }
    }
}
