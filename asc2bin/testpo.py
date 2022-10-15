#!/usr/bin/python

#Program to test DE implementation against JPL supplied test vecotrs
#Greg Miller (gmiller@gregmiller.net) 2019
#Released as public domain

import math
from JPLDE import *
au=0

map=[0,0,1,13,3,4,5,6,7,8,9,10,12,2,11,12,0,14]

def get(series,jd,x):
    jd=float(jd)
    #0 means no center, and 12 is the SSB which is always 0
    if(series==12 or series==0):
        return 0

    s=map[series]

    global de
    b=[0,0,0,0,0,0]

    if(series==3):
        emb=de.getPlanet(2,jd)
        moon=de.getPlanet(9,jd)
        b=JPLDE.getEarthPositionFromEMB(emb,moon)
    elif(series==10):
        emb=de.getPlanet(2,jd)
        moon=de.getPlanet(9,jd)
        earth=JPLDE.getEarthPositionFromEMB(emb,moon)
        for i in range(6):
            moon[i]+=earth[i]
        b=moon
    else:
        b=de.getPlanet(s,jd)

    return b[x-1]

def testpo(deNumber,year,month,day,jd,t,c,x,expectedValue):
    global de
    global skipped
    if(jd<de.jdStart or jd>=de.jdEnd):
        skipped=skipped+1
        return

    expectedValue=float(expectedValue)

    t1=get(t,jd,x)
    t2=get(c,jd,x)

    v=t1-t2

    if(t!=15 and t!=14 and t!=17):
        v/=au

    error=abs(v-expectedValue)

    if(error>1.0E-8 or math.isnan(error)):
        status=f"Fail: {jd}\t{t}\t{c}\t{x}\t{expectedValue}\t{v}\tDiff={error}"
        print(status)
        global fail
        fail+=1
    else:
        status=f"Pass : {jd}\t{t}\t{c}\t{x}\t{expectedValue}\t{v}\tDiff={error}"
        #print(status)
        pass

    global tests
    tests+=1

def parseTestCase(s):
    de=int(s[0:3])
    year=int(s[4:9])
    month=int(s[10:12])
    day=int(s[13:15])
    jd=float(s[15:25])
    t=int(s[25:28])
    c=int(s[28:31])
    x=int(s[31:34])

    v=float(s[34:57])
    try:
        testpo(de, year, month, day, jd, t, c, x, v)
    except FileNotFoundError as e:
        print(e)
        global skipped
        skipped+=1

def runTestFile(filename):
    f=open(filename,"r")

    l=f.readline()
    while(l[0:3]!="EOT"):
        l=f.readline()

    for l in f:
        parseTestCase(l)

    f.close();

def runVersionTest(binaryFile,testFile):
    global de
    global au

    de=JPLDE(binaryFile)
    au=de.header.au
    runTestFile(testFile)

de=[]

path=".\\"
versions=["102","200","202","403","405","406","410","413","414","418","421","422","423","424","430","430t","431","432","432t","433","434","435","436","436t","438","438t","440","440t","441"]
versions=["405"]

for i in range(len(versions)):
    tests=0
    fail=0
    skipped=0

    v=versions[i]
    runVersionTest("jpleph."+v,"testpo."+v)

    print()
    print(v)
    print(f"Tests ran:{tests} Failed:{fail} Skipped:{skipped}")
