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
		t=[1,x]

		for n in range(2,len(coefficients)):
			tn=2*x*t[n-1]-t[n-2]
			t.append(tn)

		#Multiply the polynomial by the coefficients.
		#Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
		position=0
		for i in range(len(coefficients)-1,-1,-1):
			position+=coefficients[i]*t[i]

		#Compute velocity (just the derivitave of the above)
		v=[0,1,4*x]
		for n in range(3,len(coefficients)):
			v.append(2*x*v[n-1]+2*t[n-1]-v[n-2])

		velocity=0
		for i in range(len(coefficients)-1,-1,-1):
			velocity+=v[i]*coefficients[i]

		return [position,velocity]

class DE:
	def __init__(self,data):
		#First three variables (after "name") are from "GROUP 1030" in header file
		#Last variable is NCOEFF from first line of header file
		self.name=data[0]
		self.start=float(data[6])
		self.end=float(data[7])
		self.daysPerBlock=int(data[8])
		self.coefficientsPerBlock=int(data[3]) + 2  #+2 because each block is padded with two zeros
		self.yearsPerFile=int(data[2])
		self.fileBaseName=data[1]
		self.fileBase=int(self.fileBaseName[4:8])
		self.fileNamePad=4
		if(self.fileBaseName[9:10]=="."):
			print ("Here")
			self.fileBase=int(self.fileBaseName[4:9])
			self.fileNamePad=5

		if(self.fileBaseName[3:4]=="m"):
			self.fileBase*=-1

		propertyCountIndex=9
		propertyCount=data[propertyCountIndex]

		series=[]
		for i in range(propertyCount):
			series.append(JPLSeries(data[i+propertyCountIndex+1+propertyCount*0],
				data[i+propertyCountIndex+1+propertyCount*1],
				data[i+propertyCountIndex+1+propertyCount*4],
				data[i+propertyCountIndex+1+propertyCount*2],
				data[i+propertyCountIndex+1+propertyCount*3]))

		self.series=series
		self.loadedFile=""

	def loadFile(self,filename):
		if(self.loadedFile==filename):
			return
		
		print(f"Loading: {filename}")

		self.coefficients=[]
		f=open("de"+self.name+"/"+filename,"r")
		for l in f:
			if(len(l) >17 and l[17:18]!=" "):
				#print(l)
				t=re.split(" +",l);
				for i in range(1,4):
					if(i>len(t)-1):
						self.coefficients.append(Decimal("0.0"));
					else:
						self.coefficients.append(Decimal(t[i].replace("D","e")));
		f.close()
		self.loadedFile=filename
		self.chunkStart=self.coefficients[0]

		self.chunkEnd=self.coefficients[len(self.coefficients)-self.coefficientsPerBlock+1]

	def loadFileForJD(self,jd):
		year=DE.julainDateToGregorian(jd)[0]
		year=math.floor((year-self.fileBase)/self.yearsPerFile)*self.yearsPerFile+self.fileBase

		print(f"{year} {self.fileBase} {self.yearsPerFile}")

		pm="p"
		if(year<0):
			year=abs(year)
			pm="m"

		fileName=("asc"+pm+"{:0>"+str(self.fileNamePad)+"d}."+self.name).format(year)
		neededFile=fileName
		self.loadFile(neededFile)

	def getAllPropertiesForSeries(self,series,JD):
		self.loadFileForJD(JD)

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
	
	@staticmethod
	def INT(a):
		return math.floor(a)

	#From Meeus, CH7
	@staticmethod
	def julainDateToGregorian(jd):
		temp=jd+Decimal(.5)
		Z=math.trunc(temp)
		F=temp-Z
		A=Z
		if(Z>=2299161):
			alpha=DE.INT((Z-Decimal(1867216.25))/Decimal(36524.25))
			A=Z+1+alpha-DE.INT(alpha/4)
		
		B=A+1524
		C=DE.INT((B-122.1)/365.25)
		D=DE.INT(365.25*C)
		E=DE.INT((B-D)/30.6001)

		day=B-D-DE.INT(30.6001*E)+F
		month=E-1
		if(E>13):
			month=E-13
		
		year=C-4716
		if(month<3):
			year=C-4715

		return [year,month,day]
