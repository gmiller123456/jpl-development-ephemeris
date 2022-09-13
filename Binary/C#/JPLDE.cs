/*
Greg Miller (gmiller@gregmiller.net) 2022
Released as public domain
http://www.celestialprogramming.com/

Class to read binary versions of JPL's Development Ephemeris.  Files in
the propper format can be obtained from:
ftp://ssd.jpl.nasa.gov/pub/eph/planets/Linux

#    Properties       Units          Center Description
0    x,y,z            km             SSB    Mercury
1    x,y,z            km             SSB    Venus
2    x,y,z            km             SSB    Earth-Moon barycenter
3    x,y,z            km             SSB    Mars
4    x,y,z            km             SSB    Jupiter
5    x,y,z            km             SSB    Saturn
6    x,y,z            km             SSB    Uranus
7    x,y,z            km             SSB    Neptune
8    x,y,z            km             SSB    Pluto
9    x,y,z            km             Earth  Moon (geocentric)
10   x,y,z            km             SSB    Sun
11   dPsi,dEps        radians               Earth Nutations in longitude and obliquity
12   phi,theta,psi    radians               Lunar mantle libration
13   Ox,Oy,Oz         radians/day           Lunar mantle angular velocity
14   t                seconds               TT-TDB (at geocenter)

Example: (prints x coordinate of venus using first JD available)

JPLDE.DE de = new JPLDE.DE(@"E:\Astronomy\_Ephemeris\JPLDEBinaries\jpleph.405");
Console.WriteLine(de.getPlanet(1, de.getHeader().jdStart)[0]);

24857048.3412405
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace JPLDE
{
    class DE
    {
        HeaderData header = null;

        String filename;
        double jdStart;
        double jdEnd;
        double jdStep;
        static double au = 0.149597870691000015E+09;
        static double emrat = 0.813005600000000044E+02;
        List<long[]> coeffPtr;
        int blockSize;
        int[] seriesVars = { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 1 };

        double[] cachedBlock = null;
        long cachedBlockNum = -1;

        public DE(String filename)
        {
            header = HeaderReader.loadHeader(filename);
            this.filename = filename;
            jdStart = header.jdStart;
            jdEnd = header.jdEnd;
            jdStep = header.jdStep;
            blockSize = (int)header.blockSize;
            coeffPtr = header.coeffPtr;
            au = header.au;
            emrat = header.emrat;
        }

        public HeaderData getHeader()
        {
            return header;
        }
        public static double[] getEarthPositionFromEMB(double[] emb, double[] moon)
        {
            double[] earth = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            for (int i = 0; i < 6; i++)
            {
                earth[i] = emb[i] - moon[i] / (1.0 + emrat);
            }
            return earth;
        }

        static double[] convertByteArrayToDoubleArray(byte[] b)
        {
            double[] a = new double[b.Length];

            for (int i = 0; i < b.Length/8; i++)
            {
                a[i] = BitConverter.ToDouble(b, i * 8);
            }

            return a;
        }

        double[] getBlockForJD(double jd)
        {
            double jdoffset = jd - jdStart;
            long blockNum = (int)Math.Floor(jdoffset / jdStep);

            if (!(cachedBlockNum == blockNum))
            {
                FileStream f = new FileStream(filename,FileMode.Open);
                f.Seek(blockNum * blockSize + 2 * blockSize, SeekOrigin.Begin);
                byte[] bin = new byte[blockSize];
                f.Read(bin, 0 ,bin.Length);
                f.Close();

                cachedBlock = convertByteArrayToDoubleArray(bin);
                cachedBlockNum = blockNum;
            }

            return cachedBlock;
        }
        public double[] getPlanet(int planet, double jd)
        {
            double[] block = getBlockForJD(jd);
            long[] d = coeffPtr[planet];
            int seriesOffset = (int)d[0] - 1;
            int ccount = (int)d[1];
            int subint = (int)d[2];
            int varCount = header.seriesVars[planet];

            double startJD = block[0];
            double endJD = block[1];
            double blockDuration = endJD - startJD;

            int subintervalDuration = (int)Math.Floor(blockDuration / subint);
            int subintervalSize = ccount * varCount;
            int subintervalNumber = (int)(Math.Floor((jd - startJD) / subintervalDuration));
            int subintervalStart = subintervalDuration * subintervalNumber;
            int subintervalEnd = subintervalDuration * subintervalNumber + subintervalDuration;

            //Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
            //If using two doubles for JD, this is where the two parts should be combined:
            //e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
            double x = ((jd - (startJD + subintervalStart)) / subintervalDuration) * 2 - 1;

            double[] properties = new double[6];
            for (int i = 0; i < varCount; i++)
            {
                int offset = seriesOffset + i * ccount + subintervalSize * subintervalNumber;
                double[] coeff = new double[ccount];

                Array.Copy(block, offset, coeff, 0,ccount);

                double[] t = computePolynomial(x, coeff);
                properties[i] = t[0];

                double velocity = t[1];
                velocity = velocity * ((2.0) * subint / blockDuration);
                properties[i + varCount] = velocity;
            }

            return properties;
        }
        double[] computePolynomial(double x, double[] coefficients)
        {
            double[] t = new double[coefficients.Length];
            //Equation 14.20 from Explanetory Supplement 3 rd ed.
            t[0] = 1.0;
            t[1] = x;

            for (int n = 2; n < coefficients.Length; n++)
            {
                t[n] = 2 * x * t[n - 1] - t[n - 2];
            }

            //Multiply the polynomial by the coefficients.
            //Loop through coefficients backwards (from smallest to largest)to avoid floating point rounding errors
            double position = 0;
            for (int i = coefficients.Length - 1; i >= 0; i--)
            {
                position += coefficients[i] * t[i];
            }

            //Compute velocity (just the derivitave of the above)
            double[] v = new double[coefficients.Length];
            v[0] = 0.0;
            v[1] = 1.0;
            v[2] = 4.0 * x;
            for (int n = 3; n < coefficients.Length; n++)
            {
                v[n] = (2 * x * v[n - 1] + 2 * t[n - 1] - v[n - 2]);
            }

            double velocity = 0.0;
            for (int i = coefficients.Length - 1; i >= 0; i--)
            {
                velocity += v[i] * coefficients[i];
            }

            double[] r = new double[2];
            r[0] = position;
            r[1] = velocity;
            return r;
        }

    }

    public class HeaderData
    {
        public String description;
        public String startString;
        public String endString;
        public List<String> constantNames;
        public double jdStart;
        public double jdEnd;
        public double jdStep;
        public long numConstants;
        public double au;
        public double emrat;
        public List<long[]> coeffPtr;
        public long version;
        public long blockSize;
        public double[] constants;
        public int[] seriesVars = { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 1 };
    }

    class HeaderReader
    {
        private static int BUFSIZE = 30000;

        static long findRecLength(HeaderData h)
        {
            for (int i = h.coeffPtr.Count - 1; i >= 0; i--)
            {
                if (h.coeffPtr[i][0] != 0)
                {
                    long[] cp = h.coeffPtr[i];
                    long reclen = cp[0] + (cp[1] * cp[2] * h.seriesVars[i]) - 1;
                    return reclen * 8;
                }
            }
            throw new Exception("Header info contains no coefficient offsets.");
        }

        public static HeaderData loadHeader(String filename)
        {
            HeaderData h = new HeaderData();
            FileStream f = new FileStream(filename, FileMode.Open);
            byte[] b = new byte[BUFSIZE];
            f.Read(b, 0, b.Length);
            f.Close();

            h.description = System.Text.Encoding.ASCII.GetString(b, 0, 84);
            h.startString = System.Text.Encoding.ASCII.GetString(b, 84, 84);
            h.endString = System.Text.Encoding.ASCII.GetString(b, 168, 84);

            h.constantNames = new List<String>();
            for (int i = 0; i < 400; i++)
            {
                h.constantNames.Add(System.Text.Encoding.ASCII.GetString(b, 252 + i * 6, 6));
            }

            h.jdStart = BitConverter.ToDouble(b, 2652);
            h.jdEnd = BitConverter.ToDouble(b, 2660);
            h.jdStep = BitConverter.ToDouble(b, 2668);
            h.numConstants = BitConverter.ToInt32(b, 2676);
            h.au = BitConverter.ToDouble(b, 2680);
            h.emrat = BitConverter.ToDouble(b, 2688);

            //Account for possible future versions containing enormous numbers of constants
            if (BUFSIZE < (h.numConstants * 6 + 4000))
            {
                f = new FileStream(filename, FileMode.Open);
                b = new byte[(int)h.numConstants * 6 + 4000];
                f.Read(b, 0, b.Length);
                f.Close();
            }

            h.coeffPtr = new List<long[]>();
            long[] t;
            //Group 1050 data
            for (int i = 0; i < 12; i++)
            {
                t = new long[3];
                t[0] = BitConverter.ToInt32(b, 2696 + i * 3 * 4);
                t[1] = BitConverter.ToInt32(b, 2700 + i * 3 * 4);
                t[2] = BitConverter.ToInt32(b, 2704 + i * 3 * 4);
                h.coeffPtr.Add(t);
            }

            h.version = BitConverter.ToInt32(b, 2840);

            //more Group 1050 data
            t = new long[3];
            t[0] = BitConverter.ToInt32(b, 2844);
            t[1] = BitConverter.ToInt32(b, 2848);
            t[2] = BitConverter.ToInt32(b, 2852);
            h.coeffPtr.Add(t);

            int offset = 2856;
            //more constant names, if there's more than 400
            if (h.numConstants > 400)
            {
                for (int i = 0; i < h.numConstants - 400; i++)
                {
                    h.constantNames.Add(System.Text.Encoding.ASCII.GetString(b, 2856 + i * 6, 6));
                }
                offset = 2856 + (int)((h.numConstants - 400) * 6);
            }

            //more Group 1050 data
            t = new long[3];
            t[0] = BitConverter.ToInt32(b, offset);
            t[1] = BitConverter.ToInt32(b, offset + 4);
            t[2] = BitConverter.ToInt32(b, offset + 8);
            h.coeffPtr.Add(t);

            //more Group 1050 data
            t = new long[3];
            t[0] = BitConverter.ToInt32(b, offset + 12);
            t[1] = BitConverter.ToInt32(b, offset + 16);
            t[2] = BitConverter.ToInt32(b, offset + 20);
            h.coeffPtr.Add(t);

            //Compute block size based on offsets in Group 1050
            h.blockSize = findRecLength(h);

            h.constants = new double[h.numConstants];
            for (int i = 0; i < h.numConstants; i++)
            {
                h.constants[i] = BitConverter.ToDouble(b, (int)h.blockSize + i * 8);
            }

            return h;
        }

    }
}
