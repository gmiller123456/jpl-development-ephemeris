
#!/usr/bin/python

#Program to test DE405 implementation against JPL supplied test vecotrs
#Greg Miller (gmiller@gregmiller.net) 2019
#Released as public domain

import math
from de import *
import decimal

au=Decimal(0.149597870691000015E+09)
map=[0,0,1,13,3,4,5,6,7,8,9,10,12,2,11,12]

def get(series,jd,x):
	jd=Decimal(jd)
	if(series==12 or series==0):
		return 0

	s=map[series]

	global de
	b=[0,0,0,0,0,0]

	if(series==3):
		emb=de.getAllPropertiesForSeries(2,jd)
		moon=de.getAllPropertiesForSeries(9,jd)
		b=DE.getEarthPositionFromEMB(emb,moon)
	elif(series==10):
		emb=de.getAllPropertiesForSeries(2,jd)
		moon=de.getAllPropertiesForSeries(9,jd)
		earth=DE.getEarthPositionFromEMB(emb,moon)
		for i in range(6):
			moon[i]+=earth[i]
		b=moon
	else:
		b=de.getAllPropertiesForSeries(s,jd)

	return b[x-1]

def testpo(deNumber,year,month,day,jd,t,c,x,expectedValue):
	expectedValue=Decimal(expectedValue)

	t1=get(t,jd,x)
	t2=get(c,jd,x)

	v=t1-t2

	if(t!=15 and t!=14):
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
	year=int(s[5:9])
	month=int(s[10:12])
	day=int(s[13:15])
	jd=Decimal(s[16:25])
	t=int(s[25:28])
	c=int(s[28:31])
	x=int(s[31:34])

	v=Decimal(s[34:57])
	#print(f"{de} {year} {month} {day} {jd} {t} {c} {x} {v}")
	testpo(de, year, month, day, jd, t, c, x, v)

def runTestFile(filename):
	f=open(filename,"r")

	l=f.readline()
	while(l[0:3]!="EOT"):
		l=f.readline()

	for l in f:
		parseTestCase(l)

	f.close();


tests=0
fail=0
skipped=0

#l=[  "405",      "ascp1600.405",   20,  1018,   "1599 DEC 09 00:00:00",   "2201 FEB 20 00:00:00",     2305424.50,     2525008.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"]
#de=DE(l)
#runTestFile("de405/testpo.405")

#l=[  "410",      "ascp1960.410",   20,  1018,   "1900 FEB 06 00:00:00",   "2019 DEC 15 00:00:00",     2415056.50,     2458832.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"]
#de=DE(l)
#runTestFile("de410/testpo2.410")

l=[  "430",      "ascp1550.430",   100,  1018,   "1549-DEC-21 00:00:00",   "2650-JAN-25 00:00:00",     2287184.50,     2688976.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end"]
de=DE(l)
runTestFile("de430/testpo.430")



print()
print(f"Tests ran:{tests} Failed:{fail} Skipped:{skipped}")

