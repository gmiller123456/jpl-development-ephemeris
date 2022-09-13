import java.nio.ByteOrder;
import java.util.ArrayList;

/**
 * Created by Greg on 9/8/2022.
 */
public class JPLDEHeaderData {
    public String description;
    public String startString;
    public String endString;
    public ArrayList<String> constantNames;
    public double jdStart;
    public double jdEnd;
    public double jdStep;
    public long numConstants;
    public double au;
    public double emrat;
    public ArrayList<long[]> coeffPtr;
    public long version;
    public long blockSize;
    public double[] constants;
    public int[] seriesVars={3,3,3,3,3,3,3,3,3,3,3,2,3,3,1};
    public ByteOrder endian=ByteOrder.LITTLE_ENDIAN;
}
