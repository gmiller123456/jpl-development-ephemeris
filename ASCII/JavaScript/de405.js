//Example lib to compute DE405 Development Ephemeris from JPL
//Greg Miller (gmiller@gregmiller.net) 2019
//Released as public domain

//TODO:
//Determine which file JD falls in
//Load proper file

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

		//Parameters 2,4,5 are from "GROUP 1050" in header file
		//Paremeter 3 must be inferred from the type of series
		const series=new Array();
		series[0]=new JPLSeries("mercury",3,3,14,4);
		series[1]=new JPLSeries("venus",171,3,10,2);
		series[2]=new JPLSeries("emb",231,3,13,2);
		series[3]=new JPLSeries("mars",309,3,11,1);
		series[4]=new JPLSeries("jupiter",342,3,8,1);
		series[5]=new JPLSeries("saturn",366,3,7,1);
		series[6]=new JPLSeries("uranus",387,3,6,1);
		series[7]=new JPLSeries("neptune",405,3,6,1);
		series[8]=new JPLSeries("pluto",423,3,6,1);
		series[9]=new JPLSeries("moon",441,3,13,8);
		series[10]=new JPLSeries("sun",753,3,11,2);
		series[11]=new JPLSeries("nutation",819,2,10,4);
		series[12]=new JPLSeries("libration",899,3,10,4);
		//series[13]=JPLSeries(de,"mantle-velocity",0,0,0,0);
		//series[14]=JPLSeries(de,"tt-tdb",0,0,0,0);
		this.series=series;
		this.coefficients=de405;

		this.chunkStart=this.coefficients[0];
		this.chunkEnd=this.coefficients[this.coefficients.length-this.coefficientsPerBlock+1];
	}

	getAllPropertiesForSeries(series,JD){
		let blockNumber=Math.floor((JD-this.chunkStart)/this.daysPerBlock);
		const blockOffset=blockNumber*(this.coefficientsPerBlock);
		return this.series[series].getAllPropertiesForSeries(JD,this.coefficients,blockOffset);
	}

	getEarth(JD){
		let moon=this.getAllPropertiesForSeries(9,JD);
		let emb=this.getAllPropertiesForSeries(2,JD);
		return DE405.getEarthPositionFromEMB(emb,moon);
	}

	static getEarthPositionFromEMB(emb,moon){
		const earthMoonRatio=0.813005600000000044E+02
		const earth=new Array();
		for(let i=0;i<6;i++){
			earth[i]=emb[i]-moon[i]/(1+earthMoonRatio);
		}
		return earth;
	}
}

class JPLSeries{
	constructor(seriesName,seriesOffset,numberOfProperties,numberOfCoefficients,numberOfSubIntervals){
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

		const properties=new Array();
		for(let i=0;i<this.numberOfProperties;i++){
			const offset=blockOffset+this.offset+i*this.numberOfCoefficients+subintervalSize*subintervalNumber;
			let t=this.computePropertyForSeries(x,coefficients,offset);
			properties[i]=t[0];

			let velocity = t[1];
			velocity=velocity*(2.0*this.numberOfSubIntervals/blockDuration);
			properties[i+this.numberOfProperties]=velocity;
		}
		return properties;
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
		let position=0;
		for(let i=coefficients.length-1;i>=0;i--){
			position+=coefficients[i]*t[i];
		}

		//Compute velocity (just the derivitave of the above)
		const v=new Array();
		v[0]=0;
		v[1]=1;
		v[2]=4*x;
		for(let n=3;n<coefficients.length;n++){
			v[n]=2*x*v[n-1]+2*t[n-1]-v[n-2];
		}

		let velocity=0;
		for(let i=coefficients.length-1;i>=0;i--){
			velocity+=v[i]*coefficients[i];
		}

		let retval=new Array();
		retval[0]=position;
		retval[1]=velocity;
		return retval;
	}
}
