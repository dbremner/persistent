using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
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
