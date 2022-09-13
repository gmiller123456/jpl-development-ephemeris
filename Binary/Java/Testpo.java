import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;

/**
 * Created by Greg on 9/7/2022.
 */
public class Testpo {

    static int[] map={0,0,1,13,3,4,5,6,7,8,9,10,12,2,11,12,0,14};
    static JPLDE de=null;

    static int skipped=0;
    static int fail=0;
    static int pass=0;
    static int tests=0;

    static double au=0.149597870700000000E+09;

    static double get(int series,double jd,int x) throws Exception {
        //0 means no center, and 12 is the SSB which is always 0
        if (series == 12 || series == 0) {
            return 0;
        }

        int s = map[series];

        double[] b = {0, 0, 0, 0, 0, 0};

        if (series == 3) {
            double[] emb = de.getPlanet(2, jd);
            double[] moon = de.getPlanet(9, jd);
            b = JPLDE.getEarthPositionFromEMB(emb, moon);
        } else if (series == 10) {
            double[] emb = de.getPlanet(2, jd);
            double[] moon = de.getPlanet(9, jd);
            double[] earth = JPLDE.getEarthPositionFromEMB(emb, moon);
            for(int i=0;i<6;i++) {
                moon[i] += earth[i];
            }
            b = moon;
        } else {
            b = de.getPlanet(s, jd);
        }
        return b[x - 1];
    }

    static void testpo(int deNumber,int year,int month,int day,double jd,int t,int c,int x,double expectedValue) throws Exception {
        if (jd < de.jdStart || jd >= de.jdEnd) {
            skipped = skipped + 1;
            return;
        }

        double t1 = get(t, jd, x);
        double t2 = get(c, jd, x);

        double v = t1 - t2;

        if (t != 15 && t != 14 && t != 17) v /= au;

        double error = Math.abs(v - expectedValue);

        if (error > 1.0E-8 || Double.isNaN(error)) {
            String status = "Fail: "+jd+"\t"+t+"\t"+c+"\t"+x+"\t"+expectedValue+"\t"+v+"\tDiff="+error;
            System.out.println(status);
            fail++;
        } else {
            //String status = "Pass: "+jd+"\t"+t+"\t"+c+"\t"+x+"\t"+expectedValue+"\t"+v+"\tDiff="+error;
            //System.out.println(status);
        }
        tests++;
    }

    static void parseTestCase(String s) throws Exception{
        int de = Integer.parseInt(s.substring(0, 3).replaceAll(" ",""));
        int year = Integer.parseInt(s.substring(4, 9).replaceAll(" ", ""));
        int month = Integer.parseInt(s.substring(10, 12).replaceAll(" ", ""));
        int day = Integer.parseInt(s.substring(13, 15).replaceAll(" ", ""));
        double jd = Double.parseDouble(s.substring(15, 25).replaceAll(" ", ""));
        int t = Integer.parseInt(s.substring(25, 28).replaceAll(" ", ""));
        int c = Integer.parseInt(s.substring(28, 31).replaceAll(" ", ""));
        int x = Integer.parseInt(s.substring(31, 34).replaceAll(" ", ""));
        double v = Double.parseDouble(s.substring(34, 56).replaceAll(" ", ""));

        try {
            testpo(de, year, month, day, jd, t, c, x, v);
        } catch (FileNotFoundException e)  {
            System.out.println(e);
            skipped += 1;
        }
    }

    static void runTestFile(String filename) throws Exception {
        BufferedReader f = new BufferedReader(new FileReader(filename));

        String l = f.readLine();
        while (l.length()<3 || !l.substring(0,3).equals("EOT")){
            l = f.readLine();
        }

        l=f.readLine();
        while(l!=null) {
            parseTestCase(l);
            l=f.readLine();
        }

        f.close();
    }

    static void runVersionTest(String binaryFile, String testFile) throws Exception {
        de = new JPLDE(binaryFile);
        au = de.getHeader().au;
        runTestFile(testFile);
    }

    public static void main(String[] args) throws Exception{
        String path="E:\\Astronomy\\_Ephemeris\\JPLDEBinaries\\";
        String[] versions={"102","200","202","403","405","406","410","413","414","418","421","422","423","424","430","430t","431","432","432t","433","434","435","436","436t","438","438t","440","440t","441"};
        //String[] versions={"431"};

        for(int i=0;i<versions.length;i++) {
            tests = 0;
            fail = 0;
            skipped = 0;
            pass=0;

            String v = versions[i];
            runVersionTest(path + "jpleph." + v, path + "testpo." + v);

            System.out.println();
            System.out.println(v);
            System.out.println("Tests ran:"+tests+" Failed:"+fail+" Skipped:"+skipped);
        }
    }
}
