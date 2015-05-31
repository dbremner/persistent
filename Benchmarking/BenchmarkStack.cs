using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class BenchmarkStack
    {
        private const int N = 1000000;

        public static void Perform(Benchmark benchmark)
        {
            Utils.WriteHeader("Stack", N, benchmark);

            PushTest(benchmark);
            PopTest(benchmark);
        }

        public static void MemoryTest(int[] sizes)
        {
            Utils.WriteMethod("Memory test");
            foreach (var size in sizes)
            {
                Utils.WriteMethod(String.Format("[{0}]", size));

                Utils.WriteMemoryUsed("PersistentStack", () =>
                {
                    var q = PersistentStack<int>.Empty;
                    for (int i = 0; i < size; i++)
                        q = q.Push(i);

                    return q;
                });

                Utils.WriteMemoryUsed("Stack", () => new Stack<int>(Enumerable.Range(0, size)));
                Utils.WriteMemoryUsed("ImmutableStack", () =>
                {
                    var q = ImmutableStack<int>.Empty;
                    for (int i = 0; i < size; i++)
                        q = q.Push(i); 

                    return q;
                });
                Console.WriteLine("----------");
            }
        }


        private static void PopTest(Benchmark benchmark)
        {
            var pStack = new Action<PersistentStack<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d = d.Pop(); 
                
            });

            var stack = new Action<Stack<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d.Pop(); 
            });

            var iStack = new Action<ImmutableStack<int>>(d =>
            {
                for (int i = 0; i < N; i++)
                    d = d.Pop(); 
            });

            Func<PersistentStack<int>> fullPStack = () =>
            {
                var q = PersistentStack<int>.Empty;
                for (int i = 0; i < N; i++)
                    q = q.Push(i);

                return q;
            };

            Func<Stack<int>> fullStack = () => new Stack<int>(Enumerable.Range(0, N));

            Func<ImmutableStack<int>> fullIStack = () =>
            {
                var q = ImmutableStack<int>.Empty;
                for (int i = 0; i < N; i++)
                    q = q.Push(i); 

                return q;
            };

            Utils.WriteMethod("Pop");
            Utils.Write("PersistentStack", benchmark.Perform(pStack, fullPStack));
            Utils.Write("Stack", benchmark.Perform(stack, fullStack));
            Utils.Write("ImmutableStack", benchmark.Perform(iStack, fullIStack));
            Console.WriteLine("----------");
        }

        private static void PushTest(Benchmark benchmark)
        {
            var pStack = new Action(() =>
            {
                var d = PersistentStack<int>.Empty;

                for (int i = 0; i < N; i++)
                    d = d.Push(i);

            });

            var stack = new Action(() =>
            {
                var d = new Stack<int>();

                for (int i = 0; i < N; i++)
                    d.Push(i);
            });

            var iStack = new Action(() =>
            {
                var d = ImmutableStack<int>.Empty;

                for (int i = 0; i < N; i++)
                    d = d.Push(i);
            });

            Utils.WriteMethod("Push");
            Utils.Write("PersistentStack", benchmark.Perform(pStack));
            Utils.Write("Stack", benchmark.Perform(stack));
            Utils.Write("ImmutableStack", benchmark.Perform(iStack));
            Console.WriteLine("----------");
        }
    }
}
