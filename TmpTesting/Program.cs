using PersistentCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var empty = PersistentList<int>.Empty;
            var plist = empty.Add(1); // [1]
            
            var a = plist.Add(2, 3, 4, 5);              // [1, 2, 3, 4, 5]
            var b = empty.Add(new[] { 1, 2, 3, 4, 5 }); // [ 1, 2, 3, 4, 5 ]

            if (a == b) // true
            {
                Console.WriteLine("{0} == {1}, ", a, b);
            }

            plist = plist.RemoveLast();

            if (plist == empty) // true
                Console.WriteLine("{0} == {1}, ", plist, empty);


            var pdict = a.ToPersistentDictionary(x => x, x => x * 2);
            pdict = pdict.Remove(1).Remove(2);


            Console.ReadLine();
        }
    }
}
