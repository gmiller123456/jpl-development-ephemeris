using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPLDEBinary
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            JPLDE.DE de = new JPLDE.DE(@"E:\Astronomy\_Ephemeris\JPLDEBinaries\jpleph.431");
            Console.WriteLine(de.getPlanet(1, de.getHeader().jdStart)[0]);
            */
            Testpo.RunTests();

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
