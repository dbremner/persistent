using PersistentCollections;
using PersistentCollections.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TmpTesting
{
    class Program
    {

        static void Main(string[] args)
        {
            var max = 5000000;
            var t = Stopwatch.StartNew();
            
            Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            for (int n = 0; n < 20; n++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var qq = PersistentVList<int>.Empty;
                var d = ImmutableStack<int>.Empty;
                var v0 = new PersistentVList<int>[max];
                var v1 = new ImmutableStack<int>[max];
                t.Restart();
                for (int i = 0; i < max; i++)
                {
                    qq = qq.Add(i);
                    v0[i] = qq;
                    //if (i % 200 == 0) qq = qq.RemoveLast();
                }
                Console.WriteLine(t.Elapsed);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                t.Restart();
                for (int i = 0; i < max; i++)
                {
                    d = d.Push(i);
                    v1[i] = d;
                    //if (i % 200 == 0) qq = qq.RemoveLast();
                }
                Console.WriteLine(t.Elapsed);
                Console.WriteLine("---");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.ReadLine();
        }
    }

    static class Extensions
    {
        public static int[] RandomArray(this Random r, int max, int lenght)
        {
            var list = new List<int>(lenght);

            for (int i = 0; i < lenght; i++)
            {
                list.Add(r.Next(max));
            }

            return list.ToArray();
        }
    }
}
