using System;
using System.IO;

namespace JPLDEBinary
{
    class Testpo
    {
        static int[] map = { 0, 0, 1, 13, 3, 4, 5, 6, 7, 8, 9, 10, 12, 2, 11, 12, 0, 14 };
        static JPLDE.DE de = null;

        static int skipped = 0;
        static int fail = 0;
        static int pass = 0;
        static int tests = 0;

        static double au = 0.149597870700000000E+09;

        static double get(int series, double jd, int x)
        {
            //0 means no center, and 12 is the SSB which is always 0
            if (series == 12 || series == 0) {
                return 0;
            }

            int s = map[series];

            double[] b = { 0, 0, 0, 0, 0, 0 };

            if (series == 3) {
                double[] emb = de.getPlanet(2, jd);
                double[] moon = de.getPlanet(9, jd);
                b = JPLDE.DE.getEarthPositionFromEMB(emb, moon);
            } else if (series == 10) {
                double[] emb = de.getPlanet(2, jd);
                double[] moon = de.getPlanet(9, jd);
                double[] earth = JPLDE.DE.getEarthPositionFromEMB(emb, moon);
                for (int i = 0; i < 6; i++) {
                    moon[i] += earth[i];
                }
                b = moon;
            } else {
                b = de.getPlanet(s, jd);
            }
            return b[x - 1];
        }

        static void testpo(int deNumber, int year, int month, int day, double jd, int t, int c, int x, double expectedValue)
        {
            if (jd < de.getHeader().jdStart || jd >= de.getHeader().jdEnd) {
                skipped = skipped + 1;
                return;
            }

            double t1 = get(t, jd, x);
            double t2 = get(c, jd, x);

            double v = t1 - t2;

            if (t != 15 && t != 14 && t != 17) v /= au;

            double error = Math.Abs(v - expectedValue);

            if (error > 1.0E-8 || Double.IsNaN(error))
            {
                String status = "Fail: " + jd + "\t" + t + "\t" + c + "\t" + x + "\t" + expectedValue + "\t" + v + "\tDiff=" + error;
                Console.WriteLine(status);
                fail++;
            }
            else
            {
                //String status = "Pass: "+jd+"\t"+t+"\t"+c+"\t"+x+"\t"+expectedValue+"\t"+v+"\tDiff="+error;
                //Console.WriteLine(status);
            }
            tests++;
        }

        static void parseTestCase(String s)
        {
            int de = Int32.Parse(s.Substring(0, 3).Replace(" ", ""));
            int year = Int32.Parse(s.Substring(4, 5).Replace(" ", ""));
            int month = Int32.Parse(s.Substring(10, 2).Replace(" ", ""));
            int day = Int32.Parse(s.Substring(13, 2).Replace(" ", ""));
            double jd = Double.Parse(s.Substring(15, 10).Replace(" ", ""));
            int t = Int32.Parse(s.Substring(25, 3).Replace(" ", ""));
            int c = Int32.Parse(s.Substring(28, 3).Replace(" ", ""));
            int x = Int32.Parse(s.Substring(31, 3).Replace(" ", ""));
            double v = Double.Parse(s.Substring(34, 22).Replace(" ", ""));

            try {
                testpo(de, year, month, day, jd, t, c, x, v);
            } catch (FileNotFoundException e) {
                Console.WriteLine(e);
                skipped += 1;
            }
        }

        static void runTestFile(String filename)
        {
            StreamReader f = new StreamReader(filename);

            String l = f.ReadLine();
            while (l.Length < 3 || !l.Substring(0, 3).Equals("EOT")) {
                l = f.ReadLine();
            }

            l = f.ReadLine();
            while (l != null) {
                parseTestCase(l);
                l = f.ReadLine();
            }

            f.Close();
        }

        static void runVersionTest(String binaryFile, String testFile)
        {
            de = new JPLDE.DE(binaryFile);
            au = de.getHeader().au;
            runTestFile(testFile);
        }

        public static void RunTests()
        {
            String path = "E:\\Astronomy\\_Ephemeris\\JPLDEBinaries\\";
            String[] versions = { "102", "200", "202", "403", "405", "406", "410", "413", "414", "418", "421", "422", "423", "424", "430", "430t", "431", "432", "432t", "433", "434", "435", "436", "436t", "438", "438t", "440", "440t", "441" };
            //String[] versions={"405"};

            for (int i = 0; i < versions.Length; i++) {
                tests = 0;
                fail = 0;
                skipped = 0;
                pass = 0;

                String v = versions[i];
                runVersionTest(path + "jpleph." + v, path + "testpo." + v);

                Console.WriteLine();
                Console.WriteLine(v);
                Console.WriteLine("Tests ran:" + tests + " Failed:" + fail + " Skipped:" + skipped);
            }
        }
    }
}
