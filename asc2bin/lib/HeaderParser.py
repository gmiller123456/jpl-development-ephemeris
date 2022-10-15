import re

class Header:
    def temp():
        pass

class ASCIIHeaderParser:
    
    @staticmethod
    def getGroup(group,t):
        r=re.search("GROUP\s*"+group+"(.*?)GROUP",t,re.M | re.I | re.S)
        g=r.group(1).strip()
        #g=re.sub("^\s*","",g)
        #g=re.sub("\s*$","",g)
        return g

    @staticmethod
    def parseHeader(filename):
        f=open(filename,"r")
        t=f.read()
        f.close()

        h=Header()

        r=re.search("NCOEFF=\s*(\d+)",t)
        h.blockSize=r.group(1)

        g1010=ASCIIHeaderParser.getGroup("1010",t)
        temp=re.split("\r*\n",g1010)
        h.description=temp[0]
        h.startString=temp[1]
        h.endString=temp[2]

        g1030=ASCIIHeaderParser.getGroup("1030",t)
        temp=re.split("\s+",g1030)
        h.jdStep=temp[2]
        h.jdStep=float(re.sub("\.$",".0",h.jdStep))

        g1040=ASCIIHeaderParser.getGroup("1040",t)
        h.constantNames=re.split("\s+",g1040)
        h.numConstants=h.constantNames.pop(0)

        g1041=ASCIIHeaderParser.getGroup("1041",t)
        h.constants=re.split("\s+",g1041)
        h.constants.pop(0)

        g1050=ASCIIHeaderParser.getGroup("1050",t)
        temp=re.split("\s+",g1050)
        numPlanets=int((len(temp)/3))

        h.coeffPtr=[]
        for i in range(16):
            if(i<numPlanets):
                lastoffset=int(temp[i])
                h.coeffPtr.append([temp[i],temp[i+numPlanets],temp[i+numPlanets*2]])
            else:
                h.coeffPtr.append([0,0,0])

        return h
