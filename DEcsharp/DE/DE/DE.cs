/*
# Example lib to compute Development Ephemeris from JPL
# Greg Miller (gmiller@gregmiller.net) 2020
# Released as public domain
*/
using System;
using System.Text.RegularExpressions;

//TODO: Remove ../../.... from file path.

namespace DE
{
    class DE
    {
        int name;
        double start;
        double end;
        int daysPerBlock;
        int coefficientsPerBlock;
        int yearsPerFile;
        JPLSeries[] series;
        FileManager fileManager;

        public DE(object[] data) {

            this.name = (int)data[0];
            this.start = (double)data[6];
            this.end = (double)data[7];
            this.daysPerBlock = (int)data[8];
            this.coefficientsPerBlock = (int)data[3] + 2;  //+2 because each block is padded with two zeros
            this.yearsPerFile = (int)data[2];

            fileManager = new FileManager(this.name,(string)data[1], this.yearsPerFile, this.daysPerBlock, this.coefficientsPerBlock);

            int propertyCountIndex = 9;
            int propertyCount = (int)data[propertyCountIndex];

            this.series = new JPLSeries[propertyCount];
            for (int i = 0; i < propertyCount; i++) {
                series[i] = new JPLSeries((string)data[i + propertyCountIndex + 1 + propertyCount * 0],
                    (int)data[i + propertyCountIndex + 1 + propertyCount * 1],
                    (int)data[i + propertyCountIndex + 1 + propertyCount * 4],
                    (int)data[i + propertyCountIndex + 1 + propertyCount * 2],
                    (int)data[i + propertyCountIndex + 1 + propertyCount * 3]);
            }
        }

        public double[] getAllPropertiesForSeries(int series, double JD)
        {
            this.fileManager.loadFileForJD(JD);
            int blockNumber = (int)Math.Floor((JD - this.fileManager.chunkStart) / this.daysPerBlock);
            int blockOffset = blockNumber * (this.coefficientsPerBlock);

            return this.series[series].getAllPropertiesForSeries(JD, this.fileManager.coefficients, blockOffset);
        }

        public static double[] getEarthPositionFromEMB(double[] emb, double[] moon)
        {
            const double earthMoonRatio = 0.813005600000000044E+02;
            double[] earth = { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < 6; i++) {
                earth[i] = emb[i] - moon[i] / (1.0 + earthMoonRatio);
            }
            return earth;
        }
    }

    class JPLSeries
    {

        string name;
        int seriesOffset;
        int numberOfProperties;
        int numberOfCoefficients;
        int numberOfSubIntervals;

        public JPLSeries(string name, int offset, int numberOfProperties, int numberOfCoefficients, int numberOfSubIntervals)
        {
            this.name = name;
            this.seriesOffset = offset - 1;
            this.numberOfProperties = numberOfProperties;
            this.numberOfCoefficients = numberOfCoefficients;
            this.numberOfSubIntervals = numberOfSubIntervals;
        }

        public double[] getAllPropertiesForSeries(double JD, double[] coefficients, int blockOffset)
        {

            double startJD = coefficients[0 + blockOffset];
            double endJD = coefficients[1 + blockOffset];
            double blockDuration = endJD - startJD;
            double subintervalDuration = blockDuration / this.numberOfSubIntervals;
            int subintervalSize = this.numberOfCoefficients * this.numberOfProperties;
            int subintervalNumber = (int)Math.Floor((JD - startJD) / subintervalDuration);
            double subintervalStart = subintervalDuration * subintervalNumber;
            double subintervalEnd = subintervalDuration * subintervalNumber + subintervalDuration;

            //Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
            //If using two doubles for JD, this is where the two parts should be combined:
            //e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
            double jd = JD - (startJD + subintervalStart);
            double x = (jd / subintervalDuration) * 2 - 1;

            double[] properties = { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < this.numberOfProperties; i++)
            {
                int offset = blockOffset + this.seriesOffset + i * this.numberOfCoefficients + subintervalSize * subintervalNumber;
                double[] t = this.computePolynomial(x, coefficients, offset);
                properties[i] = t[0];

                double velocity = t[1];
                velocity = velocity * (2.0 * this.numberOfSubIntervals / blockDuration);
                properties[i + this.numberOfProperties] = velocity;
            }
            return properties;
        }

        double[] computePolynomial(double x, double[] coefficients, int offset)
        {

            // Equation 14.20 from Explanetory Supplement 3rd ed.
            double[] t = new double[this.numberOfCoefficients];
            t[0] = 1.0;
            t[1] = x;

            for (int n = 2; n < this.numberOfCoefficients; n++)
            {
                double tn = 2 * x * t[n - 1] - t[n - 2];
                t[n] = tn;
            }

            //Multiply the polynomial by the coefficients.
            //Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
            double position = 0;
            for (int i = this.numberOfCoefficients - 1; i > -1; i--)
            {
                position += coefficients[i + offset] * t[i];
            }

            //Compute velocity (just the derivitave of the above)
            double[] v = new double[this.numberOfCoefficients];
            v[0] = 0.0;
            v[1] = 1.0;
            v[2] = 4 * x;
            for (int n = 3; n < this.numberOfCoefficients; n++)
            {
                v[n] = 2 * x * v[n - 1] + 2 * t[n - 1] - v[n - 2];
            }

            double velocity = 0;
            for (int i = this.numberOfCoefficients - 1; i > -1; i--)
            {
                velocity += v[i] * coefficients[i + offset];
            }

            double[] r = new double[2];
            r[0] = position;
            r[1] = velocity;
            return r;
        }
    }

    class FileManager
    {
        int name;
        string fileBaseName;
        int fileBase;
        int fileNamePad;
        int yearsPerFile;
        string loadedFile;
        internal double chunkStart;
        double chunkEnd;
        internal double[] coefficients;
        int daysPerBlock;
        int coefficientsPerBlock;

        public FileManager(int name, string fileBaseName, int yearsPerFile, int daysPerBlock, int coefficientsPerBlock)
        {
            this.loadedFile = "";
            this.name = name;
            this.yearsPerFile = yearsPerFile;
            this.fileBaseName = fileBaseName;
            this.daysPerBlock = daysPerBlock;
            this.coefficientsPerBlock = coefficientsPerBlock;

            this.fileBase = int.Parse(this.fileBaseName.Substring(4, 4));
            this.fileNamePad = 4;

            if (fileBaseName.Substring(9, 1).Equals("."))
            {
                fileBase = int.Parse(fileBaseName.Substring(4, 5));
                fileNamePad = 5;
            }

            if (fileBaseName.Substring(3, 1).Equals("m"))
            {
                fileBase *= -1;
            }
        }

        double[] loadFile(string filename)
        {
            double[] coefficients = new double[this.daysPerBlock * (this.coefficientsPerBlock + 2) * this.yearsPerFile * 366];

            System.IO.StreamReader f = new System.IO.StreamReader("../../../../../de" + this.name + "/" + filename);
            int count = 0;
            string l = f.ReadLine();
            while (l != null)
            {
                if (l.Length > 17 && !l.Substring(17, 1).Equals(" "))
                {
                    string[] t = Regex.Split(l, " +");
                    for (int i = 1; i < 4; i++)
                    {
                        if (i > t.Length - 1)
                        {
                            coefficients[count] = 0.0;
                            count++;
                        }
                        else
                        {
                            t[i] = t[i].Replace("D", "e");
                            coefficients[count] = Double.Parse(t[i]);
                            count++;
                        }
                    }
                }
                l = f.ReadLine();
            }

            f.Close();
            this.loadedFile = filename;
            this.chunkStart = coefficients[0];

            this.chunkEnd = coefficients[count - this.coefficientsPerBlock + 1];
            return coefficients;
        }

        public void loadFileForJD(double jd)
        {
            if (jd >= this.chunkStart && jd <= this.chunkEnd)
            {
                return ;
            }

            int year = (julainDateToGregorianYear(jd));
            string pm = "p";
            if (year < 0)
            {
                year = Math.Abs(year);
                pm = "m";
            }

            year = (int)Math.Floor((double)((year - this.fileBase) / this.yearsPerFile)) * this.yearsPerFile + this.fileBase;

            string fileName = String.Format("asc" + pm + "{0," + this.fileNamePad + "}." + this.name, year);
            this.coefficients=this.loadFile(fileName);
        }

        static int INT(double a)
        {
            return (int)Math.Truncate(a);
        }

        //From Meeus, CH7
        static int julainDateToGregorianYear(double jd)
        {
            double temp = jd + .5;
            int Z = (int)Math.Floor(temp);
            double F = temp - Z;
            int A = Z;

            if (Z >= 2299161)
            {
                int alpha = INT((Z - 1867216.25) / 36524.25);
                A = Z + 1 + alpha - INT(alpha / 4.0);
            }

            int B = A + 1524;
            int C = INT((B - 122.1) / 365.25);
            int D = INT(365.25 * C);
            int E = INT((B - D) / 30.6001);

            double day = B - D - INT(30.6001 * E) + F;
            int month = E - 1;
            if (E > 13)
            {
                month = E - 13;
            }

            int year = C - 4716;
            if (month < 3)
            {
                year = C - 4715;
            }

            return year;
        }
    }
}
