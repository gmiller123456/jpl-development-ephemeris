#!/usr/bin/python

#Example lib to compute DE405 Development Ephemeris from JPL
#Greg Miller (gmiller@gregmiller.net) 2019
#Released as public domain

#TODO:
#Determine which file JD falls in
#Load proper file
import re
from decimal import Decimal
import math

class JPLSeries:
	def __init__(self,seriesName,seriesOffset,numberOfProperties,numberOfCoefficients,numberOfSubIntervals):
		self.name=seriesName
		self.offset=seriesOffset-1
		self.numberOfProperties=numberOfProperties
		self.numberOfCoefficients=numberOfCoefficients
		self.numberOfSubIntervals=numberOfSubIntervals

	def getAllPropertiesForSeries(self,JD,coefficients,blockOffset):
		startJD=coefficients[0+blockOffset]
		endJD=coefficients[1+blockOffset]
		blockDuration=endJD-startJD
		subintervalDuration=blockDuration/self.numberOfSubIntervals
		subintervalSize=self.numberOfCoefficients*self.numberOfProperties
		subintervalNumber=math.floor((JD-startJD)/subintervalDuration)
		subintervalStart=subintervalDuration*subintervalNumber
		subintervalEnd=subintervalDuration*subintervalNumber+subintervalDuration

		#Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
		#If using two doubles for JD, this is where the two parts should be combined:
		#e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
		jd=JD-(startJD+subintervalStart)
		x=(jd/subintervalDuration)*2-1

		properties=[0,0,0,0,0,0]
		for i in range(self.numberOfProperties):
			offset=blockOffset+self.offset+i*self.numberOfCoefficients+subintervalSize*subintervalNumber
			t=self.computePropertyForSeries(x,coefficients,offset)
			properties[i]=t[0]

			velocity = t[1]
			velocity=velocity*(Decimal(2.0)*self.numberOfSubIntervals/blockDuration)
			properties[i+self.numberOfProperties]=velocity
		return properties

	def computePropertyForSeries(self,x,coefficients,offset):
		c=[]
		for i in range(self.numberOfCoefficients):
			c.append(coefficients[offset+i])
		p=self.computePolynomial(x,c)
		return p

	def computePolynomial(self,x,coefficients):
		#Equation 14.20 from Explanetory Supplement 3rd ed.
		t=[]
		t.append(1);
		t.append(x);

		for n in range(2,len(coefficients)):
			tn=2*x*t[n-1]-t[n-2]
			t.append(tn)

		#Multiply the polynomial by the coefficients.
		#Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
		position=0
		for i in range(len(coefficients)-1,-1,-1):
			position+=coefficients[i]*t[i]

		#Compute velocity (just the derivitave of the above)
		v=[]
		v.append(0)
		v.append(1)
		v.append(4*x)
		for n in range(3,len(coefficients)):
			v.append(2*x*v[n-1]+2*t[n-1]-v[n-2])

		velocity=0
		for i in range(len(coefficients)-1,-1,-1):
			velocity+=v[i]*coefficients[i]

		retval=[]
		retval.append(position)
		retval.append(velocity)
		return retval

class DE405:
	def __init__(self):
		#First three variables (after "name") are from "GROUP 1030" in header file
		#Last variable is NCOEFF from first line of header file
		self.name="de405"
		self.start=2305424.50
		self.end=2525008.50
		self.daysPerBlock=32
		self.coefficientsPerBlock=1018 + 2  #+2 because each block is padded with two zeros

		#Parameters 2,4,5 are from "GROUP 1050" in header file
		#Paremeter 3 must be inferred from the type of series
		series=[]
		series.append(JPLSeries("mercury",3,3,14,4))
		series.append(JPLSeries("venus",171,3,10,2))
		series.append(JPLSeries("emb",231,3,13,2))
		series.append(JPLSeries("mars",309,3,11,1))
		series.append(JPLSeries("jupiter",342,3,8,1))
		series.append(JPLSeries("saturn",366,3,7,1))
		series.append(JPLSeries("uranus",387,3,6,1))
		series.append(JPLSeries("neptune",405,3,6,1))
		series.append(JPLSeries("pluto",423,3,6,1))
		series.append(JPLSeries("moon",441,3,13,8))
		series.append(JPLSeries("sun",753,3,11,2))
		series.append(JPLSeries("nutation",819,2,10,4))
		series.append(JPLSeries("libration",899,3,10,4))
		#series[13]=JPLSeries(de,"mantle-velocity",0,0,0,0)
		#series[14]=JPLSeries(de,"tt-tdb",0,0,0,0)
		self.series=series
		self.loadedFile=""
		self.loadFile("ascp2020.405")

		self.chunkStart=self.coefficients[0]
		self.chunkEnd=self.coefficients[len(self.coefficients)-self.coefficientsPerBlock+1]

	def loadFile(self,filename):
		if(self.loadedFile==filename):
			return

		self.coefficients=[]
		f=open(filename,"r")
		for l in f:
			if(l[2:3] != " "):
				t=re.split(" +",l);
				for i in range(1,4):
					self.coefficients.append(Decimal(t[i].replace("D","e")));
		f.close()
		self.loadedFile=filename

	def getAllPropertiesForSeries(self,series,JD):
		blockNumber=math.floor((JD-self.chunkStart)/self.daysPerBlock)
		blockOffset=blockNumber*(self.coefficientsPerBlock)
		return self.series[series].getAllPropertiesForSeries(JD,self.coefficients,blockOffset)

	@staticmethod
	def getEarthPositionFromEMB(emb,moon):
		earthMoonRatio=Decimal(0.813005600000000044E+02)
		earth=[0,0,0,0,0,0]
		for i in range(6):
			earth[i]=emb[i]-moon[i]/(Decimal(1)+earthMoonRatio)
		return earth
	
