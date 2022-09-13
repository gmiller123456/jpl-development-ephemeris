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

Example:
-----------------------------------------------
async function test(){
    data=await fetchJSON();
    const dv=new DataView(data);
    de=new JPLDE(dv);

    console.log(de.getPlanet(0,2451736.5));
}

async function fetchJSON() {
    const response = await fetch('jpleph2000-2040.405');
    const blob = await response.blob();
    const data= await blob.arrayBuffer();
    return data;
}

test();
*/

class JPLDE{
    data=null;
    constructor(data){
        this.littleEndian=true;
        this.data=data;
        this.header=JPLDEHeader.readHeader(data);

        this.jdStart = this.header.jdStart;
        this.jdEnd = this.header.jdEnd;
        this.jdStep = this.header.jdStep;
        this.blockSize = this.header.blockSize;
        this.coeffPtr = this.header.coeffPtr;
        this.au = this.header.au;
        this.emrat = this.header.emrat;
    }

    getHeader(){
        return this.header;
    }

    getEarthPositionFromEMB(emb, moon){
        const earth = [ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 ];
        for (let i = 0; i < 6; i++){
            earth[i] = emb[i] - moon[i] / (1.0 + this.emrat);
        }
        return earth;
    }

    getBlockForJD(jd){
        const jdoffset = jd - this.jdStart;
        const blockNum = Math.floor(jdoffset / this.jdStep);

        const offset=blockNum * this.blockSize + 2 * this.blockSize;
        const block=new DataView(this.data.buffer,offset,this.blockSize);

        return block;
    }

    getPlanet(planet, jd){
        const block = this.getBlockForJD(jd);
        const d = this.coeffPtr[planet];
        const seriesOffset = d[0] - 1;
        const ccount = d[1];
        const subint = d[2];
        const varCount = this.header.seriesVars[planet];

        const startJD = block.getFloat64(0,this.littleEndian);
        const endJD = block.getFloat64(8,this.littleEndian);
        const blockDuration = endJD - startJD;

        const subintervalDuration = Math.floor(blockDuration / subint);
        const subintervalSize = ccount * varCount;
        const subintervalNumber = (Math.floor((jd - startJD) / subintervalDuration));
        const subintervalStart = subintervalDuration * subintervalNumber;
        const subintervalEnd = subintervalDuration * subintervalNumber + subintervalDuration;

        //Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
        //If using two doubles for JD, this is where the two parts should be combined:
        //e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
        const x = ((jd - (startJD + subintervalStart)) / subintervalDuration) * 2 - 1;

        const properties = [0,0,0,0,0,0];
        for (let i = 0; i < varCount; i++){
            const offset = seriesOffset + i * ccount + subintervalSize * subintervalNumber;

            const coeff=new Array();
            for(let j=0;j<ccount;j++){
                coeff[j]=block.getFloat64(offset*8+j*8,this.littleEndian)
            }

            const t = this.computePolynomial(x, coeff);
            properties[i] = t[0];

            let velocity = t[1];
            velocity = velocity * ((2.0) * subint / blockDuration);
            properties[i + varCount] = velocity;
        }

        return properties;
    }

    computePolynomial(x, coefficients){
        const t = new Array();
        //Equation 14.20 from Explanetory Supplement 3 rd ed.
        t[0] = 1.0;
        t[1] = x;

        for (let n = 2; n < coefficients.length; n++){
            t[n] = 2 * x * t[n - 1] - t[n - 2];
        }

        //Multiply the polynomial by the coefficients.
        //Loop through coefficients backwards (from smallest to largest)to avoid floating point rounding errors
        let position = 0;
        for (let i = coefficients.length - 1; i >= 0; i--)
        {
            position += coefficients[i] * t[i];
        }

        //Compute velocity (just the derivitave of the above)
        const v = new Array();
        v[0] = 0.0;
        v[1] = 1.0;
        v[2] = 4.0 * x;
        for (let n = 3; n < coefficients.length; n++)
        {
            v[n] = (2 * x * v[n - 1] + 2 * t[n - 1] - v[n - 2]);
        }

        let velocity = 0.0;
        for (let i = coefficients.length - 1; i >= 0; i--)
        {
            velocity += v[i] * coefficients[i];
        }

        const r = new Array();
        r[0] = position;
        r[1] = velocity;
        return r;
    }
}

class JPLHeaderData{
    description;
    startString;
    endString;
    constantNames;
    jdStart;
    jdEnd;
    jdStep;
    numConstants;
    au;
    emrat;
    coeffPtr;
    version;
    blockSize;
    constants;
    seriesVars = [3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 3, 3, 1];
}

class JPLDEHeader{

    static getString(dv,offset,length){
        //Gets a string from a data view.
        let s="";
        for(let i=0;i<length;i++){
            const v=dv.getUint8(offset+i);
            s+=String.fromCharCode(v);
        }
        return s;
    }

    static findRecLength(h){
        for (let i = h.coeffPtr.length - 1; i >= 0; i--){
            if (h.coeffPtr[i][0] != 0)
            {
                const cp = h.coeffPtr[i];
                const reclen = cp[0] + (cp[1] * cp[2] * h.seriesVars[i]) - 1;
                return reclen * 8;
            }
        }
        throw new Exception("Header info contains no coefficient offsets.");
    }

    //data parameter should be a DataView
    static readHeader(data){
        const le=true; //Use little endian format
        const h = new JPLHeaderData();

        h.description = this.getString(data,0, 84);
        h.startString = this.getString(data,84, 84);
        h.endString = this.getString(data,168, 84);

        h.constantNames = [];
        for (let i = 0; i < 400; i++)
        {
            h.constantNames[i]=this.getString(data,252 + i * 6, 6);
        }
        //console.log(h.constantNames);

        h.jdStart=data.getFloat64(2652,le);
        h.jdEnd = data.getFloat64(2660,le);
        h.jdStep = data.getFloat64(2668,le);
        h.numConstants = data.getUint32(2676,le);
        h.au = data.getFloat64(2680,le);
        h.emrat = data.getFloat64(2688,le);

        h.coeffPtr = [];
        let t=[];
        //Group 1050 data
        for (let i = 0; i < 12; i++)
        {
            t = [0,0,0];
            t[0] = data.getUint32(2696 + i * 3 * 4,le);
            t[1] = data.getUint32(2700 + i * 3 * 4,le);
            t[2] = data.getUint32(2704 + i * 3 * 4,le);
            h.coeffPtr[i]=t;
        }

        h.version = data.getUint32(2840,le);

        //more Group 1050 data
        t = [0,0,0];
        t[0] = data.getUint32(2844,le);
        t[1] = data.getUint32(2848,le);
        t[2] = data.getUint32(2852,le);
        h.coeffPtr[h.coeffPtr.length]=t;

        let offset = 2856;
        //more constant names, if there's more than 400
        if (h.numConstants > 400)
        {
            for (let i = 0; i < h.numConstants - 400; i++)
            {
                h.constantNames[h.constantNames.length]=this.getString(data, 2856 + i * 6, 6);
            }
            offset = 2856 + ((h.numConstants - 400) * 6);
        }

        //more Group 1050 data
        t = [0,0,0];
        t[0] = data.getUint32(offset,le);
        t[1] = data.getUint32(offset+4,le);
        t[2] = data.getUint32(offset+8,le);
        h.coeffPtr[h.coeffPtr.length]=t;

        //more Group 1050 data
        t = [0,0,0];
        t[0] = data.getUint32(offset+12,le);
        t[1] = data.getUint32(offset+16,le);
        t[2] = data.getUint32(offset+20,le);
        h.coeffPtr[h.coeffPtr.length]=t;

        //Compute block size based on offsets in Group 1050
        h.blockSize = this.findRecLength(h);

        h.constants = [];
        for (let i = 0; i < h.numConstants; i++)
        {
            h.constants[i] = data.getFloat64(h.blockSize + i * 8,le);
        }

        return h;
    }
}