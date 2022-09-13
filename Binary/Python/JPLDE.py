#!/usr/bin/python

# Greg Miller (gmiller@gregmiller.net) 2022
# Released as public domain
# http://www.celestialprogramming.com/
# 
# Class to read binary versions of JPL's Development Ephemeris.  Files in
# the propper format can be obtained from:
# ftp://ssd.jpl.nasa.gov/pub/eph/planets/Linux
# 
# #    Properties       Units          Center Description
# 0    x,y,z            km             SSB    Mercury
# 1    x,y,z            km             SSB    Venus
# 2    x,y,z            km             SSB    Earth-Moon barycenter
# 3    x,y,z            km             SSB    Mars
# 4    x,y,z            km             SSB    Jupiter
# 5    x,y,z            km             SSB    Saturn
# 6    x,y,z            km             SSB    Uranus
# 7    x,y,z            km             SSB    Neptune
# 8    x,y,z            km             SSB    Pluto
# 9    x,y,z            km             Earth  Moon (geocentric)
# 10   x,y,z            km             SSB    Sun
# 11   dPsi,dEps        radians               Earth Nutations in longitude and obliquity
# 12   phi,theta,psi    radians               Lunar mantle libration
# 13   Ox,Oy,Oz         radians/day           Lunar mantle angular velocity
# 14   t                seconds               TT-TDB (at geocenter)
# 
# Example: (prints x coordinate of venus using first JD available)
# 
# de=JPLDE("E:\\Astronomy\\_Ephemeris\\JPLDEBinaries\\jpleph.405")
# print(de.getPlanet(1,de.getHeader().jdStart)[0])
#
# 24857048.341240495

import struct
import array
import math

class JPLDE:
    def __init__(self,filename):
        self.filename=filename
        h=JPLDEHeader(filename)
        self.header=h
        self.jdStart=h.jdStart
        self.jdEnd=h.jdEnd
        self.jdStep=h.jdStep
        self.blockSize=h.blockSize
        self.cachedBlockNum=-1
        self.coeffPtr=h.coeffPtr

    def getBlockForJD(self,jd):
        jdoffset=jd-self.jdStart
        blockNum=int(jdoffset/self.jdStep)

        if(not self.cachedBlockNum==blockNum):
            f=open(self.filename,"rb")
            f.seek(blockNum*self.blockSize + 2*self.blockSize)
            bin=f.read(self.blockSize)
            f.close()
            self.cachedBlockNum=blockNum
            self.block=array.array("d",bin)

        return self.block

    def getPlanet(self,planet,jd):
        block=self.getBlockForJD(jd)
        d=self.coeffPtr[planet]
        seriesOffset=d[0]-1
        ccount=d[1]
        subint=d[2]
        varCount=self.header.seriesVars[planet]

        startJD=block[0]
        endJD=block[1]
        blockDuration=endJD-startJD

        subintervalDuration=blockDuration/subint
        subintervalSize=ccount*varCount
        subintervalNumber=math.floor((jd-startJD)/subintervalDuration)
        subintervalStart=subintervalDuration*subintervalNumber
        subintervalEnd=subintervalDuration*subintervalNumber+subintervalDuration

        #Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
        #If using two doubles for JD, this is where the two parts should be combined:
        #e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
        x=((jd-(startJD+subintervalStart))/subintervalDuration)*2-1

        properties=[0,0,0,0,0,0]
        for i in range(varCount):
            offset=seriesOffset+i*ccount+subintervalSize*subintervalNumber
            t=self.computePolynomial(x,block[offset:offset+ccount])
            properties[i]=t[0]

            velocity = t[1]
            velocity=velocity*((2.0)*subint/blockDuration)
            properties[i+varCount]=velocity
        
        return properties

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

    @staticmethod
    def getEarthPositionFromEMB(emb,moon):
        earthMoonRatio=0.813005600000000044E+02
        earth=[0,0,0,0,0,0]
        for i in range(6):
            earth[i]=emb[i]-moon[i]/(1.0+earthMoonRatio)
        return earth

class JPLDEHeader:
    def __init__(self,filename):
        self.seriesVars=[3,3,3,3,3,3,3,3,3,3,3,2,3,3,1]
        self.filename=filename
        self.loadHeader(filename)

    def findRecLength(self):
        for i in range(len(self.coeffPtr)-1,-1,-1):
            if(self.coeffPtr[i][0] != 0):
                cp=self.coeffPtr[i]
                reclen=cp[0]+(cp[1]*cp[2]*self.seriesVars[i])-1
                return reclen*8

    def loadHeader(self,filename):
        f=open(filename,"rb")

        self.description=f.read(84)
        self.startString=f.read(84)
        self.endString=f.read(84)
        self.constantNames=[]
        for i in range(400):
            self.constantNames.append(f.read(6))

        self.jdStart=struct.unpack("d",f.read(8))[0]
        self.jdEnd=struct.unpack("d",f.read(8))[0]
        self.jdStep=struct.unpack("d",f.read(8))[0]
        self.numConstants=struct.unpack("l",f.read(4))[0]
        self.au=struct.unpack("d",f.read(8))[0]
        self.emrat=struct.unpack("d",f.read(8))[0]

        self.coeffPtr=[]
        #Group 1050 data
        for i in range(12):
            self.coeffPtr.append([])
            for j in range(3):
                self.coeffPtr[i].append(struct.unpack("l",f.read(4))[0])

        self.version=struct.unpack("l",f.read(4))[0]

        #more Group 1050 data
        self.coeffPtr.append([])
        for i in range(3):
            self.coeffPtr[12].append(struct.unpack("l",f.read(4))[0])

        #more constant names, if there's more than 400
        if(self.numConstants>400):
            for i in range(self.numConstants-400):
                self.constantNames.append(f.read(6))

        #more Group 1050 data
        self.coeffPtr.append([])
        for i in range(3):
            self.coeffPtr[13].append(struct.unpack("l",f.read(4))[0])

        #more Group 1050 data
        self.coeffPtr.append([])
        for i in range(3):
            self.coeffPtr[14].append(struct.unpack("l",f.read(4))[0])

        #Compute block size based on offsets in Group 1050
        self.blockSize=self.findRecLength()
        padSize=self.blockSize-f.tell()
        f.read(padSize)

        self.constants=[]
        for i in range(self.numConstants):
            self.constants.append(struct.unpack("d",f.read(8))[0])

        f.close()
