using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    class MemoryTest
    {
        private List<int> sizes = new List<int>();

        public void Add(int size)
        {
            sizes.Add(size);
        }

        public void WriteMemoryUsed<T>(string structure, Func<T> getCollection)
        {
            foreach (var size in sizes)
            {
                var memBefore = GC.GetTotalMemory(true);
                var collection = getCollection();
                var memAfter = GC.GetTotalMemory(true);
                Console.WriteLine("{0}: {1} [{2}]", structure, memAfter - memBefore, size);

                // In order to prevent JIT to do optimizations
                if (collection.GetHashCode() != 0)
                    memAfter = 0;
            }
        }

    }
}
