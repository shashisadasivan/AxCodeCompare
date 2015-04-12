/// Created by: Shashi Sadasivan
/// Visit: shashidotnet.wordpress.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCodeCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            List<int> baseModels = new List<int>() { 56 };
            List<int> compareToModels = new List<int>() { 37,40,41,43,44,19,26,27,28,45,46,47,48,49,50,11,42,23 };

            CompareModels c = new CompareModels(baseModels, compareToModels);
            var lines = c.Start();

            //c = new CompareModels("56", "37,40,41,43,44,19,26,27,28,45,46,47,48,49,50,11,42,23");
            //lines = c.Start();

            System.IO.File.WriteAllLines("out.txt", lines);
        }
    }
}
