//Example lib to compute DE405 Development Ephemeris from JPL
//Greg Miller (gmiller@gregmiller.net) 2019
//Released as public domain

//TODO:
//Determine which file JD falls in
//Load proper file
//Compute block within file for JD

class DE405{
	constructor(){
		//First three variables (after "name") are from "GROUP 1030" in header file
		//Last variable is NCOEFF from first line of header file
		this.name="de405";
		this.start=2305424.50;
		this.end=2525008.50;
		this.daysPerBlock=32;
		this.coefficientsPerBlock=1018 + 2;  //+2 because each block is padded with two zeros

		this.earthMoonRatio=0.813005600000000044E+02  //EMRAT from header constants section

		this.chunkStart=2458832.5;
		this.chunkEnd=2466160.5;

		//Parameters 2,4,5 are from "GROUP 1050" in header file
		//Paremeter 3 must be inferred from the type of series
		const series=new Array();
		series[0]=new JPLSeries(this,"mercury",3,3,14,4);
		series[1]=new JPLSeries(this,"venus",171,3,10,2);
		series[2]=new JPLSeries(this,"emb",231,3,13,2);
		series[3]=new JPLSeries(this,"mars",309,3,11,1);
		series[4]=new JPLSeries(this,"jupiter",342,3,8,1);
		series[5]=new JPLSeries(this,"saturn",366,3,7,1);
		series[6]=new JPLSeries(this,"uranus",387,3,6,1);
		series[7]=new JPLSeries(this,"neptune",405,3,6,1);
		series[8]=new JPLSeries(this,"pluto",423,3,6,1);
		series[9]=new JPLSeries(this,"moon",441,3,13,8);
		series[10]=new JPLSeries(this,"sun",753,3,11,2);
		series[11]=new JPLSeries(this,"nutation",819,2,10,4);
		series[12]=new JPLSeries(this,"libration",899,3,10,4);
		//series[13]=JPLSeries(de,"mantle-velocity",0,0,0,0);
		//series[14]=JPLSeries(de,"tt-tdb",0,0,0,0);
		this.series=series;
		this.coefficients=de405;
	}

	getAllPropertiesForSeries(series,JD){
		let blockNumber=Math.floor((JD-this.chunkStart)/this.daysPerBlock);
		const blockOffset=blockNumber*(this.coefficientsPerBlock);
		return this.series[series].getAllPropertiesForSeries(JD,this.coefficients,blockOffset);
	}

	static getEarthPositionFromEMB(emb,moon){
		const earth=new Array();
		for(let i=0;i<3;i++){
			earth[j]=emb[j]-moon[j]/(1+earthMoonRatio);
		}
		return earth;
	}
}

class JPLSeries{
	constructor(de,seriesName,seriesOffset,numberOfProperties,numberOfCoefficients,numberOfSubIntervals){
		this.de=de;
		this.name=seriesName;
		this.offset=seriesOffset-1;
		this.numberOfProperties=numberOfProperties;
		this.numberOfCoefficients=numberOfCoefficients;
		this.numberOfSubIntervals=numberOfSubIntervals;
	}

	getAllPropertiesForSeries(JD,coefficients,blockOffset){
		const startJD=coefficients[0+blockOffset];
		const endJD=coefficients[1+blockOffset];
		const blockDuration=endJD-startJD;
		const subintervalDuration=blockDuration/this.numberOfSubIntervals;
		const subintervalSize=this.numberOfCoefficients*this.numberOfProperties;
		const subintervalNumber=Math.floor((JD-startJD)/subintervalDuration);
		const subintervalStart=subintervalDuration*subintervalNumber;
		const subintervalEnd=subintervalDuration*subintervalNumber+subintervalDuration;

		//Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
		//If using two doubles for JD, this is where the two parts should be combined:
		//e.g. const jd=(JD[0]-(startJD+subintervalStart))+JD[1];
		const jd=JD-(startJD+subintervalStart);
		const x=(jd/subintervalDuration)*2-1;

		const position=new Array();
		for(let i=0;i<this.numberOfProperties;i++){
			const offset=blockOffset+this.offset+i*this.numberOfCoefficients+subintervalSize*subintervalNumber;
			position[i]=this.computePropertyForSeries(x,coefficients,offset);
		}
		return position;
	}

	computePropertyForSeries(x,coefficients,offset){
		const c=new Array();
		for(let i=0;i<this.numberOfCoefficients;i++){
			c[i]=coefficients[offset+i];
		}
		let p=this.computePolynomial(x,c);
		return p;
	}

	computePolynomial(x,coefficients){
		//Equation 14.20 from Explanetory Supplement 3rd ed.
		const t=new Array();
		t[0]=1;
		t[1]=x;

		for(let n=2;n<coefficients.length; n++){
			let tn=2*x*t[n-1]-t[n-2];
			t[n]=tn;
		}

		//Multiply the polynomial by the coefficients.
		//Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
		let pos=0;
		for(let i=coefficients.length-1;i>=0;i--){
			pos+=coefficients[i]*t[i];
		}
		return pos;
	}
}
