from lib import HeaderParser
import struct
import re

class FileWriter:
    def __init__(self,denum,headerFile,asciiFileList,outputFile,startJD,endJD):
        self.denum=denum
        self.lastBlockJD=-9E30
        asciiheader=HeaderParser.ASCIIHeaderParser.parseHeader(headerFile)
        binheader=self.createBinaryHeader(asciiheader,denum,startJD,endJD)

        f=open(outputFile,"wb")
        f.write(binheader)
        f.close()
        
        for file in asciiFileList:
            print("Processing: "+file)
            self.appendASCIIFile(outputFile,file,startJD,endJD)

    def getNcoeffFromDividerLine(self,l):
        l=l.strip()
        t=re.split("\s+",l)
        return int(t[1])

    def appendASCIIFile(self,binaryFilename,asciiFilename,startJD,endJD):
        ascii=open(asciiFilename,"r")
        binary=open(binaryFilename,"ab")

        l=ascii.readline()
        ncoeff=self.getNcoeffFromDividerLine(l)

        ccount=0
        block=bytearray()
        notdone=True
        firstBlock=True
        while notdone:
            if(len(l)==0):
                notdone=False
            else:
                ccount=0
                while ccount<ncoeff:
                    l=ascii.readline().strip()
                    arr=re.split("\s+",l)
                    if ccount==0:
                        thisBlockJD=float(arr[0].replace("D","E"))
                    while ccount<ncoeff and len(arr)>0:
                        d=arr.pop(0)
                        block+=struct.pack("d",float(d.replace("D","E")))
                        ccount+=1
                if thisBlockJD>self.lastBlockJD:
                    if(thisBlockJD>=startJD and thisBlockJD<=endJD):
                        binary.write(block)
                    self.lastBlockJD=thisBlockJD
                if firstBlock:
                    self.firstBlockJD=thisBlockJD
                    firstBlock=False
                block=bytearray()
            l=ascii.readline().strip()

        binary.close()
        ascii.close()
        pass

    def createBinaryHeader(self,h,denum,jdStart,jdEnd):
        t=struct.pack("84s",bytes("{:<84}".format(h.description),'utf-8'))
        t+=struct.pack("84s",bytes("{:<84}".format(h.startString),'utf-8'))
        t+=struct.pack("84s",bytes("{:<84}".format(h.endString),'utf-8'))

        for i in range(400):
            if i<len(h.constantNames):
                t+=struct.pack("6s",bytes("{:<6}".format(h.constantNames[i]),'utf-8'))
            else:
                t+=struct.pack('6s',bytes('\x00\x00\x00\x00\x00\x00',"utf-8"))

        t+=struct.pack("d",jdStart)
        t+=struct.pack("d",jdEnd)
        t+=struct.pack("d",h.jdStep)
        t+=struct.pack("l",int(h.numConstants))
        t+=struct.pack("d",float(h.constants[6].replace("D","E")))
        t+=struct.pack("d",float(h.constants[7].replace("D","E")))

        for i in range(12):
            a=h.coeffPtr[i]
            t+=struct.pack("lll",int(a[0]),int(a[1]),int(a[2]))

        t+=struct.pack("l",int(denum))

        a=h.coeffPtr[12]
        t+=struct.pack("lll",int(a[0]),int(a[1]),int(a[2]))

        for i in range(int(h.numConstants)-400):
            t+=struct.pack("6s",bytes("{:<6}".format(h.constantNames[i+400]),'utf-8'))

        a=h.coeffPtr[13]
        t+=struct.pack("lll",int(a[0]),int(a[1]),int(a[2]))
        a=h.coeffPtr[14]
        t+=struct.pack("lll",int(a[0]),int(a[1]),int(a[2]))

        for i in range(int(h.blockSize)*8-len(t)):
            t+=struct.pack('c',bytes('\x00',"utf-8"))

        for i in range(len(h.constants)):
            t+=struct.pack('d',float(h.constants[i].replace("D","E")))

        for i in range(2*int(h.blockSize)*8-len(t)):
            t+=struct.pack('c',bytes('\x00',"utf-8"))

        return t

    
