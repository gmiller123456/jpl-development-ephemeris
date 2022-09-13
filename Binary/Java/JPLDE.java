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

JPLDE de=new JPLDE("E:\\Astronomy\\_Ephemeris\\JPLDEBinaries\\jpleph.405");
System.out.println(de.getPlanet(1,de.getHeader().jdStart)[0]);

2.4857048341240495E7
*/

import java.io.FileInputStream;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.DoubleBuffer;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * Created by Greg on 9/7/2022.
 */
public class JPLDE {
    private JPLDEHeaderData header=null;

    String filename;
    double jdStart;
    double jdEnd;
    double jdStep;
    static double au=0.149597870691000015E+09;
    static double emrat=0.813005600000000044E+02;
    ArrayList<long[]> coeffPtr;
    int blockSize;
    int[] seriesVars={3,3,3,3,3,3,3,3,3,3,3,2,3,3,1};
    double[] cachedBlock=null;

    long cachedBlockNum=-1;
    ByteOrder endian=ByteOrder.LITTLE_ENDIAN;

    JPLDE(String filename) throws IOException{
        header=JPLDEHeader.loadHeader(filename);
        this.filename=filename;
        jdStart=header.jdStart;
        jdEnd=header.jdEnd;
        jdStep=header.jdStep;
        blockSize=(int)header.blockSize;
        coeffPtr=header.coeffPtr;
        au=header.au;
        emrat=header.emrat;
        endian=header.endian;
    }

    public JPLDEHeaderData getHeader(){
        return header;
    }

    public static double[] getEarthPositionFromEMB(double[] emb,double[] moon) {
        double[] earth ={0.0, 0.0, 0.0, 0.0, 0.0, 0.0};
        for(int i=0;i<6;i++) {
            earth[i] = emb[i] - moon[i] / (1.0 + emrat);
        }
        return earth;
    }

    double[] getBlockForJD(double jd) throws IOException{
        double jdoffset=jd-jdStart;
        long blockNum=(int)Math.floor(jdoffset / jdStep);

        if(!(cachedBlockNum==blockNum)) {
            FileInputStream f=new FileInputStream(filename);
            f.skip(blockNum * blockSize + 2 * blockSize);
            byte[] bin=new byte[blockSize];
            f.read(bin);
            f.close();

            ByteBuffer bb=ByteBuffer.wrap(bin).order(endian);
            DoubleBuffer db=bb.asDoubleBuffer();
            double[] block=new double[db.remaining()];
            db.get(block);
            cachedBlock=block;
            cachedBlockNum = blockNum;
        }

        return cachedBlock;
    }

    public double[] getPlanet(int planet,double jd) throws IOException{
        double[] block = getBlockForJD(jd);
        long[] d = coeffPtr.get(planet);
        int seriesOffset = (int)d[0] - 1;
        int ccount = (int)d[1];
        int subint = (int)d[2];
        int varCount = header.seriesVars[planet];

        double startJD = block[0];
        double endJD = block[1];
        double blockDuration = endJD - startJD;

        int subintervalDuration = (int) Math.floor(blockDuration / subint);
        int subintervalSize = ccount * varCount;
        int subintervalNumber = (int) (Math.floor((jd - startJD) / subintervalDuration));
        int subintervalStart = subintervalDuration * subintervalNumber;
        int subintervalEnd = subintervalDuration * subintervalNumber + subintervalDuration;

        //Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
        //If using two doubles for JD, this is where the two parts should be combined:
        //e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
        double x = ((jd - (startJD + subintervalStart)) / subintervalDuration) * 2 - 1;

        double[] properties = new double[6];
        for (int i = 0; i < varCount; i++) {
            int offset = seriesOffset + i * ccount + subintervalSize * subintervalNumber;
            double[] coeff=new double[ccount];
            DoubleBuffer.wrap(block,offset,ccount).get(coeff);
            double[] t = computePolynomial(x,coeff);
            properties[i] = t[0];

            double velocity = t[1];
            velocity = velocity * ((2.0) * subint / blockDuration);
            properties[i + varCount] = velocity;
        }

        return properties;
    }

    double[] computePolynomial(double x,double[] coefficients) {
        double[] t=new double[coefficients.length];
        //Equation 14.20 from Explanetory Supplement 3 rd ed.
        t[0]=1.0;
        t[1]= x;

        for(int n=2;n<coefficients.length;n++) {
            t[n] = 2 * x * t[n - 1] - t[n - 2];
        }

        //Multiply the polynomial by the coefficients.
        //Loop through coefficients backwards (from smallest to largest)to avoid floating point rounding errors
        double position = 0;
        for(int i=coefficients.length - 1; i>=0; i--) {
            position += coefficients[i] * t[i];
        }

        //Compute velocity (just the derivitave of the above)
        double[] v=new double[coefficients.length];
        v[0]=0.0;
        v[1]=1.0;
        v[2]=4.0*x;
        for(int n=3;n<coefficients.length;n++) {
            v[n]=(2 * x * v[n - 1] + 2 * t[n - 1] - v[n - 2]);
        }

        double velocity = 0.0;
        for(int i=coefficients.length - 1; i>=0;i--) {
            velocity += v[i] * coefficients[i];
        }

        double[] r=new double[2];
        r[0]=position;
        r[1]=velocity;
        return r;
    }
}
