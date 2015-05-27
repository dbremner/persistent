using PersistentCollection;
using PersistentCollection.Vectors;
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
            var r = new Random(544714);
            var max = 5000;
            var t = Stopwatch.StartNew();
            
            Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            for (int n = 0; n < 20; n++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var qq = PersistentDictionary<int, int>.Empty;
                var d = ImmutableDictionary<int, int>.Empty;

                var ra = r.RandomArray(int.MaxValue, max).Distinct();
                max = ra.Count();

                t.Restart();
                foreach (var i in ra)
                {
                    qq = qq.Add(i, i);

                    //if (i % 200 == 0) qq = qq.RemoveLast();
                }
                Console.WriteLine(t.Elapsed);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                t.Restart();
                foreach (var i in ra)
                {
                    d = d.Add(i, i);

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
