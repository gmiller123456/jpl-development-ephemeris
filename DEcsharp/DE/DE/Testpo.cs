/*
# Program to test DE405 implementation against JPL supplied test vecotrs
# Greg Miller (gmiller@gregmiller.net) 2019
# Released as public domain
*/

using System;

namespace DE
{
    class Testpo
    {

        static int tests = 0;
        static int fail = 0;
        static int skipped = 0;
        static DE de;
        static string baseDir=@"D:\JPL DE\ascii\";
        //static string baseDir = @"C:\prog\temp\jpl-development-ephemeris\";

        const double au = 0.149597870691000015E+09;
        static int[] map = { 0, 0, 1, 13, 3, 4, 5, 6, 7, 8, 9, 10, 12, 2, 11, 12, 0, 14 };

        static double get(int series, double jd, int x) {
            if (series == 12 || series == 0) {
                return 0;
            }

            int s = map[series];
            double[] b = { 0, 0, 0, 0, 0, 0 };

            if (series == 3) {
                double[] emb = de.getAllPropertiesForSeries(2, jd);
                double[] moon = de.getAllPropertiesForSeries(9, jd);
                b = DE.getEarthPositionFromEMB(emb, moon);
            } else if (series == 10) {

                double[] emb = de.getAllPropertiesForSeries(2, jd);
                double[] moon = de.getAllPropertiesForSeries(9, jd);
                double[] earth = DE.getEarthPositionFromEMB(emb, moon);

                for (int i = 0; i < 6; i++) {
                    moon[i] += earth[i];
                }
                b = moon;
            }
            else {
                b = de.getAllPropertiesForSeries(s, jd);
            }
            return b[x - 1];
        }


        static void testpo(int deNumber, int year, int month, int day, double jd, int t, int c, int x, double expectedValue) {
            Console.WriteLine($"{deNumber} {year} {month} {day} {jd} {t} {c} {x} {expectedValue}");

            double t1 = get(t, jd, x);
            double t2 = get(c, jd, x);
            double v = t1 - t2;

            if (t != 15 && t != 14 && t != 17) {
                v /= au;
            }

            double error = Math.Abs(v - expectedValue);

            if (error > 1.0E-8 || Double.IsNaN(error)) {
                Console.WriteLine($"Fail: {jd}\t{t}\t{c}\t{x}\t{expectedValue}\t{v}\tDiff={error}");
                fail++;
            }
            else {
                //Console.WriteLine($"Pass : {jd}\t{t}\t{c}\t{x}\t{expectedValue}\t{v}\tDiff={error}");
                Console.Write(".");
            }

            tests++;
        }
        static void parseTestCase(string s) {

            int de = int.Parse(s.Substring(0,3));
            int year = int.Parse(s.Substring(5,4));

            int month = int.Parse(s.Substring(10,2));
            int day = int.Parse(s.Substring(13,2));

            double jd = Double.Parse(s.Substring(16,9));

            int t = int.Parse(s.Substring(25,3));
            int c = int.Parse(s.Substring(28,3));

            int x = int.Parse(s.Substring(31,3));

            double v = Double.Parse(s.Substring(34));
            try
            {
                testpo(de, year, month, day, jd, t, c, x, v);
            } catch (System.IO.FileNotFoundException e) {
                //Console.WriteLine(e);
                Console.Write("-");
                skipped += 1;
            }
        }

        static void runTestFile(string filename) {
            System.IO.StreamReader f = new System.IO.StreamReader(filename);

            string l = f.ReadLine();
            while (l.Length<3 || !l.Substring(0,3).Equals("EOT")) {
                l = f.ReadLine();
            }

            l = f.ReadLine();

            while (l != null) {
                parseTestCase(l);
                l = f.ReadLine();
            }
            f.Close();
        }

        static void runVersionTest(object[] data) {
            Console.WriteLine("Testing: DE" + (string)data[0]);
            de = new DE(data);

            runTestFile(baseDir+"de" + data[0] + "/testpo." + data[0]);
        }

        public static void run() {
            object[][] data=new object[][] { 
               //new object[]{  "102",      "ascm0200.102",  300,   773,  "-1410-APR-16-00:00:00",   "3002-DEC-22-00:00:00",     1206160.50,     2817872.50,  64, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,   93,  138,  228,  258,  285,  309,  333,  351,  369,  729,  774,  774,  774,  774,  774,  774,  774,  774,  774,   15,   15,   15,   10,    9,    8,    8,    6,    6,   15,   15,    0,    0,    0,    0,    0,    0,    0,    0,    0,    2,    1,    2,    1,    1,    1,    1,    1,    1,    8,    1,    0,    0,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "200",      "ascp1600.200",   20,   826,   "1599 DEC 09 00:00:00",   "2169 MAY 02 00:00:00",     2305424.50,     2513392.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  147,  183,  273,  303,  330,  354,  378,  396,  414,  702,  747,    0,    0,    0,    0,    0,    0,    0,    0,   12,   12,   15,   10,    9,    8,    8,    6,    6,   12,   15,   10,    0,    0,    0,    0,    0,    0,    0,    0,    4,    1,    2,    1,    1,    1,    1,    1,    1,    8,    1,    4,    0,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "202",      "ascp1900.202",   50,   826,   "1899-DEC-04-00:00:00",   "2050-JAN-02-00:00:00",     2414992.50,     2469808.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  147,  183,  273,  303,  330,  354,  378,  396,  414,  702,  747,  827,  827,  827,  827,  827,  827,  827,  827,   12,   12,   15,   10,    9,    8,    8,    6,    6,   12,   15,   10,    0,    0,    0,    0,    0,    0,    0,    0,    4,    1,    2,    1,    1,    1,    1,    1,    1,    8,    1,    4,    0,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "403",      "ascp1600.403",  100,  1018,   "1599-APR-29-00:00:00",   "2199-JUN-22-00:00:00",     2305200.50,     2524400.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "405",      "ascp1600.405",   20,  1018,   "1599 DEC 09 00:00:00",   "2201 FEB 20 00:00:00",     2305424.50,     2525008.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "406",      "ascm0100.406",  100,   728,  "-3000 FEB 23 00:00:00",   "3000 MAY 06 00:00:00",      625360.50,     2816912.50,  64, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  207,  261,  291,  309,  327,  345,  363,  381,  693,  729,  729,  729,  729,  729,  729,  729,  729,  729,   14,   12,    9,   10,    6,    6,    6,    6,    6,   13,   12,    0,    0,    0,    0,    0,    0,    0,    0,    0,    4,    1,    2,    1,    1,    1,    1,    1,    1,    8,    1,    0,    0,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "410",      "ascp1960.410",   20,  1018,   "1900 FEB 06 00:00:00",   "2019 DEC 15 00:00:00",     2415056.50,     2458832.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "413",      "ascp1900.413",   25,  1018,   "1899 DEC 04 00:00:00",   "2050 MAR 07 00:00:00",     2414992.50,     2469872.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "414",      "ascp1600.414",  100,  1018,   "1599 DEC 09 00:00:00",   "2201 FEB 20 00:00:00",     2305424.50,     2525008.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "418",      "ascp1900.418",   50,  1018,   "1899-DEC-04-00:00:00",   "2051-JAN-21-00:00:00",     2414992.50,     2470192.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               //new object[]{  "421",      "ascp1900.421",  150,  1018,   "1899-DEC-04 00:00:00",   "2200-FEB-01 00:00:00",     2414992.50,     2524624.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "422",      "ascp0100.422",  100,  1018,  "-3000-DEC-07-00:00:00",   "3000-JAN-30-00:00:00",      625648.50,     2816816.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "423",      "ascp1800.423",   50,  1018,   "1799-DEC-16-00:00:00",   "2200-FEB-01-00:00:00",     2378480.50,     2524624.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "424",      "ascm0100.424",  100,  1018,  "-3001-DEC-21 00:00:00",   "3000-JAN-30 00:00:00",      625296.50,     2816816.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{ "430t",    "ascp01550.430t",  100,   982,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  819,  939,  939,  939,  939,  939,  939,  939,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,    0,   10,    0,   11,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    0,    4,    0,    4,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "430",      "ascp1550.430",  100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "431",     "ascm01000.431", 1000,  1018, "-13200-AUG-15 00:00:00",  "17191-MAR-15 00:00:00",     3100015.50,     8000016.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{ "432t",    "ascp01550.432t",   50,   982,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  819,  939,  939,  939,  939,  939,  939,  939,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,    0,   10,    0,   11,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    0,    4,    0,    4,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "432",     "ascp01550.432",   50,   938,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  819,  939,  939,  939,  939,  939,  939,  939,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,    0,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    0,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "434",     "ascp01550.434",  100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "435",     "ascp01550.435",  100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "436",     "ascp01550.436",  100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{ "436t",    "ascp01550.436t",  100,  1122,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,   13,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    8,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{  "438",     "ascp01550.438",  100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899, 1019, 1019, 1019, 1019, 1019, 1019, 1019,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"},
               new object[]{ "438t", "ascp01550.438t", 100, 1042, "1549-DEC-21 00:00:00", "2650-JAN-25 00:00:00", 2287184.50, 2688976.50, 32, 20, "mercury", "venus", "emb", "mars", "jupiter", "saturn", "uranus", "neptune", "pluto", "moon", "sun", "nutation", "libration", "mantle V", "TT-TDB", "future", "future", "future", "future", "future", 3, 171, 231, 309, 342, 366, 387, 405, 423, 441, 753, 819, 819, 939, 939, 939, 939, 939, 939, 939, 14, 10, 13, 11, 8, 7, 6, 6, 6, 13, 11, 0, 10, 0, 13, 0, 0, 0, 0, 0, 4, 2, 2, 1, 1, 1, 1, 1, 1, 8, 2, 0, 4, 0, 8, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 1, 0, 0, 0, 0, 0, "end" },
            };

            for(int i = 0; i < data.Length; i++)
            {
                runVersionTest(data[i]);
            }


            //object[] temp = { "438", "ascp01550.438", 100, 1018, "1549-DEC-21 00:00:00", "2650-JAN-25 00:00:00", 2287184.50, 2688976.50, 32, 20, "mercury", "venus", "emb", "mars", "jupiter", "saturn", "uranus", "neptune", "pluto", "moon", "sun", "nutation", "libration", "mantle V", "TT-TDB", "future", "future", "future", "future", "future", 3, 171, 231, 309, 342, 366, 387, 405, 423, 441, 753, 819, 899, 1019, 1019, 1019, 1019, 1019, 1019, 1019, 14, 10, 13, 11, 8, 7, 6, 6, 6, 13, 11, 10, 10, 0, 0, 0, 0, 0, 0, 0, 4, 2, 2, 1, 1, 1, 1, 1, 1, 8, 2, 4, 4, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 1, 0, 0, 0, 0, 0, "end" };
            //de = new DE(temp);

            //testpo(405, 1620, 1, 1, 2312752.5, 15, 0, 2, 0.4059670617925);
            //testpo(405, 2200, 1, 1, 2524593.5, 1, 5, 1, -4.3720415126458);
            //testpo(405, 2200, 2, 1, 2524624.5, 3, 6, 6, -0.006098120741);
            //runVersionTest(temp);

            Console.WriteLine($"Tests ran:{tests} Failed:{fail} Skipped:{skipped} ");
        }



    }
}
