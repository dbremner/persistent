using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarking
{
    static class Extensions
    {
        public static double StandardDeviation<T>(this IEnumerable<T> values, Func<T, double> select)
        {
            var avg = values
                .Select(x => select(x))
                .Average();

            return Math.Sqrt(values
                .Select(x => select(x))
                .Average(v => Math.Pow(v - avg, 2)));
        }
    }
}
