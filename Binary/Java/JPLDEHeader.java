import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * Created by Greg on 9/7/2022.
 */
public class JPLDEHeader {
    private static int BUFSIZE=30000;

    static long findRecLength(JPLDEHeaderData h){
        for(int i=h.coeffPtr.size()-1;i>=0;i--) {
            if (h.coeffPtr.get(i)[0] != 0) {
                long[] cp = h.coeffPtr.get(i);
                long reclen = cp[0] + (cp[1] * cp[2] * h.seriesVars[i]) - 1;
                return reclen * 8;
            }
        }
        throw new RuntimeException("Header info contains no coefficient offsets.");
    }

    static JPLDEHeaderData loadHeader(String filename) throws IOException{
        JPLDEHeaderData h=new JPLDEHeaderData();
        FileInputStream f=new FileInputStream(filename);
        byte[] b=new byte[BUFSIZE];
        f.read(b);
        f.close();

        ByteBuffer bb=ByteBuffer.wrap(b).order(h.endian);

        h.description=new String(b,0,84);
        h.startString=new String(b,84,168);
        h.endString=new String(b,168,252);

        h.constantNames=new ArrayList<String>();
        for(int i=0;i<400;i++) {
            h.constantNames.add(new String(b,252+i*6,6));
        }

        h.jdStart=bb.getDouble(2652);
        h.jdEnd= bb.getDouble(2660);
        h.jdStep= bb.getDouble(2668);
        h.numConstants= bb.getInt(2676);
        h.au= bb.getDouble(2680);
        h.emrat= bb.getDouble(2688);

        //Account for possible future versions containing enormous numbers of constants
        if(BUFSIZE<(h.numConstants*6 + 4000)){
            f=new FileInputStream(filename);
            b=new byte[(int)h.numConstants*6 + 4000];
            f.read(b);
            f.close();
            bb=ByteBuffer.wrap(b).order(h.endian);
        }

        h.coeffPtr=new ArrayList<long[]>();
        //Group 1050 data
        for(int i=0;i<12;i++) {
            long[] t=new long[3];
            t[0]=bb.getInt(2696+i*3*4);
            t[1]=bb.getInt(2700+i*3*4);
            t[2]=bb.getInt(2704+i*3*4);
            h.coeffPtr.add(t);
        }

        h.version=bb.getInt(2840);

        //more Group 1050 data
        long[] t=new long[3];
        t[0]=bb.getInt(2844);
        t[1]=bb.getInt(2848);
        t[2]=bb.getInt(2852);
        h.coeffPtr.add(t);

        int offset=2856;
        //more constant names, if there's more than 400
        if(h.numConstants>400) {
            for(int i=0;i<h.numConstants - 400;i++) {
                h.constantNames.add(new String(b,2856+i*6,6));
            }
            offset=2856+(int)((h.numConstants-400)*6);
        }

        //more Group 1050 data
        t=new long[3];
        t[0]=bb.getInt(offset);
        t[1]=bb.getInt(offset+4);
        t[2]=bb.getInt(offset+8);
        h.coeffPtr.add(t);

        //more Group 1050 data
        t=new long[3];
        t[0]=bb.getInt(offset+12);
        t[1]=bb.getInt(offset+16);
        t[2]=bb.getInt(offset+20);
        h.coeffPtr.add(t);

        //Compute block size based on offsets in Group 1050
        h.blockSize=findRecLength(h);

        h.constants=new double[(int)h.numConstants];
        for(int i=0;i<h.numConstants;i++) {
            h.constants[i]=bb.getDouble((int)h.blockSize+i*8);
        }
        return h;
    }
}
