

import math

def INT(a):
	return math.floor(a)


#From Meeus, CH7
def julainDateToGregorian(jd):
	temp=jd+.5
	Z=math.trunc(temp)
	F=temp-Z
	A=Z
	if(Z>=2299161):
		alpha=INT((Z-1867216.25)/36524.25)
		A=Z+1+alpha-INT(alpha/4)
	
	B=A+1524
	C=INT((B-122.1)/365.25)
	D=INT(365.25*C)
	E=INT((B-D)/30.6001)

	day=B-D-INT(30.6001*E)+F
	month=E-1
	if(E>13):
		month=E-13
	
	year=C-4716
	if(month<3):
		year=C-4715
	

	return [year,month,day]


print(julainDateToGregorian(2436116.31))
print(julainDateToGregorian(1842713.0))
print(julainDateToGregorian(1507900.13))
